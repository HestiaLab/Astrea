namespace Astrea.Components.Tree;

/// <summary>
/// 拖拽放置位置（上/中/下），对应"之前/之内/之后"。
/// </summary>
public sealed class DragPosition : IEquatable<DragPosition>
{
    private readonly int _value;

    private DragPosition(int value) => _value = value;

    public bool IsTop => this == Top;

    public bool IsCenter => this == Center;

    public bool IsBottom => this == Bottom;

    public static readonly DragPosition Top = new(1);

    public static readonly DragPosition Center = new(0);

    public static readonly DragPosition Bottom = new(-1);

    public static implicit operator int(DragPosition op) => op._value;

    public static DragPosition FromValue(int value) => new(value);

    /// <summary>从 JS 拖放回调的位置字符串解析放置位置（未知值默认 Bottom）。</summary>
    public static DragPosition Parse(string position) => position switch
    {
        "top" => Top,
        "center" => Center,
        _ => Bottom
    };

    public static bool operator ==(DragPosition? left, DragPosition? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left._value == right._value;
    }

    public static bool operator !=(DragPosition? left, DragPosition? right) => !(left == right);

    public bool Equals(DragPosition? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return _value == other._value;
    }

    public override bool Equals(object? obj) => Equals(obj as DragPosition);

    public override int GetHashCode() => _value.GetHashCode();
}

/// <summary>
/// 拖拽放置参数：源节点集合、目标节点、放置位置。
/// </summary>
public class DragNodesArgs
{
    private readonly List<TreeNode> _sources;

    private DragPosition _position = DragPosition.Top;

    public DragNodesArgs(IEnumerable<TreeNode> sources, TreeNode target, DragPosition position)
    {
        _sources = sources.ToList() ?? throw new ArgumentNullException(nameof(sources));
        Target = target;
        Position = position;
    }

    /// <summary>目标节点。</summary>
    public TreeNode Target { get; set; }

    /// <summary>放置位置（上/中/下）。</summary>
    public DragPosition Position
    {
        get => _position;
        set
        {
            if (value == DragPosition.Top || value == DragPosition.Center || value == DragPosition.Bottom)
            {
                _position = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be Top, Center or Bottom");
            }
        }
    }

    /// <summary>被拖拽的源节点集合。</summary>
    public IReadOnlyCollection<TreeNode> Sources => _sources.AsReadOnly();
}
