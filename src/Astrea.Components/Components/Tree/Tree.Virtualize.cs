using Microsoft.JSInterop;

namespace Astrea.Components.Tree;

/// <summary>
/// 虚拟化焦点协调：跨视口键盘导航时，先滚动目标索引 → 等待 Virtualize 重渲染 →
/// 再更新 aria-activedescendant（计划 §5.6 / §5.12）。aria-activedescendant 只引用已渲染节点。
/// </summary>
public sealed partial class Tree
{
    private IJSObjectReference? _scrollModule;
    private string? _ariaFocusId;
    private int _focusSyncGeneration;

    /// <summary>
    /// 协调虚拟化模式下的 aria-activedescendant。焦点节点已渲染时立即引用其 DOM id；
    /// 未渲染时滚动到目标索引，Virtualize 重渲染后由下一轮 OnAfterRender 再次触发。
    /// </summary>
    private async Task SyncVirtualizationFocusAsync()
    {
        if (_controller is null || Js is null || _dragDisposed)
        {
            return;
        }

        // 新的焦点同步取消尚未完成的旧轮询，避免过期导航写回 aria-activedescendant。
        var generation = ++_focusSyncGeneration;
        var focusedId = _controller.FocusedId;

        if (focusedId == _ariaFocusId)
        {
            return; // 已同步
        }

        if (focusedId is null)
        {
            _ariaFocusId = null;
            StateHasChanged();
            return;
        }

        var index = IndexOfVisible(_controller.FocusedNode);
        if (index < 0)
        {
            // 防御：焦点节点不在可见序列（控制器不变量应保证不发生）
            _ariaFocusId = null;
            StateHasChanged();
            return;
        }

        try
        {
            _scrollModule ??= await Js.InvokeAsync<IJSObjectReference>("import", "./_content/Astrea.Components/astrea-tree.js");
            if (_dragDisposed || generation != _focusSyncGeneration)
            {
                return;
            }

            var nodeDomId = NodeDomId(focusedId);

            // 先尝试滚动到目标索引；未渲染时轮询等待 Virtualize 重渲染（子组件滚动重渲染不触发父 OnAfterRender）
            var rendered = await _scrollModule.InvokeAsync<bool>(
                "ensureNodeRendered", $"tree-container-{_id}", nodeDomId, index, ItemSize);
            for (int attempt = 0; !rendered && attempt < 40; attempt++)
            {
                if (_dragDisposed || generation != _focusSyncGeneration)
                {
                    return;
                }

                await Task.Delay(30);
                if (_dragDisposed || generation != _focusSyncGeneration)
                {
                    return;
                }

                rendered = await _scrollModule.InvokeAsync<bool>("isNodeRendered", nodeDomId);
            }

            if (_dragDisposed || generation != _focusSyncGeneration || _controller.FocusedId != focusedId)
            {
                return;
            }

            if (rendered)
            {
                _ariaFocusId = focusedId;
                StateHasChanged();
            }
            // 超时仍未渲染：保持 _ariaFocusId 不变（不指向未渲染节点），下次导航会重新协调
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线后不再同步虚拟化焦点。
        }
        catch (ObjectDisposedException)
        {
            // Dispose 与滚动模块调用交错。
        }
    }

    private int IndexOfVisible(TreeNode? node)
    {
        if (node is null || _controller is null)
        {
            return -1;
        }

        var visible = _controller.FlattenVisible();
        for (int i = 0; i < visible.Count; i++)
        {
            if (ReferenceEquals(visible[i], node))
            {
                return i;
            }
        }

        return -1;
    }

    private async ValueTask DisposeScrollModuleAsync()
    {
        if (_scrollModule is not null)
        {
            try
            {
                await _scrollModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Blazor Server 断线：仅表示连接已断开，忽略；其他异常不得吞掉
            }

            _scrollModule = null;
        }
    }
}
