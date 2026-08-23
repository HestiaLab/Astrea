using Microsoft.AspNetCore.Components.Web;

namespace Astrea.Components.Tree;

/// <summary>携带单个节点的通用事件参数。</summary>
public sealed class TreeNodeEventArgs : EventArgs
{
    public TreeNodeEventArgs(TreeNode node) => Node = node;

    public TreeNode Node { get; }
}

/// <summary>选择集合变化事件参数（仅在用户操作实际改变选择后触发一次）。</summary>
public sealed class TreeSelectionChangedEventArgs : EventArgs
{
    public TreeSelectionChangedEventArgs(IReadOnlyCollection<string> oldSelectedIds, IReadOnlyCollection<string> selectedIds)
    {
        OldSelectedIds = oldSelectedIds;
        SelectedIds = selectedIds;
    }

    /// <summary>变化前的选中节点稳定 ID 集合。</summary>
    public IReadOnlyCollection<string> OldSelectedIds { get; }

    /// <summary>变化后的选中节点稳定 ID 集合。</summary>
    public IReadOnlyCollection<string> SelectedIds { get; }
}

/// <summary>展开/折叠状态变化事件参数。</summary>
public sealed class TreeExpandedChangedEventArgs : EventArgs
{
    public TreeExpandedChangedEventArgs(TreeNode node, bool isExpanded)
    {
        Node = node;
        IsExpanded = isExpanded;
    }

    public TreeNode Node { get; }

    public bool IsExpanded { get; }
}

/// <summary>右键上下文通知事件参数（Tree 仅通知，不渲染菜单，也不改变选中/焦点）。</summary>
public sealed class TreeContextMenuEventArgs : EventArgs
{
    public TreeContextMenuEventArgs(TreeNode node, MouseEventArgs mouseArgs)
    {
        Node = node;
        MouseArgs = mouseArgs;
    }

    public TreeNode Node { get; }

    public MouseEventArgs MouseArgs { get; }
}
