namespace Astrea.Components.Tree;

/// <summary>Tree 写操作的结果状态。</summary>
public enum TreeOperationStatus
{
    /// <summary>状态实际发生了改变。</summary>
    Changed,

    /// <summary>操作没有产生任何状态变化（如重复选择、已聚焦）。</summary>
    NoOp,

    /// <summary>操作被规则或 guard 拒绝（如禁用节点、外部节点、被否决）。</summary>
    Rejected,
}

/// <summary>
/// Tree 写操作的确定性结果，供组件据此决定是否发送事件通知。
/// </summary>
public readonly struct TreeOperationResult
{
    public TreeOperationStatus Status { get; }

    public bool Changed => Status == TreeOperationStatus.Changed;

    public bool NoOp => Status == TreeOperationStatus.NoOp;

    public bool Rejected => Status == TreeOperationStatus.Rejected;

    private TreeOperationResult(TreeOperationStatus status) => Status = status;

    public static readonly TreeOperationResult ChangedResult = new(TreeOperationStatus.Changed);

    public static readonly TreeOperationResult NoOpResult = new(TreeOperationStatus.NoOp);

    public static readonly TreeOperationResult RejectedResult = new(TreeOperationStatus.Rejected);

    public static TreeOperationResult From(bool changed) => changed ? ChangedResult : NoOpResult;

    public override string ToString() => Status.ToString();
}
