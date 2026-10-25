using System.Reflection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public sealed class TreeInteropLifecycleTests
{
    [TestMethod]
    public async Task EnsureDragInitialized_DisposedDuringModuleImport_DisposesModule()
    {
        var runtime = new GatedJsRuntime();
        var tree = CreateDragTree(runtime);
        var ensure = InvokePrivateTask(tree, "EnsureDragInitializedAsync");
        await WaitUntilAsync(() => runtime.ImportRequested, "Module import was not requested.");

        await tree.DisposeAsync();
        var module = new TrackingModule();
        runtime.Module.SetResult(module);
        await ensure.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(module.Disposed);
        Assert.IsNull(GetPrivateField(tree, "_dropModule"));
        Assert.IsNull(GetPrivateField(tree, "_dropInstance"));
    }

    [TestMethod]
    public async Task EnsureDragInitialized_DisposedDuringInitialize_DestroysInstance()
    {
        var module = new TrackingModule();
        var tree = CreateDragTree(new ImmediateJsRuntime(module));
        var ensure = InvokePrivateTask(tree, "EnsureDragInitializedAsync");
        await WaitUntilAsync(() => module.InitializeRequested, "initialize was not requested.");

        await tree.DisposeAsync();
        var instance = new TrackingInstance();
        module.Instance.SetResult(instance);
        await ensure.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(instance.DestroyCalled);
        Assert.IsTrue(instance.Disposed);
        Assert.IsNull(GetPrivateField(tree, "_dropInstance"));
    }

    [TestMethod]
    public async Task EnsureKeyboardInitialized_DisposedDuringModuleImport_DisposesModule()
    {
        var runtime = new GatedJsRuntime();
        var tree = CreateDragTree(runtime);
        var ensure = InvokePrivateTask(tree, "EnsureKeyboardInitializedAsync");
        await WaitUntilAsync(() => runtime.ImportRequested, "Module import was not requested.");

        await tree.DisposeAsync();
        var module = new TrackingModule();
        runtime.Module.SetResult(module);
        await ensure.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(module.Disposed);
        Assert.IsNull(GetPrivateField(tree, "_dropModule"));
    }

    private static Tree CreateDragTree(IJSRuntime runtime)
    {
        var tree = new Tree();
        SetProperty(tree, nameof(Tree.AllowDrag), true);
        SetProperty(tree, nameof(Tree.DataSource), new[] { new TreeNode("root", id: "root") });
        typeof(Tree).GetProperty("Js", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tree, runtime);
        return tree;
    }

    private static void SetProperty(Tree tree, string name, object value)
        => typeof(Tree).GetProperty(name, BindingFlags.Instance | BindingFlags.Public)!.SetValue(tree, value);

    private static object? GetPrivateField(Tree tree, string name)
        => typeof(Tree).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tree);

    private static Task InvokePrivateTask(Tree tree, string name)
        => (Task)typeof(Tree).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(tree, null)!;

    private static async Task WaitUntilAsync(Func<bool> condition, string message)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail(message);
    }

    private sealed class GatedJsRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> _module =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<IJSObjectReference> Module => _module;

        public bool ImportRequested { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            ImportRequested = true;
            return AwaitAsync<TValue>(_module.Task);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        private static async ValueTask<TValue> AwaitAsync<TValue>(Task<IJSObjectReference> task)
            => (TValue)(object)await task;
    }

    private sealed class ImmediateJsRuntime : IJSRuntime
    {
        private readonly IJSObjectReference _module;

        public ImmediateJsRuntime(IJSObjectReference module) => _module = module;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult((TValue)(object)_module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class TrackingModule : IJSObjectReference
    {
        private readonly TaskCompletionSource<IJSObjectReference> _instance =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<IJSObjectReference> Instance => _instance;

        public bool InitializeRequested { get; private set; }

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            InitializeRequested = true;
            return AwaitAsync<TValue>(_instance.Task);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        private static async ValueTask<TValue> AwaitAsync<TValue>(Task<IJSObjectReference> task)
            => (TValue)(object)await task;
    }

    private sealed class TrackingInstance : IJSObjectReference
    {
        public bool DestroyCalled { get; private set; }

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "destroy")
            {
                DestroyCalled = true;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
