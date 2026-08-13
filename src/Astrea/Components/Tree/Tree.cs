using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Astrea.Components.Tree;

/// <summary>
/// 全功能树组件（多选 / 拖拽 / 编辑 / 右键 + ARIA 键盘导航）。
/// 薄组件：渲染与事件转发，逻辑委托 <see cref="TreeController"/>。
/// </summary>
public sealed partial class Tree : ComponentBase, IAsyncDisposable
{
    private const int DbClickDelayMs = 300;

    private readonly string _id = Guid.NewGuid().ToString("N");
    private TreeController? _controller;
    private IReadOnlyList<TreeNode>? _boundDataSource;
    private long _clickTicks;
    private TreeNode? _firstClickNode;
    private TreeNode? _lastClickNode;
    private bool _lastAllowMultiSelect;
    private string? _editingValue;
    private TreeNode? _editingNode;
    private ElementReference _editInputRef;

    [Inject] private IJSRuntime? Js { get; set; }

    #region 参数

    [Parameter] public IReadOnlyList<TreeNode>? DataSource { get; set; }

    [Parameter] public TreeSettings? Settings { get; set; }

    [Parameter] public TreeCallbacks? Callbacks { get; set; }

    [Parameter] public EventCallback<IReadOnlyCollection<TreeNode>> OnSelect { get; set; }

    [Parameter] public EventCallback<TreeNode> OnClick { get; set; }

    [Parameter] public EventCallback<TreeNode> OnDblClick { get; set; }

    [Parameter] public EventCallback<(MouseEventArgs, TreeNode)> OnContextMenu { get; set; }

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    [Parameter] public EventCallback<(TreeNode, string)> OnInputEnter { get; set; }

    [Parameter] public EventCallback<DragNodesArgs> OnDrop { get; set; }

    [Parameter] public EventCallback<TreeNode> OnExpand { get; set; }

    [Parameter] public EventCallback<TreeNode> OnCollapse { get; set; }

    /// <summary>性能诊断回调：单击/双击/拖放处理完成后触发（操作名, 耗时 ms）。未订阅时仅一次 Stopwatch 分配开销。</summary>
    [Parameter] public EventCallback<(string Operation, double ElapsedMs)> OnTiming { get; set; }

    #endregion

    private TreeSettings S => Settings ??= new TreeSettings();

    /// <summary>当前控制器（供消费方访问选择/焦点/查询 API）。</summary>
    public TreeController? Controller => _controller;

    protected override void OnParametersSet()
    {
        // 关闭多选：收敛选择集合为单节点（REQ-F-086 AC4）
        if (_lastAllowMultiSelect && !S.AllowMultiSelect)
        {
            _controller?.ConvergeToSingleSelection();
        }

        _lastAllowMultiSelect = S.AllowMultiSelect;

        if (DataSource is null)
        {
            _controller = null;
            _boundDataSource = null;
            return;
        }

        if (_controller is null || !ReferenceEquals(_boundDataSource, DataSource))
        {
            // DataSource 替换：按稳定 Id 恢复展开/选中/焦点（REQ-F-061 AC2）
            var selectedIds = _controller?.GetSelectedNodes().Select(n => n.Id).ToHashSet();
            var expandedIds = _controller?.Flatten().Where(n => n.IsExpanded).Select(n => n.Id).ToHashSet();
            var focusId = _controller?.Selection.FocusedNode?.Id;

            _editingNode = null;
            _editingValue = null;
            _boundDataSource = DataSource;
            _controller = new TreeController(DataSource, Callbacks, S.HideRootNode);
            _controller.RestoreState(selectedIds, expandedIds, focusId);
        }
        else
        {
            // 就地结构变更：重建索引与缓存，保留选择
            _controller.Refresh();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && S.AllowDrag && Js is not null && DataSource is not null)
        {
            var module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/Astrea/astrea-tree.js");
            _dropModule = await module.InvokeAsync<IJSObjectReference>("initialize", $"tree-container-{_id}", DotNetObjectReference.Create(this));
        }

        // 编辑态：聚焦编辑输入框（autofocus 对动态插入元素不可靠，REQ-F-088 AC1）
        if (_editingNode is not null)
        {
            await _editInputRef.FocusAsync();
        }
    }

    #region 鼠标事件

    private async Task HandleClick(MouseEventArgs args, TreeNode node)
    {
        var sw = Stopwatch.StartNew();
        var handled = false;
        try
        {
            // 双击互斥：抑制第二次 click 的 OnSelect/OnClick（浏览器在双击时先发两次 click）
            var now = Environment.TickCount64;
            if (TreeInteraction.IsDoubleClick(_lastClickNode, node, _clickTicks, now, DbClickDelayMs))
            {
                _clickTicks = 0;
                return;
            }

            handled = true;

            switch (TreeInteraction.ResolveSelectionAction(S.AllowMultiSelect, args.ShiftKey, args.CtrlKey, _firstClickNode is not null))
            {
                case TreeInteraction.SelectionAction.Range:
                    _controller?.SelectRange(_firstClickNode!, node);
                    break;
                case TreeInteraction.SelectionAction.Toggle:
                    _firstClickNode = node;
                    _controller?.ToggleSelection(node);
                    break;
                default:
                    _firstClickNode = node;
                    _controller?.Select(node);
                    break;
            }

            _clickTicks = now;
            _lastClickNode = node;
            _controller?.SetFocus(node); // 点击即聚焦（WAI-ARIA，供 F2 等依赖焦点节点的操作）

            await NotifySelection();
            await OnClick.InvokeAsync(node);
        }
        finally
        {
            sw.Stop();
            if (handled)
            {
                await OnTiming.InvokeAsync(("click", sw.Elapsed.TotalMilliseconds));
            }
        }
    }

    private async Task HandleDblClick(TreeNode node)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await OnDblClick.InvokeAsync(node);
        }
        finally
        {
            sw.Stop();
            await OnTiming.InvokeAsync(("dblclick", sw.Elapsed.TotalMilliseconds));
        }
    }

    private async Task HandleRightClick(MouseEventArgs args, TreeNode node)
    {
        await OnContextMenu.InvokeAsync((args, node));
    }

    #endregion

    #region 键盘

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        // 编辑态：容器不拦截按键（字符输入/Enter/Escape 交由编辑 input 自身处理）
        if (_editingNode is not null)
        {
            return;
        }

        await OnKeyDown.InvokeAsync(args);

        if (!S.UseInternalKeyboard || _controller is null)
        {
            return;
        }

        var result = TreeKeyboardNavigation.Navigate(_controller, args.Key);

        if (result.Expanded is not null)
        {
            await OnExpand.InvokeAsync(result.Expanded);
        }

        if (result.Collapsed is not null)
        {
            await OnCollapse.InvokeAsync(result.Collapsed);
        }

        if (result.SelectionChanged)
        {
            await NotifySelection();
        }

        if (result.Changed)
        {
            StateHasChanged();
        }
    }

    #endregion

    #region 选择

    private Task NotifySelection()
        => _controller is null ? Task.CompletedTask : OnSelect.InvokeAsync(_controller.GetSelectedNodes());

    /// <summary>替换选中集合（供消费方程序化调用，不触发 OnSelect，REQ-F-063 AC3）。</summary>
    public async Task ReplaceSelection(IEnumerable<TreeNode> nodes)
    {
        _controller?.ReplaceSelection(nodes);
        StateHasChanged();
    }

    /// <summary>强制重渲染并失效控制器缓存（消费方在就地变更后调用，如外部改 IsExpanded）。</summary>
    public void Refresh()
    {
        _controller?.Refresh();
        StateHasChanged();
    }

    /// <summary>进入编辑态（初始化编辑缓冲，供消费方通过 F2 等触发，REQ-F-088 AC1）。</summary>
    public bool BeginEdit(TreeNode node)
    {
        if (_controller is null || !_controller.BeginEdit(node))
        {
            return false;
        }

        _editingValue = node.Text;
        _editingNode = node;
        StateHasChanged();
        return true;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_dropModule is not null)
        {
            await _dropModule.InvokeVoidAsync("destroy");
            await _dropModule.DisposeAsync();
        }
    }
}
