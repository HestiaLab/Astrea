using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Astrea.Components.Tree;

/// <summary>
/// 全功能树组件（多选 / 拖拽 / 编辑 / 右键 + ARIA 键盘导航）。
/// 薄组件：渲染与事件转发，逻辑委托 <see cref="TreeController"/>。
/// 选择与展开支持受控（SelectedIds/ExpandedIds 非 null）与非受控（null）模式。
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
    private TreeSelectionMode _lastSelectionMode;
    private TreeEditContext? _editingContext;
    private ElementReference _editInputRef;

    [Inject] private IJSRuntime? Js { get; set; }

    #region 参数

    [Parameter, EditorRequired] public IReadOnlyList<TreeNode>? DataSource { get; set; }

    [Parameter] public TreeSelectionMode SelectionMode { get; set; } = TreeSelectionMode.Single;

    /// <summary>受控选择（非 null 时应用持有选择权威，用户操作仅触发 SelectedIdsChanged）。</summary>
    [Parameter] public IReadOnlyCollection<string>? SelectedIds { get; set; }

    [Parameter] public EventCallback<IReadOnlyCollection<string>> SelectedIdsChanged { get; set; }

    /// <summary>受控展开（非 null 时应用持有展开权威，用户操作仅触发 ExpandedIdsChanged）。</summary>
    [Parameter] public IReadOnlyCollection<string>? ExpandedIds { get; set; }

    [Parameter] public EventCallback<IReadOnlyCollection<string>> ExpandedIdsChanged { get; set; }

    [Parameter] public bool AllowDrag { get; set; }

    [Parameter] public bool EnableVirtualization { get; set; }

    [Parameter] public float ItemSize { get; set; } = 32f;

    [Parameter] public bool UseInternalKeyboard { get; set; } = true;

    [Parameter] public bool ShowLink { get; set; } = true;

    [Parameter] public int Indent { get; set; } = 20;

    [Parameter] public bool OnlyHighlightText { get; set; } = true;

    [Parameter] public RenderFragment<TreeNode>? NodeTemplate { get; set; }

    [Parameter] public RenderFragment<TreeIconContext>? IconTemplate { get; set; }

    [Parameter] public RenderFragment<TreeEditContext>? EditTemplate { get; set; }

    [Parameter] public Func<TreeNode, bool>? BeforeExpand { get; set; }

    [Parameter] public Func<TreeNode, bool>? BeforeCollapse { get; set; }

    [Parameter] public Func<TreeDropEventArgs, bool>? BeforeDrop { get; set; }

    [Parameter] public Func<TreeNode, string, bool>? BeforeRename { get; set; }

    [Parameter] public Func<TreeNode, bool>? BeforeEditName { get; set; }

    [Parameter] public EventCallback<TreeSelectionChangedEventArgs> OnSelectionChanged { get; set; }

    [Parameter] public EventCallback<TreeExpandedChangedEventArgs> OnExpandedChanged { get; set; }

    [Parameter] public EventCallback<TreeNodeEventArgs> OnNodeClicked { get; set; }

    [Parameter] public EventCallback<TreeNodeEventArgs> OnNodeInvoked { get; set; }

    [Parameter] public EventCallback<TreeContextMenuEventArgs> OnContextMenu { get; set; }

    [Parameter] public EventCallback<TreeDropEventArgs> OnDrop { get; set; }

    [Parameter] public EventCallback<TreeEditEventArgs> OnEditCommitted { get; set; }

    [Parameter] public EventCallback<TreeNodeEventArgs> OnEditCancelled { get; set; }

    [Parameter] public EventCallback<TreeEditRejectedEventArgs> OnEditRejected { get; set; }

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>附加 HTML 属性（合并到 role="tree" 容器，冲突规则见 REQ-F-001）。</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    #endregion

    /// <summary>当前控制器（供消费方访问只读查询 API）。</summary>
    public TreeController? Controller => _controller;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // 虚拟化前置条件校验（计划 §5.12）：参数赋值前 fail-fast，避免进入错误状态
        var enableVirtualization = parameters.TryGetValue<bool>(nameof(EnableVirtualization), out var v) ? v : EnableVirtualization;
        var itemSize = parameters.TryGetValue<float>(nameof(ItemSize), out var s) ? s : ItemSize;
        var nodeTemplate = parameters.TryGetValue<RenderFragment<TreeNode>>(nameof(NodeTemplate), out var nt) ? nt : NodeTemplate;
        var editTemplate = parameters.TryGetValue<RenderFragment<TreeEditContext>>(nameof(EditTemplate), out var et) ? et : EditTemplate;

        TreeVirtualizationPolicy.Validate(enableVirtualization, itemSize, nodeTemplate is not null, editTemplate is not null);

        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        // 关闭多选：收敛选择集合为单节点（REQ-F-086 AC4）
        if (_lastSelectionMode == TreeSelectionMode.Multiple && SelectionMode == TreeSelectionMode.Single)
        {
            _controller?.ConvergeToSingleSelection();
        }

        _lastSelectionMode = SelectionMode;

        if (DataSource is null)
        {
            _controller = null;
            _boundDataSource = null;
            return;
        }

        if (_controller is null || !ReferenceEquals(_boundDataSource, DataSource))
        {
            // DataSource 替换：按稳定 Id 恢复展开/选中/焦点（REQ-F-061 AC2）
            var selectedIds = _controller?.SelectedIds;
            var expandedIds = _controller?.Flatten().Where(n => n.IsExpanded).Select(n => n.Id).ToHashSet();
            var focusId = _controller?.FocusedId;

            // 结束上一次编辑：复位旧节点编辑标志，避免复用同一 TreeNode 时标签空渲染
            if (_editingContext is not null)
            {
                _editingContext.Node.IsEditing = false;
                _editingContext = null;
            }

            // 虚拟化：DataSource 替换后 DOM 重建，清空 aria 焦点缓存待重新协调
            _ariaFocusId = null;

            _boundDataSource = DataSource;
            _controller = new TreeController(DataSource);
            _controller.RestoreState(selectedIds, expandedIds, focusId);
        }
        else
        {
            // 就地结构变更：重建索引与缓存，保留选择
            _controller.Refresh();
        }

        // guard 运行期替换同步（每次参数更新都同步，不只在重建分支）
        SyncGuards();

        // 受控参数同步（外部状态更新不得触发用户事件，REQ-F-008 / REQ-F-063 AC3）
        ApplyControlledSelection();
        ApplyControlledExpansion();
    }

    private void SyncGuards()
    {
        if (_controller is null)
        {
            return;
        }

        _controller.BeforeExpand = BeforeExpand;
        _controller.BeforeCollapse = BeforeCollapse;
        _controller.BeforeRename = BeforeRename;
        _controller.BeforeEditName = BeforeEditName;
    }

    private void ApplyControlledSelection()
    {
        if (_controller is null || SelectedIds is null)
        {
            return;
        }

        var nodes = SelectedIds.Select(id => _controller.FindById(id)).Where(n => n is not null).Cast<TreeNode>();
        _controller.ReplaceSelection(nodes);
    }

    private void ApplyControlledExpansion()
    {
        if (_controller is null || ExpandedIds is null)
        {
            return;
        }

        var expanded = ExpandedIds as ICollection<string> ?? ExpandedIds.ToHashSet();
        foreach (var node in _controller.Flatten())
        {
            node.IsExpanded = expanded.Contains(node.Id);
        }

        _controller.InvalidateVisible();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 拖拽运行期启停（对称、幂等）
        if (AllowDrag && DataSource is not null)
        {
            await EnsureDragInitializedAsync();
        }
        else
        {
            await DestroyDragAsync();
        }

        // 编辑态：聚焦编辑输入框（autofocus 对动态插入元素不可靠，REQ-F-088 AC1）
        // 仅默认 input 分支会捕获 _editInputRef；EditTemplate 路径下聚焦由应用自绘 UI 管理
        if (_editingContext is not null && EditTemplate is null)
        {
            await _editInputRef.FocusAsync();
        }

        // 虚拟化：跨视口导航后协调 aria-activedescendant（先滚动 → 等渲染 → 更新）
        if (EnableVirtualization)
        {
            await SyncVirtualizationFocusAsync();
        }
    }

    #region 鼠标事件

    private async Task HandleClick(MouseEventArgs args, TreeNode node)
    {
        // 双击互斥：抑制第二次 click 的通知（浏览器在双击时先发两次 click）
        var now = Environment.TickCount64;
        if (TreeInteraction.IsDoubleClick(_lastClickNode, node, _clickTicks, now, DbClickDelayMs))
        {
            _clickTicks = 0;
            return;
        }

        await HandleSelectionInteraction(args, node);

        // 禁用节点不发送点击通知（REQ-F-061 AC5，方案 §4 缺口 13）
        if (!node.IsDisabled)
        {
            await OnNodeClicked.InvokeAsync(new TreeNodeEventArgs(node));
        }

        _clickTicks = now;
        _lastClickNode = node;
    }

    private async Task HandleSelectionInteraction(MouseEventArgs args, TreeNode node)
    {
        if (_controller is null)
        {
            return;
        }

        // 点击即聚焦（含禁用节点，REQ-F-061 AC4：禁用节点可被导航识别）
        _controller.SetFocus(node);

        if (node.IsDisabled)
        {
            return;
        }

        var oldIds = _controller.SelectedIds.ToArray();
        var action = TreeInteraction.ResolveSelectionAction(
            SelectionMode == TreeSelectionMode.Multiple, args.ShiftKey, args.CtrlKey, _firstClickNode is not null);

        TreeOperationResult result;
        switch (action)
        {
            case TreeInteraction.SelectionAction.Range:
                result = _controller.SelectRange(_firstClickNode!, node);
                break;
            case TreeInteraction.SelectionAction.Toggle:
                _firstClickNode = node;
                result = _controller.ToggleSelection(node);
                break;
            default:
                _firstClickNode = node;
                result = _controller.Select(node);
                break;
        }

        await NotifySelectionChangedAsync(oldIds, result);
    }

    private async Task HandleDblClick(TreeNode node)
    {
        if (node.IsDisabled)
        {
            return;
        }

        await OnNodeInvoked.InvokeAsync(new TreeNodeEventArgs(node));
    }

    private async Task HandleRightClick(MouseEventArgs args, TreeNode node)
    {
        await OnContextMenu.InvokeAsync(new TreeContextMenuEventArgs(node, args));
    }

    #endregion

    #region 键盘

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        // 编辑态：容器不拦截按键（字符输入/Enter/Escape 交由编辑 input 自身处理）
        if (_editingContext is not null)
        {
            return;
        }

        await OnKeyDown.InvokeAsync(args);

        if (!UseInternalKeyboard || _controller is null)
        {
            return;
        }

        var oldIds = _controller.SelectedIds.ToArray();
        var result = TreeKeyboardNavigation.Navigate(_controller, args.Key);

        if (result.Expanded is not null)
        {
            await NotifyExpansionChangedAsync(result.Expanded, isExpanded: true);
        }

        if (result.Collapsed is not null)
        {
            await NotifyExpansionChangedAsync(result.Collapsed, isExpanded: false);
        }

        if (result.SelectionChanged)
        {
            await NotifySelectionChangedAsync(oldIds, TreeOperationResult.ChangedResult);
        }

        if (result.Changed)
        {
            StateHasChanged();
        }
    }

    #endregion

    #region 状态变更通知（受控/非受控分流）

    private async Task NotifySelectionChangedAsync(IReadOnlyCollection<string> oldIds, TreeOperationResult result)
    {
        if (!result.Changed || _controller is null)
        {
            return;
        }

        if (SelectedIds is not null)
        {
            await SelectedIdsChanged.InvokeAsync(_controller.SelectedIds.ToArray());
        }
        else
        {
            await OnSelectionChanged.InvokeAsync(new TreeSelectionChangedEventArgs(oldIds, _controller.SelectedIds.ToArray()));
        }
    }

    private async Task NotifyExpansionChangedAsync(TreeNode node, bool isExpanded)
    {
        if (ExpandedIds is not null)
        {
            var ids = _controller!.Flatten().Where(n => n.IsExpanded).Select(n => n.Id).ToArray();
            await ExpandedIdsChanged.InvokeAsync(ids);
        }
        else
        {
            await OnExpandedChanged.InvokeAsync(new TreeExpandedChangedEventArgs(node, isExpanded));
        }
    }

    #endregion

    #region 程序化操作

    /// <summary>程序化替换选中集合（不触发用户选择事件，REQ-F-063 AC3）。</summary>
    public void ReplaceSelection(IEnumerable<TreeNode> nodes)
    {
        _controller?.ReplaceSelection(nodes);
        StateHasChanged();
    }

    /// <summary>强制重渲染并失效控制器缓存（消费方在就地变更后调用）。</summary>
    public void Refresh()
    {
        _controller?.Refresh();
        StateHasChanged();
    }

    /// <summary>进入编辑态（初始化编辑上下文，供消费方通过 F2 等触发，REQ-F-088 AC1）。</summary>
    public bool BeginEdit(TreeNode node)
    {
        if (_controller is null || !_controller.BeginEdit(node))
        {
            return false;
        }

        // 连续 BeginEdit 时先结束上一次编辑，保证同一时刻只有一个节点处于编辑态
        if (_editingContext is not null && !ReferenceEquals(_editingContext.Node, node))
        {
            _editingContext.Node.IsEditing = false;
        }

        _editingContext = new TreeEditContext(node, node.Text, CommitEditAsync, CancelEdit);
        StateHasChanged();
        return true;
    }

    private async Task CommitEditAsync()
    {
        var context = _editingContext;
        if (context is null || _controller is null)
        {
            return;
        }

        // BeforeRename 否决：不接受结果，保持编辑态并给出反馈（REQ-F-088 AC2）
        if (!_controller.TryRename(context.Node, context.Value))
        {
            context.ValidationMessage ??= "Rename rejected";
            StateHasChanged();
            await OnEditRejected.InvokeAsync(new TreeEditRejectedEventArgs(context.Node, context.Value, context.ValidationMessage));
            return;
        }

        _editingContext = null;
        context.Node.IsEditing = false;
        StateHasChanged();
        await OnEditCommitted.InvokeAsync(new TreeEditEventArgs(context.Node, context.Value));
    }

    private void CancelEdit()
    {
        var context = _editingContext;
        if (context is null)
        {
            return;
        }

        _editingContext = null;
        context.Node.IsEditing = false;
        StateHasChanged();
        _ = OnEditCancelled.InvokeAsync(new TreeNodeEventArgs(context.Node));
    }

    #endregion
}
