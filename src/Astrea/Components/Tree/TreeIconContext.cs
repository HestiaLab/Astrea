namespace Astrea.Components.Tree;

/// <summary>节点图标渲染上下文（技术中立，装饰图标应 aria-hidden）。</summary>
public sealed class TreeIconContext
{
    public TreeIconContext(TreeNode node, bool isExpanded, bool hasChildren, bool isDisabled)
    {
        Node = node;
        IsExpanded = isExpanded;
        HasChildren = hasChildren;
        IsDisabled = isDisabled;
    }

    public TreeNode Node { get; }

    public bool IsExpanded { get; }

    public bool HasChildren { get; }

    public bool IsDisabled { get; }
}
