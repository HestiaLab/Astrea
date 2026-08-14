namespace Astrea.Components.Tree;

/// <summary>
/// Tree 交互状态的唯一真相来源，按稳定 ID 持有选中与焦点状态，并跟踪结构 revision。
/// 纯状态容器，不做节点归属、禁用状态等校验；校验由 <see cref="TreeController"/> 在写入前完成。
/// 选择/焦点不再以 <see cref="TreeNode"/> 实例属性为权威。
/// </summary>
public sealed class TreeState
{
    private readonly HashSet<string> _selectedIds = new();
    private string? _focusedId;
    private long _revision;

    /// <summary>选中节点的稳定 ID 集合。</summary>
    public IReadOnlySet<string> SelectedIds => _selectedIds;

    /// <summary>当前焦点节点稳定 ID（全树唯一，可能为 null）。</summary>
    public string? FocusedId => _focusedId;

    /// <summary>结构 revision，每次结构刷新递增。</summary>
    public long Revision => _revision;

    public bool IsSelected(string id) => id is not null && _selectedIds.Contains(id);

    public bool IsFocused(string id) => id is not null && _focusedId == id;

    /// <summary>单选：清空现有选择后选中指定 ID。</summary>
    public bool Select(string id)
    {
        if (id is null)
        {
            return false;
        }

        if (_selectedIds.Count == 1 && _selectedIds.Contains(id))
        {
            return false;
        }

        _selectedIds.Clear();
        _selectedIds.Add(id);
        return true;
    }

    /// <summary>追加选中（幂等）。</summary>
    public bool AddToSelection(string id) => id is not null && _selectedIds.Add(id);

    /// <summary>移除选中。</summary>
    public bool RemoveFromSelection(string id) => id is not null && _selectedIds.Remove(id);

    /// <summary>切换选中状态。</summary>
    public bool ToggleSelection(string id) => IsSelected(id) ? RemoveFromSelection(id) : AddToSelection(id);

    /// <summary>替换整个选中集合。</summary>
    public bool ReplaceSelection(IEnumerable<string> ids)
    {
        var next = ids.Where(id => id is not null).ToHashSet();
        var changed = !_selectedIds.SetEquals(next);
        _selectedIds.Clear();
        _selectedIds.UnionWith(next);
        return changed;
    }

    /// <summary>清空选中。</summary>
    public bool ClearSelection()
    {
        if (_selectedIds.Count == 0)
        {
            return false;
        }

        _selectedIds.Clear();
        return true;
    }

    /// <summary>设置焦点 ID（维护全树唯一焦点不变量）。</summary>
    public bool SetFocus(string? id)
    {
        if (_focusedId == id)
        {
            return false;
        }

        _focusedId = id;
        return true;
    }

    public bool ClearFocus() => SetFocus(null);

    /// <summary>选中集合与给定有效 ID 求交集（移除已被删除节点的选中）。</summary>
    public void RetainSelected(IEnumerable<string> validIds)
    {
        var valid = validIds as ISet<string> ?? validIds.ToHashSet();
        _selectedIds.IntersectWith(valid);
    }

    /// <summary>焦点 ID 不在有效集合中时清空焦点。</summary>
    public void RetainFocus(IEnumerable<string> validIds)
    {
        var valid = validIds as ISet<string> ?? validIds.ToHashSet();
        if (_focusedId is not null && !valid.Contains(_focusedId))
        {
            _focusedId = null;
        }
    }

    /// <summary>结构刷新后递增 revision。</summary>
    public void IncrementRevision() => _revision++;
}
