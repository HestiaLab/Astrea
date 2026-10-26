namespace Astrea.Components.Tree;

/// <summary>
/// 虚拟化参数校验（纯逻辑）：固定行高虚拟化（<see cref="Tree.EnableVirtualization"/>）的前置条件。
/// 计划 §5.12：ItemSize 必须大于 0；可变高度 NodeTemplate / EditTemplate 与固定行高虚拟化冲突，
/// 拒绝而非静默产生错误布局。
/// </summary>
public static class TreeVirtualizationPolicy
{
    /// <summary>校验虚拟化配置；违规时抛出异常，否则无操作。</summary>
    public static void Validate(bool enableVirtualization, float itemSize, bool hasNodeTemplate, bool hasEditTemplate)
    {
        if (!enableVirtualization)
        {
            return;
        }

        if (itemSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemSize), itemSize, "ItemSize must be greater than 0 when virtualization is enabled.");
        }

        if (hasNodeTemplate)
        {
            throw new InvalidOperationException("NodeTemplate renders variable-height content and cannot be combined with fixed-row-height virtualization.");
        }

        if (hasEditTemplate)
        {
            throw new InvalidOperationException("EditTemplate renders variable-height content and cannot be combined with fixed-row-height virtualization.");
        }
    }
}
