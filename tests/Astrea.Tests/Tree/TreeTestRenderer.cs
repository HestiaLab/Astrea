using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable BL0006

namespace Astrea.Tests.Tree;

/// <summary>
/// Minimal framework-only renderer for component lifecycle and render-tree tests.
/// Browser behavior remains covered by the separate end-to-end suite.
/// </summary>
internal sealed class TreeTestRenderer : Renderer
{
    public TreeTestRenderer()
        : base(EmptyServiceProvider.Instance, NullLoggerFactory.Instance)
    {
    }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    public int Attach(IComponent component) => AssignRootComponentId(component);

    public Task RenderAsync(int componentId, ParameterView parameters)
        => Dispatcher.InvokeAsync(() => RenderRootComponentAsync(componentId, parameters));

    public ArrayRange<RenderTreeFrame> Frames(int componentId)
        => GetCurrentRenderTreeFrames(componentId);

    protected override void HandleException(Exception exception)
        => throw new InvalidOperationException("Component rendering failed.", exception);

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        => Task.CompletedTask;

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}

#pragma warning restore BL0006
