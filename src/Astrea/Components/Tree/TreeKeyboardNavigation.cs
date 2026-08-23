namespace Astrea.Components.Tree;

/// <summary>
/// 树键盘导航状态机（纯逻辑）：根据按键与当前焦点/可见序列移动焦点、展开折叠或选择，
/// 返回导航结果供组件触发对应通知。符合 WAI-ARIA Tree View Pattern（REQ-F-066）。
/// </summary>
public static class TreeKeyboardNavigation
{
    /// <summary>一次键盘导航的结果。</summary>
    public sealed class Result
    {
        /// <summary>焦点或状态是否变化（决定组件是否重渲染）。</summary>
        public bool Changed { get; set; }

        /// <summary>因 ArrowRight 展开的节点（组件需触发 OnExpand）。</summary>
        public TreeNode? Expanded { get; set; }

        /// <summary>因 ArrowLeft 折叠的节点（组件需触发 OnCollapse）。</summary>
        public TreeNode? Collapsed { get; set; }

        /// <summary>因 Space/Enter 触发的选择（组件需触发 OnSelectionChanged）。</summary>
        public bool SelectionChanged { get; set; }
    }

    /// <summary>执行一次键盘导航。</summary>
    public static Result Navigate(
        TreeController controller,
        string key,
        bool allowExpand = true,
        bool allowCollapse = true)
    {
        var result = new Result();
        var visible = controller.FlattenVisible();
        if (visible.Count == 0)
        {
            return result;
        }

        var focused = controller.FocusedNode;
        int idx = IndexOf(visible, focused);

        switch (key)
        {
            case "ArrowDown":
                if (idx < 0)
                {
                    controller.SetFocus(visible[0]);
                    result.Changed = true;
                }
                else if (idx + 1 < visible.Count)
                {
                    controller.SetFocus(visible[idx + 1]);
                    result.Changed = true;
                }
                break;

            case "ArrowUp":
                if (idx > 0)
                {
                    controller.SetFocus(visible[idx - 1]);
                    result.Changed = true;
                }
                else if (idx < 0)
                {
                    controller.SetFocus(visible[0]);
                    result.Changed = true;
                }
                break;

            case "Home":
                controller.SetFocus(visible[0]);
                result.Changed = true;
                break;

            case "End":
                controller.SetFocus(visible[visible.Count - 1]);
                result.Changed = true;
                break;

            case "ArrowRight":
                if (focused is not null && focused.HasChildren && !focused.IsExpanded)
                {
                    if (allowExpand && controller.Expand(focused))
                    {
                        result.Expanded = focused;
                        result.Changed = true;
                    }
                }
                else if (focused is not null && focused.HasChildren && focused.IsExpanded)
                {
                    controller.SetFocus(focused.Children[0]);
                    result.Changed = true;
                }
                break;

            case "ArrowLeft":
                if (focused is not null && focused.IsExpanded)
                {
                    if (allowCollapse && controller.Collapse(focused))
                    {
                        result.Collapsed = focused;
                        result.Changed = true;
                    }
                }
                else if (focused?.Parent is not null)
                {
                    controller.SetFocus(focused.Parent);
                    result.Changed = true;
                }
                break;

            case " ":
            case "Spacebar":
                if (focused is not null && !focused.IsDisabled)
                {
                    var selectResult = controller.Select(focused);
                    result.SelectionChanged = selectResult.Changed;
                    result.Changed = selectResult.Changed;
                }
                break;
        }

        return result;
    }

    private static int IndexOf(IReadOnlyList<TreeNode> list, TreeNode? node)
    {
        if (node is null)
        {
            return -1;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], node))
            {
                return i;
            }
        }

        return -1;
    }
}
