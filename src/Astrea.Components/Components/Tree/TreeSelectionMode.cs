namespace Astrea.Components.Tree;

/// <summary>Tree 的选择模式。</summary>
public enum TreeSelectionMode
{
    /// <summary>单选：选择新节点时替换原选择（REQ-F-063）。</summary>
    Single,

    /// <summary>多选：支持范围选择（Shift）与切换选择（Ctrl）（REQ-F-086）。</summary>
    Multiple,
}
