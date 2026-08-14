using System.Collections;

namespace Astrea.Components.Tree;

/// <summary>
/// 树节点：数据 + 视图状态载体 + 结构导航。
/// 不包含树级算法、选择缓存或业务耦合；这些由 <see cref="TreeController"/> 与
/// <see cref="TreeState"/> 负责。
/// </summary>
public class TreeNode : IEnumerable<TreeNode>
{
    private readonly List<TreeNode> _children = new();
    private string _cssClass = string.Empty;
    private string _text = string.Empty;

    public TreeNode(string text, string? icon = null, object? bindData = null, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("TreeNode text cannot be null or empty.", nameof(text));
        }

        // 稳定身份：应用提供 id 时沿用（跨重建保留）；未提供（null）时分配进程内唯一 id；显式空白 id 拒绝。
        Id = id is null
            ? Guid.NewGuid().ToString("N")
            : string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("TreeNode id cannot be empty or whitespace.", nameof(id))
                : id;
        _text = text;
        Icon = icon;
        BindData = bindData;
    }

    #region 身份与展示

    /// <summary>节点稳定标识（应用提供或自动分配）。</summary>
    public string Id { get; }

    /// <summary>节点显示文本（拒绝空或空白）。</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("TreeNode text cannot be null or empty.", nameof(value));
            }

            _text = value;
        }
    }

    /// <summary>绑定数据（领域对象，由消费方提供）。</summary>
    public object? BindData { get; set; }

    /// <summary>占位显示文本。</summary>
    public string? PlaceHolder { get; set; }

    #endregion

    #region 样式

    /// <summary>
    /// 节点样式：静态基础段（<see cref="SetCssClass"/>）+ 动态段（<see cref="CssClassResolver"/>）。
    /// </summary>
    public string CssClass
    {
        get
        {
            var resolved = CssClassResolver?.Invoke(this);
            if (string.IsNullOrEmpty(resolved))
            {
                return _cssClass;
            }

            return string.IsNullOrEmpty(_cssClass) ? resolved : $"{_cssClass} {resolved}";
        }
    }

    /// <summary>设置静态基础样式段。</summary>
    public void SetCssClass(string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            _cssClass = cssClass;
        }
    }

    /// <summary>动态样式解析器（消费方注入，如连接状态高亮）。</summary>
    public Func<TreeNode, string?>? CssClassResolver { get; set; }

    #endregion

    #region 结构导航

    public TreeNode? Parent { get; internal set; }

    public IReadOnlyList<TreeNode> Children => _children.AsReadOnly();

    /// <summary>节点在父元素中的索引。</summary>
    public int Index { get; internal set; }

    /// <summary>节点脱离树时的原父节点（焦点恢复锚点；重新挂接时清空）。</summary>
    internal TreeNode? DetachedParent { get; private set; }

    /// <summary>节点脱离树时在原父节点中的索引。</summary>
    internal int DetachedIndex { get; private set; }

    /// <summary>节点层级（0 表示根节点）。</summary>
    public int Level => Parent?.Level + 1 ?? 0;

    public bool IsRoot => Parent is null;

    public bool HasChildren => _children.Count > 0;

    public bool IsLeaf => !HasChildren;

    /// <summary>最上层根节点。</summary>
    public TreeNode Root => Parent?.Root ?? this;

    public int Count => _children.Count;

    public TreeNode this[int index] => _children[index];

    /// <summary>当前节点的所有兄弟节点（包括自身）。</summary>
    public IEnumerable<TreeNode> Siblings() => Parent?.Children ?? Enumerable.Empty<TreeNode>();

    /// <summary>当前节点到根节点的路径（不含自身，从父节点起）。</summary>
    public IEnumerable<TreeNode> GetPathToRoot()
    {
        var node = Parent;
        while (node is not null)
        {
            yield return node;
            node = node.Parent;
        }
    }

    /// <summary>当前节点是否是 <paramref name="node"/> 的后代（<paramref name="node"/> 是其祖先）。</summary>
    public bool IsDescendantOf(TreeNode node)
    {
        if (node is null || Parent is null)
        {
            return false;
        }

        for (var current = Parent; current is not null; current = current.Parent)
        {
            if (current == node)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>当前节点是否是 <paramref name="node"/> 的祖先（<paramref name="node"/> 是其后代）。</summary>
    public bool IsAncestorOf(TreeNode node)
    {
        if (node is null)
        {
            return false;
        }

        var stack = new Stack<TreeNode>();
        foreach (var child in _children)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == node)
            {
                return true;
            }

            foreach (var child in current.Children)
            {
                stack.Push(child);
            }
        }

        return false;
    }

    #endregion

    #region 结构变更

    public void Add(TreeNode child)
    {
        if (child is null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        GuardAgainstCycle(child);

        if (child.Parent == this)
        {
            return;
        }

        child.Parent?.Remove(child);
        child.Parent = this;
        child.DetachedParent = null;
        child.DetachedIndex = 0;
        child.Index = Count;
        _children.Add(child);
    }

    public void Remove(TreeNode child)
    {
        int index = IndexOf(child);
        if (index < 0)
        {
            return;
        }

        child.DetachedParent = this;
        child.DetachedIndex = index;
        child.Parent = null;
        _children.RemoveAt(index);

        for (int i = index; i < Count; i++)
        {
            _children[i].Index = i;
        }
    }

    public void Insert(TreeNode child, int index)
    {
        if (child is null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (index < 0 || index > _children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        GuardAgainstCycle(child);

        if (child.Parent == this)
        {
            Remove(child);
            if (index > _children.Count)
            {
                index = _children.Count;
            }
        }
        else
        {
            child.Parent?.Remove(child);
        }

        child.Parent = this;
        child.DetachedParent = null;
        child.DetachedIndex = 0;
        _children.Insert(index, child);

        for (int i = index; i < Count; i++)
        {
            _children[i].Index = i;
        }
    }

    public void InsertChildren(IEnumerable<TreeNode> children, int index)
    {
        foreach (var node in children)
        {
            Insert(node, index);
            index++;
        }
    }

    public void AddChildren(IEnumerable<TreeNode> children)
    {
        if (children is null)
        {
            throw new ArgumentNullException(nameof(children));
        }

        foreach (var node in children)
        {
            Add(node);
        }
    }

    public void RemoveChildren(IEnumerable<TreeNode> children)
    {
        if (children is null)
        {
            throw new ArgumentNullException(nameof(children));
        }

        foreach (var node in children)
        {
            Remove(node);
        }
    }

    private int IndexOf(TreeNode child) => _children.IndexOf(child);

    /// <summary>阻止把节点挂到自身或其自身后代之下，避免形成循环。</summary>
    private void GuardAgainstCycle(TreeNode child)
    {
        if (ReferenceEquals(child, this) || this.IsDescendantOf(child))
        {
            throw new InvalidOperationException("Cannot attach a node to itself or to one of its own descendants; this would create a cycle.");
        }
    }

    #endregion

    #region 图标

    /// <summary>节点图标（叶子或折叠态使用）。</summary>
    public string? Icon { get; set; }

    /// <summary>展开态节点图标（如打开的文件夹）。</summary>
    public string? UnFoldIcon { get; set; }

    /// <summary>展开/折叠控制图标（展开态）。</summary>
    public string? ExpandedIcon { get; set; }

    /// <summary>展开/折叠控制图标（折叠态）。</summary>
    public string? CollapsedIcon { get; set; }

    /// <summary>展开/折叠控制图标；叶子节点或禁用控制图标时为 null。</summary>
    public string? ExpandIcon
    {
        get
        {
            if (IsExpandIconDisable || IsLeaf)
            {
                return null;
            }

            return IsExpanded ? ExpandedIcon : CollapsedIcon;
        }
    }

    /// <summary>节点图标。</summary>
    public string? FoldIcon
    {
        get
        {
            if (IsFoldIconDisable || IsLeaf)
            {
                return Icon;
            }

            return IsExpanded ? UnFoldIcon ?? Icon : Icon;
        }
    }

    public bool IsExpandIconDisable { get; set; }

    public bool IsFoldIconDisable { get; set; }

    public bool IsExpandDisable { get; set; }

    #endregion

    #region 视图状态（由 Controller 维护，勿直接写）

    public bool IsExpanded { get; set; }

    /// <summary>选中状态（派生自所属 Controller 的状态，非权威；未绑定时为 false）。</summary>
    public bool IsSelected => Controller?.IsSelected(Id) ?? false;

    /// <summary>焦点状态（派生自所属 Controller 的状态，非权威；未绑定时为 false）。</summary>
    public bool IsFocused => Controller?.IsFocused(Id) ?? false;

    public bool IsEditing { get; internal set; }

    public bool IsDisabled { get; set; }

    #endregion

    #region 克隆

    /// <summary>深克隆子树（BindData 为浅拷贝引用）。</summary>
    public object Clone()
    {
        var clone = DuplicateNode(this);
        foreach (var child in Children)
        {
            clone.Add((TreeNode)child.Clone());
        }

        return clone;
    }

    /// <summary>复制单个节点字段（不复制子节点与 Id）。</summary>
    public static TreeNode DuplicateNode(TreeNode node)
    {
        var clone = new TreeNode(node.Text, node.Icon, node.BindData)
        {
            PlaceHolder = node.PlaceHolder,
            UnFoldIcon = node.UnFoldIcon,
            ExpandedIcon = node.ExpandedIcon,
            CollapsedIcon = node.CollapsedIcon,
            IsExpanded = node.IsExpanded,
            IsEditing = node.IsEditing,
            IsDisabled = node.IsDisabled,
            IsExpandIconDisable = node.IsExpandIconDisable,
            IsFoldIconDisable = node.IsFoldIconDisable,
            IsExpandDisable = node.IsExpandDisable,
            CssClassResolver = node.CssClassResolver,
        };
        clone._cssClass = node._cssClass;
        return clone;
    }

    #endregion

    #region 门面方法（委托 Controller，未绑定时 no-op）

    /// <summary>所属控制器（绑定 DataSource 时由 <see cref="TreeController"/> 设置）。</summary>
    internal TreeController? Controller { get; set; }

    /// <summary>获取整树选中节点（树顺序）。</summary>
    public IEnumerable<TreeNode> GetSelectedNodes() => Controller?.GetSelectedNodes() ?? Enumerable.Empty<TreeNode>();

    /// <summary>设置当前节点为焦点。</summary>
    public void SetFocus() => Controller?.SetFocus(this);

    /// <summary>清除焦点。</summary>
    public void UnFocus() => Controller?.ClearFocus();

    /// <summary>清除当前节点及其后代的选中状态。</summary>
    public void UnSelectAll() => Controller?.ClearSelectionSubtree(this);

    /// <summary>选中当前节点可见后代（排除自身）。</summary>
    public void SelectAllExcludeSelf() => Controller?.SelectAll(this, excludeSelf: true);

    /// <summary>切换展开/折叠状态。</summary>
    public void Toggle()
    {
        if (IsExpandDisable)
        {
            return;
        }

        if (Controller is not null)
        {
            if (IsExpanded)
            {
                Controller.Collapse(this);
            }
            else
            {
                Controller.Expand(this);
            }
        }
        else
        {
            IsExpanded = !IsExpanded;
        }
    }

    public void ExpandAll() => Controller?.ExpandAll(this);

    public void CollapseAll() => Controller?.CollapseAll(this);

    public void ExpandAllParents() => Controller?.ExpandAllParents(this);

    /// <summary>按稳定身份查找。</summary>
    public TreeNode? FindNode(string id) => Controller?.FindById(id);

    /// <summary>按绑定数据查找。</summary>
    public TreeNode? FindNode(object? bindData) => Controller?.FindByBindData(bindData);

    /// <summary>按绑定数据集合查找。</summary>
    public IEnumerable<TreeNode> FindNodes(IEnumerable<object>? bindDatas)
        => Controller?.FindAll(n => bindDatas?.Contains(n.BindData) == true) ?? Enumerable.Empty<TreeNode>();

    #endregion

    public IEnumerator<TreeNode> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => Text;
}
