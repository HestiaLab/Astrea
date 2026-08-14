namespace Astrea.Components.Tree;

/// <summary>
/// 树控制器：持有根集合，维护 O(1) 节点索引、扁平化缓存、选择与展开操作、拖拽校验。
/// 纯 C#，不依赖 Blazor 渲染，可独立单测。交互状态的唯一写入口（写前校验节点归属与禁用状态）。
/// </summary>
public sealed class TreeController
{
    private readonly TreeState _state = new();
    private IReadOnlyList<TreeNode> _roots;
    private Dictionary<string, TreeNode>? _index;
    private List<TreeNode>? _flatten;
    private List<TreeNode>? _flattenVisible;
    private TreeSelectionMode _selectionMode;

    public TreeController(
        IReadOnlyList<TreeNode> roots,
        TreeSelectionMode selectionMode = TreeSelectionMode.Multiple)
    {
        _roots = roots ?? throw new ArgumentNullException(nameof(roots));
        _selectionMode = selectionMode;
        Refresh();
    }

    internal void SetSelectionMode(TreeSelectionMode selectionMode) => _selectionMode = selectionMode;

    /// <summary>根节点集合。</summary>
    public IReadOnlyList<TreeNode> Roots => _roots;

    /// <summary>否决展开（返回 false 阻止）。</summary>
    public Func<TreeNode, bool>? BeforeExpand { get; set; }

    /// <summary>否决折叠（返回 false 阻止）。</summary>
    public Func<TreeNode, bool>? BeforeCollapse { get; set; }

    /// <summary>否决重命名（参数：节点、新值）。</summary>
    public Func<TreeNode, string, bool>? BeforeRename { get; set; }

    /// <summary>否决进入编辑态。</summary>
    public Func<TreeNode, bool>? BeforeEditName { get; set; }

    /// <summary>选中节点的稳定 ID 集合（只读快照）。</summary>
    public IReadOnlyCollection<string> SelectedIds => _state.SelectedIds;

    /// <summary>当前焦点节点稳定 ID。</summary>
    public string? FocusedId => _state.FocusedId;

    /// <summary>当前焦点节点（可能为 null）。</summary>
    public TreeNode? FocusedNode => FocusedId is null ? null : FindById(FocusedId);

    /// <summary>结构 revision（每次 Refresh 递增，用于拒绝陈旧拖拽会话）。</summary>
    public long Revision => _state.Revision;

    /// <summary>供 <see cref="TreeNode"/> 派生的选中状态读取（非权威入口）。</summary>
    internal bool IsSelected(string id) => _state.IsSelected(id);

    /// <summary>供 <see cref="TreeNode"/> 派生的焦点状态读取（非权威入口）。</summary>
    internal bool IsFocused(string id) => _state.IsFocused(id);

    #region 索引与查询

    /// <summary>按稳定身份查找（O(1)）。</summary>
    public TreeNode? FindById(string id)
    {
        if (id is null || _index is null)
        {
            return null;
        }

        return _index.TryGetValue(id, out var node) ? node : null;
    }

    /// <summary>按绑定数据查找（O(n)，引用相等）。</summary>
    public TreeNode? FindByBindData(object? bindData)
        => bindData is null ? null : Flatten().FirstOrDefault(n => n.BindData == bindData);

    /// <summary>按文本查找（O(n)）。</summary>
    public TreeNode? FindByText(string text) => Flatten().FirstOrDefault(n => n.Text == text);

    /// <summary>按谓词查找全部（O(n)）。</summary>
    public IEnumerable<TreeNode> FindAll(Func<TreeNode, bool> predicate) => Flatten().Where(predicate);

    #endregion

    #region 扁平化

    /// <summary>全部节点（DFS 顺序，含折叠子树）。</summary>
    public IReadOnlyList<TreeNode> Flatten()
    {
        if (_flatten is null)
        {
            _flatten = _roots.SelectMany(EnumerateSubtree).ToList();
        }

        return _flatten;
    }

    /// <summary>可见节点（沿展开路径的 DFS 序列，包含全部根节点）。</summary>
    public IReadOnlyList<TreeNode> FlattenVisible()
    {
        if (_flattenVisible is null)
        {
            _flattenVisible = _roots.SelectMany(EnumerateVisibleSubtree).ToList();
        }

        return _flattenVisible;
    }

    /// <summary>重建索引与缓存，整树校验后原子提交；清理悬空选择并恢复丢失的焦点。</summary>
    public void Refresh()
    {
        var previousFocusedNode = FocusedNode;
        var previousFocusedId = _state.FocusedId;

        // 整树校验 + 构建新索引（任何不变量违规都在这里抛出，不产生部分提交）
        var (index, flatten) = BuildIndexAndValidate(_roots);

        _index = index;
        _flatten = flatten;
        _flattenVisible = null;

        foreach (var node in flatten)
        {
            node.Controller = this;
        }

        // 悬空选择按稳定 ID 与当前索引求交集（不视为用户操作）
        _state.RetainSelected(index.Keys);

        // 焦点恢复（REQ-F-077）：焦点节点已移除或不再可见时，恢复到最近可见目标
        if (previousFocusedId is not null)
        {
            var visible = FlattenVisible();
            if (previousFocusedNode is null || !visible.Contains(previousFocusedNode))
            {
                RestoreFocus(previousFocusedNode, visible);
            }
        }

        _state.IncrementRevision();
    }

    /// <summary>按稳定 Id 恢复展开、选中、焦点状态（DataSource 替换后由组件调用，REQ-F-061 AC2）。</summary>
    public void RestoreState(IEnumerable<string>? selectedIds, IEnumerable<string>? expandedIds, string? focusId)
    {
        if (expandedIds is not null)
        {
            var expanded = expandedIds as ICollection<string> ?? expandedIds.ToHashSet();
            foreach (var node in Flatten())
            {
                if (expanded.Contains(node.Id))
                {
                    node.IsExpanded = true;
                }
            }

            InvalidateVisible();
        }

        if (selectedIds is not null)
        {
            var selected = selectedIds as ICollection<string> ?? selectedIds.ToHashSet();
            var existing = Flatten().Where(n => selected.Contains(n.Id)).Select(n => n.Id);
            _state.ReplaceSelection(existing);
        }

        if (focusId is not null)
        {
            if (_index is not null && _index.ContainsKey(focusId))
            {
                _state.SetFocus(focusId);
            }
            else
            {
                _state.ClearFocus();
            }
        }
    }

    /// <summary>失效可见节点缓存（外部直接修改展开态后调用）。</summary>
    public void InvalidateVisible() => _flattenVisible = null;

    /// <summary>整树校验：重复 Id、重复节点实例/循环、Parent/Index 不一致均抛确定性异常。</summary>
    private static (Dictionary<string, TreeNode> Index, List<TreeNode> Flatten) BuildIndexAndValidate(IReadOnlyList<TreeNode> roots)
    {
        var index = new Dictionary<string, TreeNode>();
        var flatten = new List<TreeNode>();
        var visited = new HashSet<TreeNode>();

        void Walk(TreeNode node)
        {
            if (!visited.Add(node))
            {
                throw new TreeStructureException($"Tree node '{node.Text}' appears more than once (duplicate instance or cycle).");
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (!ReferenceEquals(child.Parent, node))
                {
                    throw new TreeStructureException($"Tree node '{child.Text}' has an inconsistent Parent reference.");
                }

                if (child.Index != i)
                {
                    throw new TreeStructureException($"Tree node '{child.Text}' has an inconsistent Index ({child.Index} != {i}).");
                }
            }

            if (!index.TryAdd(node.Id, node))
            {
                throw new TreeStructureException($"Duplicate node Id '{node.Id}' detected.");
            }

            flatten.Add(node);

            foreach (var child in node.Children)
            {
                Walk(child);
            }
        }

        foreach (var root in roots)
        {
            if (root.Parent is not null)
            {
                throw new TreeStructureException($"Tree root '{root.Text}' has a non-null Parent reference.");
            }

            Walk(root);
        }

        return (index, flatten);
    }

    /// <summary>焦点节点脱离可见树后，恢复到最近可见目标。</summary>
    private void RestoreFocus(TreeNode? lost, IReadOnlyList<TreeNode> visible)
    {
        // 锚点：脱离时的父节点（仍在树内）
        var anchor = lost?.DetachedParent;
        if (anchor is not null && _index is not null && _index.ContainsKey(anchor.Id))
        {
            if (visible.Contains(anchor))
            {
                var children = anchor.Children;
                int idx = lost!.DetachedIndex;
                TreeNode? target;
                if (idx >= 0 && idx < children.Count)
                {
                    target = children[idx]; // 下一个可见同级
                }
                else if (idx - 1 >= 0 && idx - 1 < children.Count)
                {
                    target = children[idx - 1]; // 上一个可见同级
                }
                else
                {
                    target = anchor;
                }

                _state.SetFocus(target.Id);
                return;
            }

            // 锚点不可见（被折叠）→ 最近可见祖先
            for (var p = anchor.Parent; p is not null; p = p.Parent)
            {
                if (visible.Contains(p))
                {
                    _state.SetFocus(p.Id);
                    return;
                }
            }
        }

        // 兜底：第一个可见节点；无可见节点则清空焦点
        if (visible.Count > 0)
        {
            _state.SetFocus(visible[0].Id);
        }
        else
        {
            _state.ClearFocus();
        }
    }

    #endregion

    #region 展开与折叠

    /// <summary>展开节点（受 BeforeExpand 否决）。</summary>
    public bool Expand(TreeNode node)
    {
        if (!IsOwnedNode(node) || node.IsLeaf || node.IsDisabled)
        {
            return false;
        }

        if (BeforeExpand?.Invoke(node) == false)
        {
            return false;
        }

        node.IsExpanded = true;
        InvalidateVisible();
        return true;
    }

    /// <summary>折叠节点（受 BeforeCollapse 否决；焦点在被折叠分支内时移到折叠节点）。</summary>
    public bool Collapse(TreeNode node)
    {
        if (!IsOwnedNode(node) || node.IsLeaf || node.IsDisabled)
        {
            return false;
        }

        if (BeforeCollapse?.Invoke(node) == false)
        {
            return false;
        }

        var focused = FocusedNode;
        node.IsExpanded = false;
        InvalidateVisible();

        // 焦点恢复：焦点位于被折叠分支内时，移到折叠节点（REQ-F-077 AC4）
        if (focused is not null && focused != node && focused.IsDescendantOf(node))
        {
            _state.SetFocus(node.Id);
        }

        return true;
    }

    /// <summary>展开全部（指定子树或整树）。</summary>
    public void ExpandAll(TreeNode? root = null)
    {
        foreach (var node in EnumerateNodes(root))
        {
            if (node.HasChildren)
            {
                node.IsExpanded = true;
            }
        }

        InvalidateVisible();
    }

    /// <summary>折叠全部（指定子树或整树）。</summary>
    public void CollapseAll(TreeNode? root = null)
    {
        foreach (var node in EnumerateNodes(root))
        {
            if (node.HasChildren)
            {
                node.IsExpanded = false;
            }
        }

        InvalidateVisible();
    }

    /// <summary>展开从节点到根的全部祖先。</summary>
    public void ExpandAllParents(TreeNode node)
    {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }

        InvalidateVisible();
    }

    #endregion

    #region 选择

    /// <summary>单选：替换选中（禁用节点或外部节点被拒绝）。</summary>
    public TreeOperationResult Select(TreeNode node)
    {
        if (!IsOwnedNode(node) || node.IsDisabled)
        {
            return TreeOperationResult.RejectedResult;
        }

        return TreeOperationResult.From(_state.Select(node.Id));
    }

    /// <summary>追加选中（禁用节点或外部节点被拒绝）。</summary>
    public TreeOperationResult AddToSelection(TreeNode node)
    {
        if (!IsOwnedNode(node) || node.IsDisabled)
        {
            return TreeOperationResult.RejectedResult;
        }

        return _selectionMode == TreeSelectionMode.Single
            ? TreeOperationResult.From(_state.Select(node.Id))
            : TreeOperationResult.From(_state.AddToSelection(node.Id));
    }

    /// <summary>切换选中（禁用节点或外部节点被拒绝）。</summary>
    public TreeOperationResult ToggleSelection(TreeNode node)
    {
        if (!IsOwnedNode(node) || node.IsDisabled)
        {
            return TreeOperationResult.RejectedResult;
        }

        if (_selectionMode == TreeSelectionMode.Single)
        {
            return _state.IsSelected(node.Id)
                ? TreeOperationResult.From(_state.ClearSelection())
                : TreeOperationResult.From(_state.Select(node.Id));
        }

        return TreeOperationResult.From(_state.ToggleSelection(node.Id));
    }

    /// <summary>替换整个选中集合（任一节点归属无效或禁用时整体拒绝，原子提交）。</summary>
    public TreeOperationResult ReplaceSelection(IEnumerable<TreeNode> nodes)
    {
        var list = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
        foreach (var node in list)
        {
            if (!IsOwnedNode(node) || node.IsDisabled)
            {
                return TreeOperationResult.RejectedResult;
            }
        }

        if (_selectionMode == TreeSelectionMode.Single && list.Select(node => node.Id).Distinct().Skip(1).Any())
        {
            return TreeOperationResult.RejectedResult;
        }

        return TreeOperationResult.From(_state.ReplaceSelection(list.Select(n => n.Id)));
    }

    public TreeOperationResult ClearSelection() => TreeOperationResult.From(_state.ClearSelection());

    /// <summary>收敛选择集合为单节点（关闭多选时调用）：焦点节点若已选中则保留，否则清空。</summary>
    public TreeOperationResult ConvergeToSingleSelection()
    {
        var selected = GetSelectedNodes();
        if (selected.Count <= 1)
        {
            return TreeOperationResult.NoOpResult;
        }

        var focused = FocusedNode;
        if (focused is not null && _state.IsSelected(focused.Id))
        {
            _state.ReplaceSelection(new[] { focused.Id });
        }
        else
        {
            _state.ClearSelection();
        }

        return TreeOperationResult.ChangedResult;
    }

    /// <summary>Shift 区间选择。</summary>
    public TreeOperationResult SelectRange(TreeNode from, TreeNode to)
    {
        if (_selectionMode == TreeSelectionMode.Single)
        {
            return ReferenceEquals(from, to) ? Select(to) : TreeOperationResult.RejectedResult;
        }

        if (!IsOwnedNode(from) || !IsOwnedNode(to) || from.IsDisabled || to.IsDisabled)
        {
            return TreeOperationResult.RejectedResult;
        }

        var visible = FlattenVisible();
        var fromIndex = IndexOfReference(visible, from);
        var toIndex = IndexOfReference(visible, to);
        if (fromIndex < 0 || toIndex < 0)
        {
            return TreeOperationResult.RejectedResult;
        }

        var start = Math.Min(fromIndex, toIndex);
        var end = Math.Max(fromIndex, toIndex);
        var ids = visible
            .Skip(start)
            .Take(end - start + 1)
            .Where(node => !node.IsDisabled)
            .Select(node => node.Id);

        return TreeOperationResult.From(_state.ReplaceSelection(ids));
    }

    /// <summary>清除指定子树内的选中节点。</summary>
    public TreeOperationResult ClearSelectionSubtree(TreeNode root)
    {
        if (!IsOwnedNode(root))
        {
            return TreeOperationResult.RejectedResult;
        }

        var toRemove = GetSelectedNodes().Where(n => n == root || n.IsDescendantOf(root)).Select(n => n.Id).ToList();
        var changed = false;
        foreach (var id in toRemove)
        {
            changed |= _state.RemoveFromSelection(id);
        }

        return TreeOperationResult.From(changed);
    }

    /// <summary>选中指定子树的可见节点（可选排除自身）。</summary>
    public TreeOperationResult SelectAll(TreeNode root, bool excludeSelf)
    {
        if (!IsOwnedNode(root))
        {
            return TreeOperationResult.RejectedResult;
        }

        var candidates = EnumerateVisibleSubtree(root)
            .Where(node => !(excludeSelf && node == root) && !node.IsDisabled)
            .ToArray();
        if (_selectionMode == TreeSelectionMode.Single && candidates.Length > 1)
        {
            return TreeOperationResult.RejectedResult;
        }

        var changed = false;
        foreach (var node in candidates)
        {
            changed |= _selectionMode == TreeSelectionMode.Single
                ? _state.Select(node.Id)
                : _state.AddToSelection(node.Id);
        }

        return TreeOperationResult.From(changed);
    }

    /// <summary>按树顺序返回全部选中节点（含折叠子树内的选中）。</summary>
    public IReadOnlyCollection<TreeNode> GetSelectedNodes() => Flatten().Where(n => _state.IsSelected(n.Id)).ToList();

    /// <summary>按树顺序返回指定子树内的选中节点。</summary>
    public IReadOnlyCollection<TreeNode> GetSelectedNodes(TreeNode? subtreeRoot)
        => (subtreeRoot is null ? Flatten() : EnumerateSubtree(subtreeRoot)).Where(n => _state.IsSelected(n.Id)).ToList();

    /// <summary>设置焦点（外部节点被拒绝；禁用节点可聚焦）。</summary>
    public TreeOperationResult SetFocus(TreeNode node)
    {
        if (!IsOwnedNode(node))
        {
            return TreeOperationResult.RejectedResult;
        }

        return TreeOperationResult.From(_state.SetFocus(node.Id));
    }

    public TreeOperationResult ClearFocus() => TreeOperationResult.From(_state.ClearFocus());

    #endregion

    #region 编辑

    /// <summary>进入编辑态（受 BeforeEditName 否决）。</summary>
    public bool BeginEdit(TreeNode node)
    {
        if (!IsOwnedNode(node))
        {
            return false;
        }

        if (BeforeEditName?.Invoke(node) == false)
        {
            return false;
        }

        node.IsEditing = true;
        return true;
    }

    /// <summary>提交重命名（受 BeforeRename 否决）。返回 false 表示被否决，应用不应接受该新值。</summary>
    public bool TryRename(TreeNode node, string newValue)
        => IsOwnedNode(node) && BeforeRename?.Invoke(node, newValue) != false;

    public void EndEdit(TreeNode node)
    {
        if (IsOwnedNode(node))
        {
            node.IsEditing = false;
        }
    }

    #endregion

    #region 区间与拖拽

    /// <summary>同根树上两节点之间的全部可见节点（跳过折叠子树）。</summary>
    public static bool GetNodesBetween(TreeNode node1, TreeNode node2, out List<TreeNode> result)
    {
        result = new List<TreeNode>();
        if (node1 is null || node2 is null)
        {
            return false;
        }

        if (!ReferenceEquals(node1.Root, node2.Root))
        {
            return false;
        }

        var nodeList = EnumerateVisibleSubtree(node1.Root).ToList();
        int index1 = IndexOfReference(nodeList, node1);
        int index2 = IndexOfReference(nodeList, node2);
        if (index1 == -1 || index2 == -1)
        {
            return false;
        }

        int start = Math.Min(index1, index2);
        int end = Math.Max(index1, index2);
        result.AddRange(nodeList.Skip(start).Take(end - start + 1));

        return true;
    }

    /// <summary>
    /// 内置拖拽结构规则：拒绝自引用与循环引用（目标为源的祖先）。同一 Tree 内跨根拖拽允许。
    /// 不校验消息协议、revision、relation 或 guard——这些由 <see cref="DragProtocol.Validate"/> 处理。
    /// </summary>
    public bool CanDrop(IReadOnlyList<TreeNode> sources, TreeNode target)
    {
        if (sources is null || sources.Count == 0)
        {
            return false;
        }

        if (sources.Any(s => !IsOwnedNode(s)) || !IsOwnedNode(target))
        {
            return false;
        }

        if (sources.Any(s => s == target))
        {
            return false;
        }

        if (sources.Any(s => target.IsDescendantOf(s)))
        {
            return false;
        }

        return true;
    }

    #endregion

    #region 遍历辅助

    /// <summary>深度优先枚举子树（含自身，按子节点顺序）。</summary>
    public static IEnumerable<TreeNode> EnumerateSubtree(TreeNode root)
    {
        var stack = new Stack<TreeNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            for (int i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(current.Children[i]);
            }
        }
    }

    /// <summary>深度优先枚举可见子树（仅沿展开路径）。</summary>
    public static IEnumerable<TreeNode> EnumerateVisibleSubtree(TreeNode root)
    {
        var stack = new Stack<TreeNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            if (current.HasChildren && current.IsExpanded)
            {
                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.Children[i]);
                }
            }
        }
    }

    private IEnumerable<TreeNode> EnumerateNodes(TreeNode? root)
        => root is null ? _roots.SelectMany(EnumerateSubtree) : EnumerateSubtree(root);

    private static int IndexOfReference(IReadOnlyList<TreeNode> nodes, TreeNode target)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (ReferenceEquals(nodes[i], target))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>节点是否属于当前 Controller（索引中按 Id 命中且为同一实例）。</summary>
    private bool IsOwnedNode(TreeNode? node)
        => node is not null
           && _index is not null
           && _index.TryGetValue(node.Id, out var owned)
           && ReferenceEquals(owned, node);

    #endregion
}
