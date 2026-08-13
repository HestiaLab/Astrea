namespace Astrea.Components.Tree;

/// <summary>
/// 选择与焦点的唯一真相来源。
/// 节点上的 <see cref="TreeNode.IsSelected"/> / <see cref="TreeNode.IsFocused"/> 由本类维护，
/// <see cref="SelectedNodes"/> 是与其始终一致的派生缓存（HashSet，O(1) 成员判定）。
/// 全树仅允许一个焦点节点。
/// </summary>
public sealed class TreeSelection
{
    private readonly HashSet<TreeNode> _selected = new();
    private TreeNode? _focused;

    /// <summary>当前焦点节点（全树唯一，可能为 null）。</summary>
    public TreeNode? FocusedNode => _focused;

    /// <summary>选中节点缓存，与节点 <see cref="TreeNode.IsSelected"/> 状态始终一致。</summary>
    public IReadOnlyCollection<TreeNode> SelectedNodes => _selected;

    /// <summary>节点是否被选中（O(1)）。</summary>
    public bool IsSelected(TreeNode node) => _selected.Contains(node);

    /// <summary>节点是否拥有焦点（O(1)）。</summary>
    public bool IsFocused(TreeNode node) => ReferenceEquals(_focused, node);

    /// <summary>单选：清空现有选择后选中指定节点。</summary>
    public void Select(TreeNode node)
    {
        ClearSelection();
        AddToSelection(node);
    }

    /// <summary>追加选中（幂等）。</summary>
    public void AddToSelection(TreeNode node)
    {
        if (node.IsSelected)
        {
            return;
        }

        node.IsSelected = true;
        _selected.Add(node);
    }

    /// <summary>从选中集合移除。</summary>
    public void RemoveFromSelection(TreeNode node)
    {
        if (!node.IsSelected)
        {
            return;
        }

        node.IsSelected = false;
        _selected.Remove(node);
    }

    /// <summary>切换选中状态。</summary>
    public void ToggleSelection(TreeNode node)
    {
        if (node.IsSelected)
        {
            RemoveFromSelection(node);
        }
        else
        {
            AddToSelection(node);
        }
    }

    /// <summary>替换整个选中集合。</summary>
    public void ReplaceSelection(IEnumerable<TreeNode> nodes)
    {
        ClearSelection();
        foreach (var node in nodes)
        {
            AddToSelection(node);
        }
    }

    /// <summary>清空全部选中。</summary>
    public void ClearSelection()
    {
        foreach (var node in _selected)
        {
            node.IsSelected = false;
        }

        _selected.Clear();
    }

    /// <summary>设置焦点节点（维护全树唯一焦点不变量）。</summary>
    public void SetFocus(TreeNode node)
    {
        if (ReferenceEquals(_focused, node))
        {
            return;
        }

        if (_focused is not null)
        {
            _focused.IsFocused = false;
        }

        _focused = node;
        node.IsFocused = true;
    }

    /// <summary>清除焦点。</summary>
    public void ClearFocus()
    {
        if (_focused is not null)
        {
            _focused.IsFocused = false;
        }

        _focused = null;
    }
}
