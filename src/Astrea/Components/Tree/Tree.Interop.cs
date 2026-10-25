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
    private IJSObjectReference? _keyboardInstance;
    private DotNetObjectReference<Tree>? _dotNetRef;
    private bool _dragDisposed;

    /// <summary>确保拖拽已初始化（幂等；容器已渲染、AllowDrag=true 时调用）。</summary>
    private async Task EnsureDragInitializedAsync()
    {
        if (_dragDisposed || !AllowDrag || Js is null || DataSource is null || _dropInstance is not null)
        {
            return;
        }

        _dotNetRef ??= DotNetObjectReference.Create(this);

        try
        {
            var module = _dropModule;
            if (module is null)
            {
                module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/Astrea/astrea-tree.js");
                if (_dragDisposed || !AllowDrag || DataSource is null)
                {
                    await DisposeReferenceQuietlyAsync(module);
                    return;
                }

                _dropModule = module;
            }

            var instance = await module.InvokeAsync<IJSObjectReference>(
                "initialize", $"tree-container-{_id}", _id, _dotNetRef);
            if (_dragDisposed || !AllowDrag || DataSource is null)
            {
                await DestroyInstanceQuietlyAsync(instance);
                return;
            }

            _dropInstance = instance;
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线：放弃本次初始化，后续渲染可重试。
        }
        catch (ObjectDisposedException)
        {
            // Dispose 与初始化交错时模块/实例已被释放。
        }
    }

    /// <summary>销毁拖拽实例并释放（幂等；AllowDrag=false 或 Dispose 时调用）。</summary>
    private async Task DestroyDragAsync()
    {
        // 运行期关闭拖拽时需要再渲染一次以移除 Blazor 拥有的预览 DOM；Dispose 期间禁止请求渲染。
        InvalidateDragState(requestRender: !_dragDisposed);

        var instance = _dropInstance;
        _dropInstance = null;
        if (instance is null)
        {
            return;
        }

        try
        {
            await instance.InvokeVoidAsync("destroy");
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线：仅表示连接已断开，忽略；其他异常不得吞掉。
        }
        finally
        {
            try
            {
                await instance.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // 断线后 JS object 已不可达；字段已先复位，本地清理仍可继续。
            }
        }
    }

    private async Task EnsureKeyboardInitializedAsync()
    {
        if (_dragDisposed || !UseInternalKeyboard || Js is null || _keyboardInstance is not null)
        {
            return;
        }

        try
        {
            var module = _dropModule;
            if (module is null)
            {
                module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/Astrea/astrea-tree.js");
                if (_dragDisposed || !UseInternalKeyboard)
                {
                    await DisposeReferenceQuietlyAsync(module);
                    return;
                }

                _dropModule = module;
            }

            var instance = await module.InvokeAsync<IJSObjectReference>(
                "initializeKeyboard", $"tree-container-{_id}");
            if (_dragDisposed || !UseInternalKeyboard)
            {
                await DestroyInstanceQuietlyAsync(instance);
                return;
            }

            _keyboardInstance = instance;
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线：放弃本次初始化，后续渲染可重试。
        }
        catch (ObjectDisposedException)
        {
            // Dispose 与初始化交错时模块/实例已被释放。
        }
    }

    private static async Task DestroyInstanceQuietlyAsync(IJSObjectReference instance)
    {
        try
        {
            await instance.InvokeVoidAsync("destroy");
        }
        catch (JSDisconnectedException)
        {
            // 断线后不能调用 JS，本地引用仍需释放。
        }
        catch (ObjectDisposedException)
        {
            // 实例已随模块释放。
        }
        finally
        {
            await DisposeReferenceQuietlyAsync(instance);
        }
    }

    private static async Task DisposeReferenceQuietlyAsync(IJSObjectReference reference)
    {
        try
        {
            await reference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // 断线后 JS 句柄已不可达。
        }
        catch (ObjectDisposedException)
        {
            // 已被并发释放。
        }
    }

    private async Task DestroyKeyboardAsync()
    {
        var instance = _keyboardInstance;
        _keyboardInstance = null;
        if (instance is null)
        {
            return;
        }

        try
        {
            await instance.InvokeVoidAsync("destroy");
        }
        catch (JSDisconnectedException)
        {
            // The local reference is still released below.
        }
        finally
        {
            try
            {
                await instance.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone; field state has been cleared.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dragDisposed)
        {
            return;
        }

        _dragDisposed = true;
        EndEditingSession(restoreFocus: false);
        try
        {
            await DestroyDragAsync();
        }
        finally
        {
            try
            {
                await DestroyKeyboardAsync();
            }
            finally
            {
                try
                {
                    await DisposeScrollModuleAsync();
                }
                finally
                {
                    var module = _dropModule;
                    _dropModule = null;
                    try
                    {
                        if (module is not null)
                        {
                            await module.DisposeAsync();
                        }
                    }
                    catch (JSDisconnectedException)
                    {
                        // Blazor Server 断线不妨碍释放本地 DotNetObjectReference。
                    }
                    finally
                    {
                        _dotNetRef?.Dispose();
                        _dotNetRef = null;
                    }
                }
            }
        }
    }
}
