using System.Reflection;
using Astrea.Tests.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.ResourceTable.Tests;

[TestClass]
public sealed class ResourceTableInteropTests
{
    [TestMethod]
    public async Task ResizeInterop_FollowsResizableColumnsAndColumnChanges()
    {
        var runtime = new RecordingJsRuntime();
        using var renderer = new TreeTestRenderer();
        var component = new ResourceTable();
        typeof(ResourceTable)
            .GetProperty("Js", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(component, runtime);
        var componentId = renderer.Attach(component);
        var columns = new[]
        {
            new TableColumn("A") { MinWidth = 40 },
            new TableColumn("B") { MinWidth = 60 },
        };

        await RenderAsync(renderer, componentId, columns, resizable: true);
        await WaitForAsync(() => runtime.Module.Calls.Count > 0, "Resize interop did not initialize.");

        var module = runtime.Module;
        Assert.AreEqual(1, module.Calls.Count);
        Assert.AreEqual("initializeResizeTable", module.Calls[0].Identifier);
        CollectionAssert.AreEqual(new double?[] { 40, 60 }, GetMinWidths(module.Calls[0].Args));

        columns = new[]
        {
            new TableColumn("A") { MinWidth = 100 },
            new TableColumn("B") { MinWidth = 60 },
        };
        await RenderAsync(renderer, componentId, columns, resizable: true);

        Assert.AreEqual(2, module.Calls.Count);
        Assert.AreEqual("initializeResizeTable", module.Calls[1].Identifier);
        CollectionAssert.AreEqual(new double?[] { 100, 60 }, GetMinWidths(module.Calls[1].Args));

        await RenderAsync(renderer, componentId, columns, resizable: false);
        Assert.AreEqual("disposeResizableColumns", module.Calls[^1].Identifier);

        await RenderAsync(renderer, componentId, columns, resizable: true);
        Assert.AreEqual(3, module.Calls.Count(call => call.Identifier == "initializeResizeTable"));

        await component.DisposeAsync();
        Assert.IsTrue(module.Disposed);
    }

    private static Task RenderAsync(
        TreeTestRenderer renderer,
        int componentId,
        IEnumerable<TableColumn> columns,
        bool resizable)
        => renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(ResourceTable.Columns)] = columns,
            [nameof(ResourceTable.ResizableColumns)] = resizable,
            [nameof(ResourceTable.TableBody)] = (RenderFragment)(builder => builder.AddContent(0, "rows")),
        }));

    private static async Task WaitForAsync(Func<bool> condition, string message)
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

    private static double?[] GetMinWidths(object?[]? args)
    {
        var options = args![2]!;
        var property = options.GetType().GetProperty("minWidths")!;
        return (double?[])property.GetValue(options)!;
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public RecordingModule Module { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.AreEqual("import", identifier);
            return ValueTask.FromResult((TValue)(object)Module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class RecordingModule : IJSObjectReference
    {
        public List<Call> Calls { get; } = new();

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(new Call(identifier, args));
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

    private sealed record Call(string Identifier, object?[]? Args);
}
