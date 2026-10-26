namespace Astrea.Components.Tree;

/// <summary>编辑提交结果事件参数（节点 + 新值）。</summary>
public sealed class TreeEditEventArgs : EventArgs
{
    public TreeEditEventArgs(TreeNode node, string value)
    {
        Node = node;
        Value = value;
    }

    public TreeNode Node { get; }

    public string Value { get; }
}

/// <summary>编辑否决事件参数（节点 + 被否决的新值 + 反馈）。</summary>
public sealed class TreeEditRejectedEventArgs : EventArgs
{
    public TreeEditRejectedEventArgs(TreeNode node, string value, string? reason)
    {
        Node = node;
        Value = value;
        Reason = reason;
    }

    public TreeNode Node { get; }

    public string Value { get; }

    public string? Reason { get; }
}
