using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Astrea.Components.ResourceTable;

/// <summary>
/// 资源表组件：列驱动表头 + 模板表体 + 可拖拽列宽 + 工具栏（+ − ↑ ↓ / Undo / Redo）。
/// 薄组件：渲染与事件转发；列宽拖拽由 <c>astrea-resource-table.js</c> 实现，
/// 拖拽结束通过 <see cref="OnColumnResized"/> 通知应用。
/// </summary>
public sealed partial class ResourceTable : ComponentBase, IAsyncDisposable
{
    private readonly string _tableId = Guid.NewGuid().ToString("N");

    private List<TableColumn> _columns = new();

    private DotNetObjectReference<ResourceTable>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private bool _resizeInitialized;
    private string? _resizeSignature;
    private bool _disposed;

    [Inject] private IJSRuntime? Js { get; set; }

    #region 参数

    /// <summary>
    /// 列定义；允许空集合（渲染空表头）
    /// </summary>
    [Parameter, EditorRequired]
    public IEnumerable<TableColumn> Columns
    {
        get => _columns;
        set => _columns = value?.ToList() ?? new List<TableColumn>();
    }

    /// <summary>
    /// 表体模板（行内容由应用渲染）
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment? TableBody { get; set; }

    /// <summary>
    /// 工具栏附加内容
    /// </summary>
    [Parameter]
    public RenderFragment? ToolBar { get; set; }

    /// <summary>
    /// 显示 + − ↑ ↓ 按钮
    /// </summary>
    [Parameter]
    public bool ShowButton { get; set; } = true;

    /// <summary>
    /// 显示 Undo/Redo 按钮
    /// </summary>
    [Parameter]
    public bool ShowRedoButton { get; set; }

    /// <summary>Toolbar button accessible names/tooltips (REQ-F-009).</summary>
    [Parameter]
    public string AddButtonLabel { get; set; } = "Add";

    [Parameter]
    public string RemoveButtonLabel { get; set; } = "Remove";

    [Parameter]
    public string MoveUpButtonLabel { get; set; } = "Move Up";

    [Parameter]
    public string MoveDownButtonLabel { get; set; } = "Move Down";

    [Parameter]
    public string UndoButtonLabel { get; set; } = "Undo";

    [Parameter]
    public string RedoButtonLabel { get; set; } = "Redo";

    /// <summary>Resize handle accessible name; <c>{0}</c> is the column title.</summary>
    [Parameter]
    public string ResizeHandleLabelFormat { get; set; } = "Resize column {0}";

    /// <summary>
    /// 表格容器样式类
    /// </summary>
    [Parameter]
    public string? ContainerCss { get; set; }

    [Parameter]
    public EventCallback OnAddClicked { get; set; }

    [Parameter]
    public EventCallback OnRemoveClicked { get; set; }

    [Parameter]
    public EventCallback OnMoveUpClicked { get; set; }

    [Parameter]
    public EventCallback OnMoveDownClicked { get; set; }

    [Parameter]
    public EventCallback OnUndoClicked { get; set; }

    [Parameter]
    public EventCallback OnRedoClicked { get; set; }

    [Parameter]
    public bool CanUndo { get; set; }

    [Parameter]
    public bool CanRedo { get; set; }

    /// <summary>
    /// 键盘导航回调
    /// </summary>
    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// 是否启用列宽拖拽（默认 true）
    /// </summary>
    [Parameter]
    public bool ResizableColumns { get; set; } = true;

    /// <summary>
    /// 列宽拖拽结束回调（参数：列索引、列 Key、新宽度 px）
    /// </summary>
    [Parameter]
    public EventCallback<ColumnResizedEventArgs> OnColumnResized { get; set; }

    /// <summary>
    /// 工具栏图标（默认内联 SVG，零框架依赖；传入后覆盖默认图标）
    /// </summary>
    [Parameter]
    public RenderFragment? IconAdd { get; set; }

    [Parameter]
    public RenderFragment? IconRemove { get; set; }

    [Parameter]
    public RenderFragment? IconMoveUp { get; set; }

    [Parameter]
    public RenderFragment? IconMoveDown { get; set; }

    [Parameter]
    public RenderFragment? IconUndo { get; set; }

    [Parameter]
    public RenderFragment? IconRedo { get; set; }

    /// <summary>
    /// 附加属性（REQ-F-001）：作用于根容器元素
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    #endregion

    /// <summary>
    /// 是否渲染表头（列非空）
    /// </summary>
    private bool HasColumns => _columns.Count > 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || Js is null)
        {
            return;
        }

        if (ResizableColumns)
        {
            var signature = BuildResizeSignature();
            if (!_resizeInitialized || _resizeSignature != signature)
            {
                await EnsureResizeInitializedAsync(signature);
            }
        }
        else if (_resizeInitialized)
        {
            await DestroyResizeAsync();
        }
    }

    private string BuildResizeSignature()
        => string.Join(
            "\u001f",
            _columns.Select(column => string.Join(
                "\u001e",
                column.Key,
                column.Title,
                column.MinWidth?.ToString(CultureInfo.InvariantCulture) ?? "-",
                column.Resizable.ToString())));

    private async Task EnsureResizeInitializedAsync(string signature)
    {
        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            var module = _jsModule;
            if (module is null)
            {
                module = await Js!.InvokeAsync<IJSObjectReference>("import",
                    "./_content/Astrea/astrea-resource-table.js");
                if (_disposed || !ResizableColumns)
                {
                    await DisposeReferenceQuietlyAsync(module);
                    return;
                }

                _jsModule = module;
            }

            var minWidths = _columns.Select(column => column.MinWidth).ToArray();
            await module.InvokeVoidAsync("initializeResizeTable", _tableId, _dotNetRef, new { minWidths });
            if (_disposed || !ResizableColumns)
            {
                await DestroyResizeAsync();
                return;
            }

            _resizeInitialized = true;
            _resizeSignature = signature;
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线：放弃本次初始化，后续渲染可重试。
        }
        catch (ObjectDisposedException)
        {
            // Dispose 与初始化交错时模块已被释放。
        }
    }

    private async Task DestroyResizeAsync()
    {
        var module = _jsModule;
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("disposeResizableColumns", _tableId);
            }
            catch (JSDisconnectedException)
            {
                // 断线后 JS 句柄已不可达。
            }
            catch (ObjectDisposedException)
            {
                // 模块已被并发释放。
            }
        }

        _resizeInitialized = false;
        _resizeSignature = null;
    }

    private static async Task DisposeReferenceQuietlyAsync(IJSObjectReference reference)
    {
        try
        {
            await reference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // 断线后 JS 句柄已不可达。
        }
        catch (ObjectDisposedException)
        {
            // 已被并发释放。
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_resizeInitialized)
        {
            await DestroyResizeAsync();
        }

        var module = _jsModule;
        _jsModule = null;
        if (module is not null)
        {
            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
            catch (TaskCanceledException) { }
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }

    private Task HandleKeyDown(KeyboardEventArgs e)
        => OnKeyDown.HasDelegate ? OnKeyDown.InvokeAsync(e) : Task.CompletedTask;

    /// <summary>
    /// JS 拖拽结束回调入口（JS 侧调用名：OnColumnResized）
    /// </summary>
    [JSInvokable("OnColumnResized")]
    public Task HandleColumnResized(int columnIndex, double newWidth)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count || !double.IsFinite(newWidth) || newWidth < 0)
        {
            return Task.CompletedTask;
        }

        var column = _columns[columnIndex];
        var width = column.MinWidth is { } minWidth && newWidth < minWidth ? minWidth : newWidth;
        return OnColumnResized.InvokeAsync(new ColumnResizedEventArgs(columnIndex, column.Key, width));
    }
}
