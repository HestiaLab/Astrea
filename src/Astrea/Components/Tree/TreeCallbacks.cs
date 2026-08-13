namespace Astrea.Components.Tree;

/// <summary>
/// 树操作前的否决回调集合（before*）。返回 false 表示否决该操作。
/// 纯委托（不依赖 Blazor），可由 <see cref="TreeController"/> 直接调用，便于单测。
/// </summary>
public sealed class TreeCallbacks
{
    /// <summary>否决展开。</summary>
    public Func<TreeNode, bool>? BeforeExpand { get; set; }

    /// <summary>否决折叠。</summary>
    public Func<TreeNode, bool>? BeforeCollapse { get; set; }

    /// <summary>否决拖放（返回 false 阻止放置）。</summary>
    public Func<DragNodesArgs, bool>? BeforeDrop { get; set; }

    /// <summary>否决重命名（参数：节点、新值）。</summary>
    public Func<TreeNode, string, bool>? BeforeRename { get; set; }

    /// <summary>否决进入编辑态。</summary>
    public Func<TreeNode, bool>? BeforeEditName { get; set; }

    /// <summary>否决移除。</summary>
    public Func<TreeNode, bool>? BeforeRemove { get; set; }
}
