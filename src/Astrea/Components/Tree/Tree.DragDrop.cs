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

    // 活动拖拽会话 id（C# 记录，drop 时校验消息归属同一会话）
    private string? _dragSessionId;

    private async Task ToggleNode(TreeNode node)
    {
        if (_controller is null)
        {
            return;
        }

        if (node.IsExpanded)
        {
            if (_controller.Collapse(node))
            {
                await NotifyExpansionChangedAsync(node, isExpanded: false);
            }
        }
        else
        {
            if (_controller.Expand(node))
            {
                await NotifyExpansionChangedAsync(node, isExpanded: true);
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
        if (_controller is null)
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
            if (node is null)
            {
                return "{}";
            }

            if (!seen.Add(id))
            {
                continue;
            }

            nodes.Add(node);
        }

        var normalized = DragProtocol.NormalizeSources(nodes);

        _dragSessionId = sessionId;

        return JsonSerializer.Serialize(new { sourceRevision = _controller.Revision, sourceNodeIds = normalized.Select(n => n.Id).ToList() });
    }

    /// <summary>
    /// JS drop 回调：解析并完整校验拖拽消息（协议/树归属/revision/relation/源目标/结构/guard），
    /// 全部通过后才触发一次 OnDrop。未知、陈旧、重复、跨 Tree 或非法消息确定性拒绝。
    /// </summary>
    [JSInvokable]
    public async Task HandleDrop(string messageJson)
    {
        if (_controller is null)
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

        if (message is null || message.SessionId != _dragSessionId)
        {
            return;
        }

        var validation = DragProtocol.Validate(message, _id, _controller.Revision, _controller, BeforeDrop);
        if (!validation.Accepted || validation.SourceNodes is null || validation.TargetNode is null)
        {
            return;
        }

        await OnDrop.InvokeAsync(new TreeDropEventArgs(validation.SourceNodes, validation.TargetNode, validation.Relation));
    }

    [JSInvokable]
    public void ShowPreview(int count, double clientX, double clientY)
    {
        isPreview = true;
        previewCount = count;
        PreviewX = clientX + previewOffset;
        PreviewY = clientY;
        StateHasChanged();
    }

    [JSInvokable]
    public void UpdatePreviewPos(double clientX, double clientY)
    {
        PreviewX = clientX + previewOffset;
        PreviewY = clientY;
        StateHasChanged();
    }

    [JSInvokable]
    public void HidePreview()
    {
        isPreview = false;
        StateHasChanged();
    }

    /// <summary>JS dragend 回调：结束拖拽会话并清理预览。</summary>
    [JSInvokable]
    public void ClearDrag()
    {
        _dragSessionId = null;
        HidePreview();
    }
}
