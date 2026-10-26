namespace Astrea.Components.ResourceTable;

/// <summary>
/// 资源表列定义。
/// 用于声明列标题、宽度、稳定标识、最小拖拽宽度与列级拖拽开关。
/// </summary>
public sealed class TableColumn
{
    /// <summary>
    /// 列头文本
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 列宽度（CSS 字符串，如 "100px"、"20%"）；null 时由表格自动分配
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// 稳定列标识，用于 <see cref="ColumnResizedEventArgs.ColumnKey"/> 回调
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 拖拽最小宽度（px），默认 50
    /// </summary>
    public double? MinWidth { get; set; }

    /// <summary>
    /// 是否允许拖拽调整该列宽度，默认 true（最后一列不生成手柄）
    /// </summary>
    public bool Resizable { get; set; } = true;

    public TableColumn(string title)
    {
        Title = title;
    }
}

/// <summary>
/// 列宽拖拽结束事件参数
/// </summary>
public sealed record ColumnResizedEventArgs(int ColumnIndex, string? ColumnKey, double NewWidth);
