namespace Astrea.Components.Tree;

/// <summary>
/// 树控制器：持有根集合，维护 O(1) 节点索引、扁平化缓存、选择与展开操作、拖拽校验。
/// 纯 C#，不依赖 Blazor 渲染，可独立单测。选择状态的唯一变更者。
/// </summary>
public sealed class TreeController
{
    private readonly TreeCallbacks _callbacks;
    private Dictionary<string, TreeNode>? _index;
    private List<TreeNode>? _flatten;
    private List<TreeNode>? _flattenVisible;

    public TreeController(IReadOnlyList<TreeNode> roots, TreeCallbacks? callbacks = null, bool hideRootNode = false)
    {
        Roots = roots;
        _callbacks = callbacks ?? new TreeCallbacks();
        HideRootNode = hideRootNode;
        Refresh();
    }

    /// <summary>根节点集合。</summary>
    public IReadOnlyList<TreeNode> Roots { get; }

    /// <summary>是否隐藏根节点。</summary>
    public bool HideRootNode { get; }

    /// <summary>选择/焦点唯一真相。</summary>
    public TreeSelection Selection { get; } = new();

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
            _flatten = Roots.SelectMany(EnumerateSubtree).ToList();
        }

        return _flatten;
    }

    /// <summary>可见节点（根按 IsVisible 过滤，子节点仅沿展开路径）。</summary>
    public IReadOnlyList<TreeNode> FlattenVisible()
    {
        if (_flattenVisible is null)
        {
            var list = new List<TreeNode>();
            foreach (var root in Roots)
            {
                if (!root.IsVisible)
                {
                    continue;
                }

                if (HideRootNode)
                {
                    if (root.HasChildren && root.IsExpanded)
                    {
                        foreach (var child in root.Children)
                        {
                            list.AddRange(EnumerateVisibleSubtree(child));
                        }
                    }
                }
                else
                {
                    list.AddRange(EnumerateVisibleSubtree(root));
                }
            }

            _flattenVisible = list;
        }

        return _flattenVisible;
    }

    /// <summary>重建索引与缓存，并重新挂接节点后向引用；清理悬空选择并恢复丢失的焦点。</summary>
    public void Refresh()
    {
        var previousFocus = Selection.FocusedNode;
        var previousSelected = Selection.SelectedNodes.ToList();

        _index = new Dictionary<string, TreeNode>();
        _flatten = null;
        _flattenVisible = null;

        foreach (var root in Roots)
        {
            foreach (var node in EnumerateSubtree(root))
            {
                node.Controller = this;
                _index[node.Id] = node; // 重复 id 时后者覆盖（应用应保证 id 唯一）
            }
        }

        // 清理悬空选择（被移除的选中节点）；应用结构变更，不视为用户选择，故不通知
        foreach (var node in previousSelected)
        {
            if (!_index.ContainsKey(node.Id))
            {
                Selection.RemoveFromSelection(node);
            }
        }

        // 焦点恢复（REQ-F-077 AC1/AC2）
        if (previousFocus is not null)
        {
            var visible = FlattenVisible();
            if (!visible.Contains(previousFocus))
            {
                RestoreFocus(previousFocus, visible);
            }
        }
    }

    /// <summary>焦点节点脱离可见树后，恢复到最近可见目标。</summary>
    private void RestoreFocus(TreeNode lost, IReadOnlyList<TreeNode> visible)
    {
        // 锚点：脱离时的父节点（仍在树内）
        var anchor = lost.DetachedParent;
        if (anchor is not null && _index!.ContainsKey(anchor.Id))
        {
            if (visible.Contains(anchor))
            {
                var children = anchor.Children;
                int idx = lost.DetachedIndex;
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

                Selection.SetFocus(target);
                return;
            }

            // 锚点不可见（被折叠）→ 最近可见祖先
            for (var p = anchor.Parent; p is not null; p = p.Parent)
            {
                if (visible.Contains(p))
                {
                    Selection.SetFocus(p);
                    return;
                }
            }
        }

        // 兜底：第一个可见节点；无可见节点则清空焦点
        if (visible.Count > 0)
        {
            Selection.SetFocus(visible[0]);
        }
        else
        {
            Selection.ClearFocus();
        }
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
            var nodes = Flatten().Where(n => selected.Contains(n.Id)).ToList();
            if (nodes.Count > 0)
            {
                ReplaceSelection(nodes);
            }
        }

        if (focusId is not null)
        {
            var focusNode = FindById(focusId);
            if (focusNode is not null)
            {
                SetFocus(focusNode);
            }
        }
    }

    private void InvalidateVisible() => _flattenVisible = null;

    #endregion

    #region 展开与折叠

    /// <summary>展开节点（受 BeforeExpand 否决）。</summary>
    public bool Expand(TreeNode node)
    {
        if (node.IsLeaf || node.IsDisabled)
        {
            return false;
        }

        if (_callbacks.BeforeExpand?.Invoke(node) == false)
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
        if (node.IsLeaf || node.IsDisabled)
        {
            return false;
        }

        if (_callbacks.BeforeCollapse?.Invoke(node) == false)
        {
            return false;
        }

        var focused = Selection.FocusedNode;
        node.IsExpanded = false;
        InvalidateVisible();

        // 焦点恢复：焦点位于被折叠分支内时，移到折叠节点（REQ-F-077 AC4）
        if (focused is not null && focused != node && focused.IsDescendantOf(node))
        {
            Selection.SetFocus(node);
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

    public void Select(TreeNode node)
    {
        if (node.IsDisabled)
        {
            return;
        }

        Selection.Select(node);
    }

    public void AddToSelection(TreeNode node)
    {
        if (node.IsDisabled)
        {
            return;
        }

        Selection.AddToSelection(node);
    }

    public void ToggleSelection(TreeNode node)
    {
        if (node.IsDisabled)
        {
            return;
        }

        Selection.ToggleSelection(node);
    }

    public void ReplaceSelection(IEnumerable<TreeNode> nodes) => Selection.ReplaceSelection(nodes);

    public void ClearSelection() => Selection.ClearSelection();

    /// <summary>收敛选择集合为单节点（关闭多选时调用）：焦点节点若已选中则保留，否则清空。</summary>
    public void ConvergeToSingleSelection()
    {
        var selected = GetSelectedNodes();
        if (selected.Count <= 1)
        {
            return;
        }

        var focused = Selection.FocusedNode;
        if (focused is not null && focused.IsSelected)
        {
            Selection.ReplaceSelection(new[] { focused });
        }
        else
        {
            Selection.ClearSelection();
        }
    }

    /// <summary>Shift 区间选择。</summary>
    public void SelectRange(TreeNode from, TreeNode to)
    {
        if (GetNodesBetween(from, to, out var range))
        {
            Selection.ReplaceSelection(range);
        }
    }

    /// <summary>清除指定子树内的选中节点。</summary>
    public void ClearSelectionSubtree(TreeNode root)
    {
        foreach (var node in Selection.SelectedNodes.ToList())
        {
            if (node == root || node.IsDescendantOf(root))
            {
                Selection.RemoveFromSelection(node);
            }
        }
    }

    /// <summary>选中指定子树的可见节点（可选排除自身）。</summary>
    public void SelectAll(TreeNode root, bool excludeSelf)
    {
        foreach (var node in EnumerateVisibleSubtree(root))
        {
            if (excludeSelf && node == root)
            {
                continue;
            }

            Selection.AddToSelection(node);
        }
    }

    /// <summary>按树顺序返回全部选中节点（含折叠子树内的选中，修复旧版漏选）。</summary>
    public IReadOnlyCollection<TreeNode> GetSelectedNodes() => Flatten().Where(n => n.IsSelected).ToList();

    /// <summary>按树顺序返回指定子树内的选中节点。</summary>
    public IReadOnlyCollection<TreeNode> GetSelectedNodes(TreeNode? subtreeRoot)
        => (subtreeRoot is null ? Flatten() : EnumerateSubtree(subtreeRoot)).Where(n => n.IsSelected).ToList();

    public void SetFocus(TreeNode node) => Selection.SetFocus(node);

    public void ClearFocus() => Selection.ClearFocus();

    #endregion

    #region 编辑

    /// <summary>进入编辑态（受 BeforeEditName 否决）。</summary>
    public bool BeginEdit(TreeNode node)
    {
        if (_callbacks.BeforeEditName?.Invoke(node) == false)
        {
            return false;
        }

        node.IsEditing = true;
        return true;
    }

    /// <summary>提交重命名（受 BeforeRename 否决）。返回 false 表示被否决，应用不应接受该新值。</summary>
    public bool TryRename(TreeNode node, string newValue)
        => _callbacks.BeforeRename?.Invoke(node, newValue) != false;

    public void EndEdit(TreeNode node) => node.IsEditing = false;

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

        var nodeList = EnumerateSubtree(node1.Root).ToList();
        int index1 = nodeList.IndexOf(node1);
        int index2 = nodeList.IndexOf(node2);
        if (index1 == -1 || index2 == -1)
        {
            return false;
        }

        int start = Math.Min(index1, index2);
        int end = Math.Max(index1, index2);
        for (int i = start; i <= end; i++)
        {
            if (nodeList[i].Parent?.IsExpanded == false)
            {
                continue;
            }

            result.Add(nodeList[i]);
        }

        return true;
    }

    /// <summary>校验拖放是否允许（内置规则 + BeforeDrop 否决），不执行移动。</summary>
    public bool CanDrop(IReadOnlyList<TreeNode> sources, TreeNode target, DragPosition position)
    {
        if (!DefaultDropRule(sources, target))
        {
            return false;
        }

        var args = new DragNodesArgs(sources, target, position);
        return _callbacks.BeforeDrop?.Invoke(args) != false;
    }

    /// <summary>内置拖拽规则：拒绝自引用、跨树、循环引用（覆盖全部源节点）。</summary>
    private static bool DefaultDropRule(IReadOnlyList<TreeNode> sources, TreeNode target)
    {
        if (sources is null || sources.Count == 0)
        {
            return false;
        }

        if (sources.Any(s => s == target))
        {
            return false;
        }

        if (sources.Any(s => !ReferenceEquals(s.Root, target.Root)))
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
        => root is null ? Roots.SelectMany(EnumerateSubtree) : EnumerateSubtree(root);

    #endregion
}
