using System.Text.Json;
using Microsoft.JSInterop;

namespace Astrea.Components.Tree;

public sealed partial class Tree
{
    private const double previewOffset = 20;

    private bool isPreview;
    private int previewCount;
    private double PreviewX;
    private double PreviewY;

    // BeginDrag 时冻结的不可变会话快照；drop 不得用当前 revision 或篡改后的源列表替代它。
    private DragSession? _dragSession;

    private async Task ToggleNode(TreeNode node)
    {
        if (_controller is null)
        {
            return;
        }

        var oldExpandedIds = SnapshotExpandedIds();
        var revision = _controller.Revision;
        if (node.IsExpanded)
        {
            if (!await CanCollapseAsync(node) || !IsCurrentNode(node, revision))
            {
                return;
            }

            if (_controller.Collapse(node))
            {
                await NotifyExpansionChangedAsync(node, isExpanded: false, oldExpandedIds);
            }
        }
        else
        {
            if (!await CanExpandAsync(node) || !IsCurrentNode(node, revision))
            {
                return;
            }

            if (_controller.Expand(node))
            {
                await NotifyExpansionChangedAsync(node, isExpanded: true, oldExpandedIds);
            }
        }

        StateHasChanged();
    }

    /// <summary>
    /// JS dragstart 回调：接收 JS 从 DOM 收集的源节点 id 与 sessionId，C# 解析、去重、归一化，
    /// 并记录 revision；返回归一化源与 revision 供 JS 在 drop 时组装完整协议消息。
    /// </summary>
    [JSInvokable]
    public string BeginDrag(string sessionId, string sourceNodeIdsJson)
    {
        if (_controller is null || !AllowDrag || _dragDisposed || string.IsNullOrWhiteSpace(sessionId))
        {
            return "{}";
        }

        List<string>? sourceNodeIds;
        try
        {
            sourceNodeIds = JsonSerializer.Deserialize<List<string>>(sourceNodeIdsJson);
        }
        catch (JsonException)
        {
            return "{}";
        }

        if (sourceNodeIds is null || sourceNodeIds.Count == 0)
        {
            return "{}";
        }

        var nodes = new List<TreeNode>(sourceNodeIds.Count);
        var seen = new HashSet<string>();
        foreach (var id in sourceNodeIds)
        {
            var node = _controller.FindById(id);
            if (node is null || node.IsDisabled)
            {
                return "{}";
            }

            if (!seen.Add(id))
            {
                continue;
            }

            nodes.Add(node);
        }

        var normalizedSourceIds = DragProtocol.NormalizeSources(nodes).Select(n => n.Id).ToArray();
        var sourceRevision = _controller.Revision;
        var sourceSnapshot = Array.AsReadOnly(normalizedSourceIds);

        _dragSession = new DragSession(sessionId, sourceRevision, sourceSnapshot);

        return JsonSerializer.Serialize(new TreeDragBeginResult
        {
            SourceRevision = sourceRevision,
            SourceNodeIds = sourceSnapshot,
        });
    }

    /// <summary>
    /// JS drop 回调：解析并完整校验拖拽消息（协议/树归属/revision/relation/源目标/结构/guard），
    /// 全部通过后才触发一次 OnDrop。未知、陈旧、重复、跨 Tree 或非法消息确定性拒绝。
    /// </summary>
    [JSInvokable]
    public async Task HandleDrop(string messageJson)
    {
        if (_controller is null || !AllowDrag || _dragDisposed)
        {
            return;
        }

        TreeDragMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TreeDragMessage>(messageJson);
        }
        catch (JsonException)
        {
            return;
        }

        var session = message is null ? null : ConsumeDragSession(message.SessionId);
        if (message is null || session is null)
        {
            return;
        }

        if (message.SourceRevision != session.SourceRevision ||
            message.SourceNodeIds is null ||
            !message.SourceNodeIds.SequenceEqual(session.SourceNodeIds, StringComparer.Ordinal))
        {
            return;
        }

        var validation = DragProtocol.Validate(message, _id, _controller.Revision, _controller, BeforeDrop);
        if (!validation.Accepted || validation.SourceNodes is null || validation.TargetNode is null)
        {
            return;
        }

        var eventArgs = new TreeDropEventArgs(validation.SourceNodes, validation.TargetNode, validation.Relation);
        if (BeforeDropAsync is not null && !await BeforeDropAsync(eventArgs))
        {
            return;
        }

        if (_controller is null ||
            _controller.Revision != session.SourceRevision ||
            !ReferenceEquals(_controller.FindById(validation.TargetNode.Id), validation.TargetNode) ||
            validation.SourceNodes.Any(node => !ReferenceEquals(_controller.FindById(node.Id), node)))
        {
            return;
        }

        await OnDrop.InvokeAsync(eventArgs);
    }

    [JSInvokable]
    public void ShowPreview(int count, double clientX, double clientY)
    {
        if (_dragSession is null || !AllowDrag || _dragDisposed)
        {
            return;
        }

        isPreview = true;
        previewCount = count;
        PreviewX = clientX + previewOffset;
        PreviewY = clientY;
        StateHasChanged();
    }

    [JSInvokable]
    public void HidePreview()
    {
        InvalidateDragPreview(requestRender: true);
    }

    /// <summary>JS dragend 回调：结束拖拽会话并清理预览。</summary>
    [JSInvokable]
    public void ClearDrag()
    {
        _dragSession = null;
        InvalidateDragPreview(requestRender: true);
    }

    /// <summary>仅取消指定会话；陈旧 JS 回调不得清除后来建立的新会话。</summary>
    [JSInvokable]
    public void CancelDrag(string sessionId)
    {
        if (_dragSession is null || !string.Equals(sessionId, _dragSession.Id, StringComparison.Ordinal))
        {
            return;
        }

        _dragSession = null;
        InvalidateDragPreview(requestRender: true);
    }

    /// <summary>
    /// 原子消费活动会话。Blazor 事件分发在首次 await 前串行执行，因此先清空 id 可确保
    /// 同一 session 的重放或并发回调至多一个进入验证/业务通知。
    /// 匹配当前会话的拒绝或异常同样会消费该会话。
    /// </summary>
    private DragSession? ConsumeDragSession(string? sessionId)
    {
        var session = _dragSession;
        if (string.IsNullOrEmpty(sessionId) || session is null || !string.Equals(sessionId, session.Id, StringComparison.Ordinal))
        {
            return null;
        }

        _dragSession = null;
        InvalidateDragPreview(requestRender: true);
        return session;
    }

    private void InvalidateDragState(bool requestRender)
    {
        _dragSession = null;
        InvalidateDragPreview(requestRender);
    }

    private void InvalidateDragPreview(bool requestRender)
    {
        if (!isPreview)
        {
            return;
        }

        isPreview = false;
        if (requestRender)
        {
            StateHasChanged();
        }
    }

    private sealed record DragSession(string Id, long SourceRevision, IReadOnlyList<string> SourceNodeIds);
}
