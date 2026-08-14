namespace Astrea.Components.Tree;

/// <summary>
/// 节点编辑上下文：默认模板与自定义模板共享的完整编辑契约。
/// 提供当前缓冲值、更新值、提交、取消与拒绝反馈。焦点管理由契约约定（默认模板由组件聚焦，
/// 自定义模板自行管理焦点）。
/// </summary>
public sealed class TreeEditContext
{
    private readonly Func<Task> _onCommit;
    private readonly Action _onCancel;

    internal TreeEditContext(TreeNode node, string initialValue, Func<Task> onCommit, Action onCancel)
    {
        Node = node;
        Value = initialValue;
        _onCommit = onCommit;
        _onCancel = onCancel;
    }

    public TreeNode Node { get; }

    /// <summary>当前编辑缓冲值。</summary>
    public string Value { get; private set; }

    /// <summary>提交被否决时的反馈信息（null 表示无否决）。</summary>
    public string? ValidationMessage { get; internal set; }

    /// <summary>更新编辑缓冲值。</summary>
    public void SetValue(string value) => Value = value;

    /// <summary>提交编辑（受 BeforeRename 否决；否决时保持编辑态并设置 ValidationMessage）。</summary>
    public Task CommitAsync() => _onCommit();

    /// <summary>取消编辑（不发送结果通知）。</summary>
    public void Cancel() => _onCancel();
}
