namespace Astrea.Components.Tree;

/// <summary>
/// 树交互的纯逻辑（单击选择策略 + 双击互斥），独立于组件生命周期，便于单测。
/// </summary>
public static class TreeInteraction
{
    /// <summary>单击选择动作。</summary>
    public enum SelectionAction
    {
        /// <summary>单选（替换选择）。</summary>
        Single,

        /// <summary>范围选择（Shift）。</summary>
        Range,

        /// <summary>切换选择（Ctrl）。</summary>
        Toggle,
    }

    /// <summary>判断一次单击是否属于双击的第二次 click（浏览器双击先发两次 click）。</summary>
    public static bool IsDoubleClick(TreeNode? lastNode, TreeNode node, long lastClickTicks, long nowTicks, int delayMs)
        => ReferenceEquals(lastNode, node) && Math.Abs(nowTicks - lastClickTicks) < delayMs;

    /// <summary>根据多选开关与修饰键解析选择动作。</summary>
    public static SelectionAction ResolveSelectionAction(bool allowMultiSelect, bool shiftKey, bool ctrlKey, bool hasAnchor)
    {
        if (allowMultiSelect && shiftKey && hasAnchor)
        {
            return SelectionAction.Range;
        }

        if (allowMultiSelect && ctrlKey)
        {
            return SelectionAction.Toggle;
        }

        return SelectionAction.Single;
    }
}
