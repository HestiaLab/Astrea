using System.Text.Json.Serialization;

namespace Astrea.Components.Tree;

/// <summary>拖拽放置位置（之前/之内/之后）。</summary>
public enum TreeDropRelation
{
    Before,
    Inside,
    After,
}

/// <summary>
/// 拖拽协议消息（JS 与 C# 之间只使用这一份事实）。
/// relation 只允许 before/inside/after；未知值必须拒绝，不得默认降级。
/// </summary>
public sealed class TreeDragMessage
{
    public const int CurrentProtocolVersion = 1;

    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; } = CurrentProtocolVersion;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("sourceTreeId")]
    public string SourceTreeId { get; set; } = string.Empty;

    [JsonPropertyName("sourceNodeIds")]
    public List<string> SourceNodeIds { get; set; } = new();

    [JsonPropertyName("sourceRevision")]
    public long SourceRevision { get; set; }

    [JsonPropertyName("targetTreeId")]
    public string TargetTreeId { get; set; } = string.Empty;

    [JsonPropertyName("targetNodeId")]
    public string TargetNodeId { get; set; } = string.Empty;

    [JsonPropertyName("relation")]
    public string Relation { get; set; } = string.Empty;

    /// <summary>严格解析 relation 字符串；未知/空白值返回 false（不回退默认值）。</summary>
    public static bool TryParseRelation(string? relation, out TreeDropRelation value)
    {
        switch (relation)
        {
            case "before":
                value = TreeDropRelation.Before;
                return true;
            case "inside":
                value = TreeDropRelation.Inside;
                return true;
            case "after":
                value = TreeDropRelation.After;
                return true;
            default:
                value = default;
                return false;
        }
    }
}

/// <summary>BeginDrag 的返回载荷；wire name 与 JS 读取的 camelCase 字段固定绑定。</summary>
internal sealed class TreeDragBeginResult
{
    [JsonPropertyName("sourceRevision")]
    public long SourceRevision { get; init; }

    [JsonPropertyName("sourceNodeIds")]
    public IReadOnlyList<string> SourceNodeIds { get; init; } = Array.Empty<string>();
}

/// <summary>拖拽放置结果事件参数（协议、结构规则与 guard 全部通过后才触发一次）。</summary>
public sealed class TreeDropEventArgs : EventArgs
{
    public TreeDropEventArgs(IReadOnlyList<TreeNode> sourceNodes, TreeNode targetNode, TreeDropRelation relation)
    {
        SourceNodes = sourceNodes;
        TargetNode = targetNode;
        Relation = relation;
    }

    /// <summary>实际拖拽源节点（已归一化：祖先与后代同时选中时只保留最高层祖先）。</summary>
    public IReadOnlyList<TreeNode> SourceNodes { get; }

    public TreeNode TargetNode { get; }

    public TreeDropRelation Relation { get; }
}

/// <summary>拖拽协议与源归一化的纯逻辑（独立于组件生命周期，便于单测）。</summary>
internal static class DragProtocol
{
    /// <summary>
    /// 源归一化：集合同时包含互为祖先/后代的节点时，只保留最高层祖先作为实际拖拽源（REQ-F-087 AC6）。
    /// </summary>
    public static List<TreeNode> NormalizeSources(IReadOnlyCollection<TreeNode> nodes)
    {
        if (nodes is null || nodes.Count == 0)
        {
            return new List<TreeNode>();
        }

        var sourceSet = new HashSet<TreeNode>(nodes, ReferenceEqualityComparer.Instance);
        return nodes
            .Where(node =>
            {
                for (var ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
                {
                    if (sourceSet.Contains(ancestor))
                    {
                        return false;
                    }
                }

                return true;
            })
            .ToList();
    }

    /// <summary>
    /// 完整校验一条拖拽消息：协议版本、树归属、revision、relation、源/目标解析、结构规则与 guard。
    /// 任何一步失败都确定性拒绝，不产生部分效果。
    /// </summary>
    public static TreeDropValidation Validate(
        TreeDragMessage message,
        string treeId,
        long currentRevision,
        TreeController controller,
        Func<TreeDropEventArgs, bool>? beforeDrop)
    {
        if (message is null)
        {
            return TreeDropValidation.Reject("missing message");
        }

        if (message.ProtocolVersion != TreeDragMessage.CurrentProtocolVersion)
        {
            return TreeDropValidation.Reject("protocol version mismatch");
        }

        if (message.SourceTreeId != treeId || message.TargetTreeId != treeId)
        {
            return TreeDropValidation.Reject("cross-tree drop");
        }

        if (message.SourceRevision != currentRevision)
        {
            return TreeDropValidation.Reject("stale revision");
        }

        if (!TreeDragMessage.TryParseRelation(message.Relation, out var relation))
        {
            return TreeDropValidation.Reject("unknown relation");
        }

        if (message.SourceNodeIds is null || message.SourceNodeIds.Count == 0)
        {
            return TreeDropValidation.Reject("empty source");
        }

        var sourceNodes = new List<TreeNode>(message.SourceNodeIds.Count);
        var seen = new HashSet<string>();
        foreach (var id in message.SourceNodeIds)
        {
            var node = controller.FindById(id);
            if (node is null)
            {
                return TreeDropValidation.Reject("unknown source node");
            }

            if (!seen.Add(id))
            {
                return TreeDropValidation.Reject("duplicate source node");
            }

            sourceNodes.Add(node);
        }

        var normalized = NormalizeSources(sourceNodes);

        var target = controller.FindById(message.TargetNodeId);
        if (target is null)
        {
            return TreeDropValidation.Reject("unknown target node");
        }

        if (!controller.CanDrop(normalized, target))
        {
            return TreeDropValidation.Reject("invalid drop (self/cycle)");
        }

        var args = new TreeDropEventArgs(normalized, target, relation);
        if (beforeDrop?.Invoke(args) == false)
        {
            return TreeDropValidation.Reject("vetoed by BeforeDrop");
        }

        return TreeDropValidation.Accept(normalized, target, relation);
    }
}

/// <summary>拖拽消息验证结果。</summary>
internal sealed class TreeDropValidation
{
    private TreeDropValidation(bool accepted, IReadOnlyList<TreeNode>? sourceNodes, TreeNode? targetNode, TreeDropRelation relation, string? rejectionReason)
    {
        Accepted = accepted;
        SourceNodes = sourceNodes;
        TargetNode = targetNode;
        Relation = relation;
        RejectionReason = rejectionReason;
    }

    public bool Accepted { get; }

    public IReadOnlyList<TreeNode>? SourceNodes { get; }

    public TreeNode? TargetNode { get; }

    public TreeDropRelation Relation { get; }

    public string? RejectionReason { get; }

    public static TreeDropValidation Accept(IReadOnlyList<TreeNode> sourceNodes, TreeNode targetNode, TreeDropRelation relation)
        => new(true, sourceNodes, targetNode, relation, null);

    public static TreeDropValidation Reject(string reason)
        => new(false, null, null, default, reason);
}
