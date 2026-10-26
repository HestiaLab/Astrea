namespace Astrea.Components.Tree;

/// <summary>
/// 树结构违反不变量（重复 Id、重复节点实例、循环、Parent/Index 不一致）时抛出的确定性异常。
/// </summary>
public sealed class TreeStructureException : InvalidOperationException
{
    public TreeStructureException(string message)
        : base(message)
    {
    }
}
