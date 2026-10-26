namespace Astrea.Components.Tree;

/// <summary>
/// Exact, reference-based snapshot used to distinguish a parent re-render from
/// an in-place structure mutation. Invalid duplicate/cyclic structures never
/// compare equal and are delegated to TreeController.Refresh for diagnostics.
/// </summary>
internal sealed class TreeStructureSnapshot
{
    private readonly Entry[] _entries;

    private TreeStructureSnapshot(Entry[] entries, bool isValid)
    {
        _entries = entries;
        IsValid = isValid;
    }

    private bool IsValid { get; }

    public static TreeStructureSnapshot Capture(IReadOnlyList<TreeNode> roots)
    {
        var entries = new List<Entry>();
        var visited = new HashSet<TreeNode>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<TreeNode>();
        for (var rootIndex = roots.Count - 1; rootIndex >= 0; rootIndex--)
        {
            pending.Push(roots[rootIndex]);
        }

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (!visited.Add(node))
            {
                return new TreeStructureSnapshot(entries.ToArray(), isValid: false);
            }

            entries.Add(new Entry(node, node.Parent, node.Index, node.Id, node.Children.Count));
            for (var childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
            {
                pending.Push(node.Children[childIndex]);
            }
        }

        return new TreeStructureSnapshot(entries.ToArray(), isValid: true);
    }

    public bool Matches(TreeStructureSnapshot other)
    {
        if (!IsValid || !other.IsValid || _entries.Length != other._entries.Length)
        {
            return false;
        }

        for (var index = 0; index < _entries.Length; index++)
        {
            if (!_entries[index].Matches(other._entries[index]))
            {
                return false;
            }
        }

        return true;
    }

    private readonly struct Entry
    {
        public Entry(TreeNode node, TreeNode? parent, int index, string id, int childCount)
        {
            Node = node;
            Parent = parent;
            Index = index;
            Id = id;
            ChildCount = childCount;
        }

        private TreeNode Node { get; }

        private TreeNode? Parent { get; }

        private int Index { get; }

        private string Id { get; }

        private int ChildCount { get; }

        public bool Matches(Entry other)
            => ReferenceEquals(Node, other.Node) &&
               ReferenceEquals(Parent, other.Parent) &&
               Index == other.Index &&
               string.Equals(Id, other.Id, StringComparison.Ordinal) &&
               ChildCount == other.ChildCount;
    }
}
