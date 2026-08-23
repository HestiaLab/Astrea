namespace Astrea.Components.Tree;

/// <summary>
/// Read-only projection of a Tree component. Mutations are exposed by the
/// component so controlled parameters, guards, and event ordering cannot be bypassed.
/// </summary>
public sealed class TreeView
{
    private readonly TreeController _controller;

    internal TreeView(TreeController controller)
    {
        _controller = controller;
    }

    public long Revision => _controller.Revision;

    public IReadOnlyCollection<string> SelectedIds => _controller.SelectedIds;

    public string? FocusedId => _controller.FocusedId;

    public TreeNode? FocusedNode => _controller.FocusedNode;

    public TreeNode? FindById(string id) => _controller.FindById(id);

    public TreeNode? FindByBindData(object? bindData) => _controller.FindByBindData(bindData);

    public IReadOnlyList<TreeNode> Flatten() => _controller.Flatten();

    public IReadOnlyList<TreeNode> FlattenVisible() => _controller.FlattenVisible();

    public IReadOnlyCollection<TreeNode> GetSelectedNodes() => _controller.GetSelectedNodes();

    public IReadOnlyCollection<TreeNode> GetSelectedNodes(TreeNode? subtreeRoot)
        => _controller.GetSelectedNodes(subtreeRoot);
}
