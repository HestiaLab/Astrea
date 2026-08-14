using Microsoft.JSInterop;

namespace Astrea.Components.Tree;

/// <summary>
/// JS 生命周期：分别持有并幂等释放 ES module、initialize 实例与 DotNetObjectReference。
/// AllowDrag 运行期启停通过 Ensure/Destroy 对称管理。
/// </summary>
public sealed partial class Tree
{
    private IJSObjectReference? _dropModule;
    private IJSObjectReference? _dropInstance;
    private DotNetObjectReference<Tree>? _dotNetRef;

    /// <summary>确保拖拽已初始化（幂等；容器已渲染、AllowDrag=true 时调用）。</summary>
    private async Task EnsureDragInitializedAsync()
    {
        if (Js is null || DataSource is null || _dropInstance is not null)
        {
            return;
        }

        _dotNetRef ??= DotNetObjectReference.Create(this);
        _dropModule ??= await Js.InvokeAsync<IJSObjectReference>("import", "./_content/Astrea/astrea-tree.js");
        _dropInstance = await _dropModule.InvokeAsync<IJSObjectReference>(
            "initialize", $"tree-container-{_id}", _id, _dotNetRef);
    }

    /// <summary>销毁拖拽实例并释放（幂等；AllowDrag=false 或 Dispose 时调用）。</summary>
    private async Task DestroyDragAsync()
    {
        if (_dropInstance is null)
        {
            return;
        }

        try
        {
            await _dropInstance.InvokeVoidAsync("destroy");
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线：仅表示连接已断开，忽略；其他异常不得吞掉。
        }

        await _dropInstance.DisposeAsync();
        _dropInstance = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DestroyDragAsync();
        await DisposeScrollModuleAsync();

        if (_dropModule is not null)
        {
            await _dropModule.DisposeAsync();
            _dropModule = null;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
