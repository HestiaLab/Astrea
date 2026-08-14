using Microsoft.AspNetCore.Components;

namespace Astrea.Components.Tree;

/// <summary>
/// 节点编辑上下文：默认模板与自定义模板共享的完整编辑契约。
/// 提供当前缓冲值、更新值、提交、取消与拒绝反馈。焦点管理由契约约定（默认模板由组件聚焦，
/// 自定义模板自行管理焦点）。
/// </summary>
public sealed class TreeEditContext
{
    private readonly Func<TreeEditContext, Task> _onCommit;
    private readonly Func<TreeEditContext, Task> _onCancel;
    private int _valueVersion;

    internal TreeEditContext(
        long sessionId,
        long sourceRevision,
        TreeNode node,
        string initialValue,
        Func<TreeEditContext, Task> onCommit,
        Func<TreeEditContext, Task> onCancel)
    {
        SessionId = sessionId;
        SourceRevision = sourceRevision;
        Node = node;
        Value = initialValue;
        _onCommit = onCommit;
        _onCancel = onCancel;
    }

    internal long SessionId { get; }

    internal long SourceRevision { get; }

    internal int ValueVersion => _valueVersion;

    internal ElementReference? FocusTarget { get; private set; }

    public TreeNode Node { get; }

    /// <summary>当前编辑缓冲值。</summary>
    public string Value { get; private set; }

    /// <summary>提交被否决时的反馈信息（null 表示无否决）。</summary>
    public string? ValidationMessage { get; internal set; }

    /// <summary>更新编辑缓冲值。</summary>
    public void SetValue(string value)
    {
        if (Value == value)
        {
            return;
        }

        Value = value;
        ValidationMessage = null;
        _valueVersion++;
    }

    /// <summary>
    /// Registers the element that should receive focus after a custom edit
    /// template is rendered. Render-tree based templates can call this from an
    /// element-reference capture; reusable Razor templates can delegate it to a
    /// small input component.
    /// </summary>
    public void RegisterFocusTarget(ElementReference target) => FocusTarget = target;

    /// <summary>提交编辑（受 BeforeRename 否决；否决时保持编辑态并设置 ValidationMessage）。</summary>
    public Task CommitAsync() => _onCommit(this);

    /// <summary>取消编辑（不发送结果通知）。</summary>
    public Task CancelAsync() => _onCancel(this);
}
