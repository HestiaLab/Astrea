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
    private TreeView? _view;
    private IReadOnlyList<TreeNode>? _boundDataSource;
    private TreeStructureSnapshot? _boundStructureSnapshot;
    private long _clickTicks;
    private TreeNode? _firstClickNode;
    private TreeNode? _lastClickNode;
    private TreeSelectionMode _lastSelectionMode;
    private TreeEditContext? _editingContext;
    private long _nextEditSessionId;
    private (long SessionId, int ValueVersion)? _suppressBlurCommit;
    private bool _suppressNodeInvoke;
    private ElementReference _editInputRef;
    private bool _editInputRendered;
    private ElementReference _containerRef;
    private bool _restoreFocusAfterEdit;
    private TreeNode? _keyboardRangeAnchor;
    private HashSet<string>? _appliedControlledExpandedIds;

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

    /// <summary>
    /// Detect in-place DataSource structure changes on every parent render. Disable for very large
    /// trees and call <see cref="RefreshAsync"/> after in-place mutations.
    /// </summary>
    [Parameter] public bool AutoDetectStructureChanges { get; set; } = true;

    [Parameter] public float ItemSize { get; set; } = 32f;

    [Parameter] public bool UseInternalKeyboard { get; set; } = true;

    [Parameter] public bool ShowLink { get; set; } = true;

    [Parameter] public int Indent { get; set; } = 20;

    [Parameter] public bool OnlyHighlightText { get; set; } = true;

    /// <summary>Virtualized loading placeholder text.</summary>
    [Parameter] public string LoadingText { get; set; } = "Loading items...";

    /// <summary>Default edit input accessible name; <c>{0}</c> is the node text.</summary>
    [Parameter] public string EditLabelFormat { get; set; } = "Edit {0}";

    /// <summary>Fallback validation message when a rename is vetoed.</summary>
    [Parameter] public string RenameRejectedText { get; set; } = "Rename rejected";

    [Parameter] public RenderFragment<TreeNode>? NodeTemplate { get; set; }

    [Parameter] public RenderFragment<TreeIconContext>? IconTemplate { get; set; }

    [Parameter] public RenderFragment<TreeEditContext>? EditTemplate { get; set; }

    [Parameter] public Func<TreeNode, bool>? BeforeExpand { get; set; }

    [Parameter] public Func<TreeNode, ValueTask<bool>>? BeforeExpandAsync { get; set; }

    [Parameter] public Func<TreeNode, bool>? BeforeCollapse { get; set; }

    [Parameter] public Func<TreeNode, ValueTask<bool>>? BeforeCollapseAsync { get; set; }

    [Parameter] public Func<TreeDropEventArgs, bool>? BeforeDrop { get; set; }

    [Parameter] public Func<TreeDropEventArgs, ValueTask<bool>>? BeforeDropAsync { get; set; }

    [Parameter] public Func<TreeNode, string, bool>? BeforeRename { get; set; }

    [Parameter] public Func<TreeNode, string, ValueTask<bool>>? BeforeRenameAsync { get; set; }

    [Parameter] public Func<TreeNode, bool>? BeforeEditName { get; set; }

    [Parameter] public Func<TreeNode, ValueTask<bool>>? BeforeEditNameAsync { get; set; }

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

    /// <summary>当前 Tree 的只读查询视图。</summary>
    public TreeView? View => _view;

    internal bool HasActiveEditSession => _editingContext is not null;

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
        ValidateGuardConfiguration();
        _controller?.SetSelectionMode(SelectionMode);

        // 关闭多选：收敛选择集合为单节点（REQ-F-086 AC4）
        if (_lastSelectionMode == TreeSelectionMode.Multiple && SelectionMode == TreeSelectionMode.Single)
        {
            _controller?.ConvergeToSingleSelection();
        }

        _lastSelectionMode = SelectionMode;

        if (DataSource is null)
        {
            EndEditingSession(restoreFocus: false);
            _keyboardRangeAnchor = null;
            _appliedControlledExpandedIds = null;
            ClearNodeTokens();
            _controller = null;
            _view = null;
            _boundDataSource = null;
            _boundStructureSnapshot = null;
            return;
        }

        if (_controller is null || !ReferenceEquals(_boundDataSource, DataSource))
        {
            // DataSource 替换：按稳定 Id 恢复展开/选中/焦点（REQ-F-061 AC2）
            var selectedIds = _controller?.SelectedIds;
            var expandedIds = _controller?.Flatten().Where(n => n.IsExpanded).Select(n => n.Id).ToHashSet();
            var focusId = _controller?.FocusedId;

            // 结束上一次编辑：复位旧节点编辑标志，避免复用同一 TreeNode 时标签空渲染
            EndEditingSession(restoreFocus: false);
            _keyboardRangeAnchor = null;
            _appliedControlledExpandedIds = null;

            // 虚拟化：DataSource 替换后 DOM 重建，清空 aria 焦点缓存待重新协调
            _ariaFocusId = null;

            _boundDataSource = DataSource;
            _controller = new TreeController(DataSource, SelectionMode);
            _view = new TreeView(_controller);
            _controller.RestoreState(selectedIds, expandedIds, focusId);
            _boundStructureSnapshot = TreeStructureSnapshot.Capture(DataSource);
            PruneNodeTokens();
        }
        else if (AutoDetectStructureChanges)
        {
            // 仅真实的就地结构变更才重建索引并递增 revision；父组件的无关重渲染不使活动会话陈旧。
            var currentSnapshot = TreeStructureSnapshot.Capture(DataSource);
            if (_boundStructureSnapshot is null || !_boundStructureSnapshot.Matches(currentSnapshot))
            {
                _controller.Refresh();
                _boundStructureSnapshot = TreeStructureSnapshot.Capture(DataSource);
                PruneNodeTokens();

                // 编辑节点被移除或替换后，旧会话不得继续渲染空编辑内容
                if (_editingContext is not null &&
                    !ReferenceEquals(_controller.FindById(_editingContext.Node.Id), _editingContext.Node))
                {
                    EndEditingSession(restoreFocus: false);
                }
            }
        }

        // 受控参数同步（外部状态更新不得触发用户事件，REQ-F-008 / REQ-F-063 AC3）
        ApplyControlledSelection();
        ApplyControlledExpansion();
    }

    private void ValidateGuardConfiguration()
    {
        EnsureSingleGuard(BeforeExpand, BeforeExpandAsync, nameof(BeforeExpand), nameof(BeforeExpandAsync));
        EnsureSingleGuard(BeforeCollapse, BeforeCollapseAsync, nameof(BeforeCollapse), nameof(BeforeCollapseAsync));
        EnsureSingleGuard(BeforeDrop, BeforeDropAsync, nameof(BeforeDrop), nameof(BeforeDropAsync));
        EnsureSingleGuard(BeforeRename, BeforeRenameAsync, nameof(BeforeRename), nameof(BeforeRenameAsync));
        EnsureSingleGuard(BeforeEditName, BeforeEditNameAsync, nameof(BeforeEditName), nameof(BeforeEditNameAsync));
    }

    private static void EnsureSingleGuard(Delegate? synchronous, Delegate? asynchronous, string syncName, string asyncName)
    {
        if (synchronous is not null && asynchronous is not null)
        {
            throw new InvalidOperationException($"Configure either {syncName} or {asyncName}, not both.");
        }
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
            _appliedControlledExpandedIds = null;
            return;
        }

        var expanded = ExpandedIds as ICollection<string> ?? ExpandedIds.ToHashSet();
        if (_appliedControlledExpandedIds is not null && _appliedControlledExpandedIds.SetEquals(expanded))
        {
            return;
        }

        _appliedControlledExpandedIds = new HashSet<string>(expanded);
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

        if (UseInternalKeyboard)
        {
            await EnsureKeyboardInitializedAsync();
        }
        else
        {
            await DestroyKeyboardAsync();
        }

        // 编辑态：聚焦编辑输入框（autofocus 对动态插入元素不可靠，REQ-F-088 AC1）
        // 仅默认 input 分支会捕获 _editInputRef；EditTemplate 路径下聚焦由应用自绘 UI 管理
        if (_editingContext is not null && EditTemplate is null)
        {
            // 虚拟化滚动可能先触发父级 OnAfterRender，而编辑输入框尚未进入本轮渲染；
            // 此时 ElementReference 尚未配置，聚焦会抛 InvalidOperationException。
            if (Js is not null && _editInputRendered)
            {
                await _editInputRef.FocusAsync();
            }
        }
        else if (_editingContext?.FocusTarget is { } customFocusTarget)
        {
            await customFocusTarget.FocusAsync();
        }
        else if (_restoreFocusAfterEdit)
        {
            _restoreFocusAfterEdit = false;
            await FocusContainerAsync();
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
        _keyboardRangeAnchor = null;

        // active-descendant 模型：DOM 焦点始终由 role=tree 容器持有
        await FocusContainerAsync();

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

        // 单独按下修饰键只改变后续按键状态，不得中断进行中的 Shift 范围锚点。
        if (args.Key is "Shift" or "Control" or "Alt" or "Meta")
        {
            return;
        }

        var oldIds = _controller.SelectedIds.ToArray();
        var oldExpandedIds = SnapshotExpandedIds();
        var revision = _controller.Revision;
        var focused = _controller.FocusedNode;
        if (args.Key == "Enter")
        {
            // 编辑提交（Enter）会与容器 Enter 激活共用同一次按键：编辑侧已消费，不再触发 OnNodeInvoked
            var suppressInvoke = _suppressNodeInvoke;
            _suppressNodeInvoke = false;
            if (!suppressInvoke && focused is not null && !focused.IsDisabled)
            {
                await OnNodeInvoked.InvokeAsync(new TreeNodeEventArgs(focused));
            }

            return;
        }

        var allowExpand = true;
        var allowCollapse = true;
        if (args.Key == "ArrowRight" && focused is not null && focused.HasChildren && !focused.IsExpanded)
        {
            allowExpand = await CanExpandAsync(focused) && IsCurrentNode(focused, revision);
        }
        else if (args.Key == "ArrowLeft" && focused?.IsExpanded == true)
        {
            allowCollapse = await CanCollapseAsync(focused) && IsCurrentNode(focused, revision);
        }

        var multipleSelection = SelectionMode == TreeSelectionMode.Multiple;
        var isRangeSpace = multipleSelection && args.ShiftKey && (args.Key == " " || args.Key == "Spacebar");
        var isRangeNavigation = multipleSelection && args.ShiftKey && IsRangeNavigationKey(args.Key);
        if (!isRangeSpace && !isRangeNavigation)
        {
            _keyboardRangeAnchor = null;
        }
        else if (_keyboardRangeAnchor is null && focused is not null && !focused.IsDisabled)
        {
            _keyboardRangeAnchor = focused;
        }

        var result = TreeKeyboardNavigation.Navigate(
            _controller,
            args.Key,
            allowExpand,
            allowCollapse,
            multipleSelection,
            args.ShiftKey,
            isRangeSpace || isRangeNavigation ? _keyboardRangeAnchor : null);

        if (result.Expanded is not null)
        {
            await NotifyExpansionChangedAsync(result.Expanded, isExpanded: true, oldExpandedIds);
        }

        if (result.Collapsed is not null)
        {
            await NotifyExpansionChangedAsync(result.Collapsed, isExpanded: false, oldExpandedIds);
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

    private Task HandleContainerFocus()
    {
        // 树容器获得焦点但尚未有焦点节点时，聚焦首个可见节点（REQ-F-066：树接收焦点时建立 active descendant）
        if (_controller is not null && _controller.FocusedNode is null)
        {
            var visible = _controller.FlattenVisible();
            if (visible.Count > 0)
            {
                _controller.SetFocus(visible[0]);
                StateHasChanged();
            }
        }

        return Task.CompletedTask;
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
            var proposedIds = _controller.SelectedIds.ToArray();
            RestoreSelection(oldIds);
            await SelectedIdsChanged.InvokeAsync(proposedIds);
        }
        else
        {
            await OnSelectionChanged.InvokeAsync(new TreeSelectionChangedEventArgs(oldIds, _controller.SelectedIds.ToArray()));
        }
    }

    private async Task NotifyExpansionChangedAsync(
        TreeNode node,
        bool isExpanded,
        IReadOnlyCollection<string> oldExpandedIds)
    {
        if (ExpandedIds is not null)
        {
            var ids = _controller!.Flatten().Where(n => n.IsExpanded).Select(n => n.Id).ToArray();
            RestoreExpansion(oldExpandedIds);
            await ExpandedIdsChanged.InvokeAsync(ids);
        }
        else
        {
            await OnExpandedChanged.InvokeAsync(new TreeExpandedChangedEventArgs(node, isExpanded));
        }
    }

    private IReadOnlyCollection<string> SnapshotExpandedIds()
        => _controller?.Flatten().Where(node => node.IsExpanded).Select(node => node.Id).ToArray()
            ?? Array.Empty<string>();

    private void RestoreSelection(IEnumerable<string> ids)
    {
        if (_controller is null)
        {
            return;
        }

        var nodes = ids
            .Select(_controller.FindById)
            .Where(node => node is not null && !node.IsDisabled)
            .Cast<TreeNode>();
        _controller.ReplaceSelection(nodes);
    }

    private void RestoreExpansion(IEnumerable<string> ids)
    {
        if (_controller is null)
        {
            return;
        }

        var expandedIds = ids as ISet<string> ?? ids.ToHashSet();
        foreach (var node in _controller.Flatten())
        {
            node.IsExpanded = expandedIds.Contains(node.Id);
        }

        _controller.InvalidateVisible();
    }

    private async ValueTask<bool> CanExpandAsync(TreeNode node)
        => BeforeExpandAsync is not null
            ? await BeforeExpandAsync(node)
            : BeforeExpand?.Invoke(node) != false;

    private async ValueTask<bool> CanCollapseAsync(TreeNode node)
        => BeforeCollapseAsync is not null
            ? await BeforeCollapseAsync(node)
            : BeforeCollapse?.Invoke(node) != false;

    private async ValueTask<bool> CanBeginEditAsync(TreeNode node)
        => BeforeEditNameAsync is not null
            ? await BeforeEditNameAsync(node)
            : BeforeEditName?.Invoke(node) != false;

    private async ValueTask<bool> CanRenameAsync(TreeNode node, string value)
        => BeforeRenameAsync is not null
            ? await BeforeRenameAsync(node, value)
            : BeforeRename?.Invoke(node, value) != false;

    private static bool IsRangeNavigationKey(string key)
        => key is "ArrowDown" or "ArrowUp" or "Home" or "End";

    private bool IsCurrentNode(TreeNode node, long revision)
        => _controller is not null &&
           _controller.Revision == revision &&
           ReferenceEquals(_controller.FindById(node.Id), node);

    #endregion

    #region 程序化操作

    /// <summary>按稳定 ID 选择节点；受控模式仅提出下一状态。</summary>
    public async Task<TreeOperationResult> SelectAsync(string nodeId)
    {
        if (_controller?.FindById(nodeId) is not { } node)
        {
            return TreeOperationResult.RejectedResult;
        }

        var oldIds = _controller.SelectedIds.ToArray();
        var result = _controller.Select(node);
        await NotifySelectionChangedAsync(oldIds, result);
        if (result.Changed)
        {
            StateHasChanged();
        }

        return result;
    }

    /// <summary>按稳定 ID 设置焦点。</summary>
    public Task<TreeOperationResult> FocusAsync(string nodeId)
    {
        var result = _controller?.FindById(nodeId) is { } node
            ? _controller.SetFocus(node)
            : TreeOperationResult.RejectedResult;
        if (result.Changed)
        {
            StateHasChanged();
        }

        return Task.FromResult(result);
    }

    /// <summary>按稳定 ID 设置展开状态；受控模式仅提出下一状态。</summary>
    public async Task<TreeOperationResult> SetExpandedAsync(string nodeId, bool isExpanded)
    {
        if (_controller?.FindById(nodeId) is not { } node || node.IsDisabled)
        {
            return TreeOperationResult.RejectedResult;
        }

        if (node.IsExpanded == isExpanded)
        {
            return TreeOperationResult.NoOpResult;
        }

        var revision = _controller.Revision;
        var allowed = isExpanded ? await CanExpandAsync(node) : await CanCollapseAsync(node);
        if (!allowed || !IsCurrentNode(node, revision))
        {
            return TreeOperationResult.RejectedResult;
        }

        var oldExpandedIds = SnapshotExpandedIds();
        var result = TreeOperationResult.From(isExpanded ? _controller.Expand(node) : _controller.Collapse(node));
        if (result.Changed)
        {
            await NotifyExpansionChangedAsync(node, isExpanded, oldExpandedIds);
            StateHasChanged();
        }

        return result;
    }

    /// <summary>显式刷新就地修改的数据源。</summary>
    public Task RefreshAsync()
    {
        _controller?.Refresh();
        if (DataSource is not null)
        {
            _boundStructureSnapshot = TreeStructureSnapshot.Capture(DataSource);
            PruneNodeTokens();
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>进入编辑态；异步决策返回后会重新验证节点与 revision。</summary>
    public async Task<bool> BeginEditAsync(string nodeId)
    {
        if (_controller?.FindById(nodeId) is not { } node || node.IsDisabled)
        {
            return false;
        }

        var revision = _controller.Revision;
        if (!await CanBeginEditAsync(node) || !IsCurrentNode(node, revision))
        {
            return false;
        }

        return BeginEditCore(node);
    }

    private bool BeginEditCore(TreeNode node)
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

        _controller.SetFocus(node);
        _editInputRef = default;
        _editInputRendered = false;
        _editingContext = new TreeEditContext(
            ++_nextEditSessionId,
            _controller.Revision,
            node,
            node.Text,
            CommitEditAsync,
            CancelEditAsync);
        _suppressBlurCommit = null;
        _suppressNodeInvoke = false;
        _restoreFocusAfterEdit = false;
        StateHasChanged();
        return true;
    }

    private async Task CommitEditAsync(TreeEditContext expectedContext)
    {
        var context = _editingContext;
        if (context is null ||
            _controller is null ||
            !ReferenceEquals(context, expectedContext) ||
            context.SourceRevision != _controller.Revision ||
            !ReferenceEquals(_controller.FindById(context.Node.Id), context.Node))
        {
            return;
        }

        // 否决：不接受结果，保持编辑态并给出反馈（REQ-F-088 AC2）。
        var allowed = await CanRenameAsync(context.Node, context.Value);
        if (!ReferenceEquals(_editingContext, context) || !IsCurrentNode(context.Node, context.SourceRevision))
        {
            return;
        }

        if (!allowed || !_controller.TryRename(context.Node, context.Value))
        {
            context.ValidationMessage ??= RenameRejectedText;
            StateHasChanged();
            await OnEditRejected.InvokeAsync(new TreeEditRejectedEventArgs(context.Node, context.Value, context.ValidationMessage));
            return;
        }

        EndEditingSession(restoreFocus: true, keepNodeInvokeSuppression: true);
        StateHasChanged();
        await OnEditCommitted.InvokeAsync(new TreeEditEventArgs(context.Node, context.Value));
    }

    private async Task CancelEditAsync(TreeEditContext expectedContext)
    {
        var context = _editingContext;
        if (context is null || !ReferenceEquals(context, expectedContext))
        {
            return;
        }

        EndEditingSession(restoreFocus: true);
        StateHasChanged();
        await OnEditCancelled.InvokeAsync(new TreeNodeEventArgs(context.Node));
    }

    private void EndEditingSession(bool restoreFocus, bool keepNodeInvokeSuppression = false)
    {
        if (_editingContext is not null)
        {
            _editingContext.Node.IsEditing = false;
            _editingContext = null;
        }

        _editInputRef = default;
        _editInputRendered = false;
        _suppressBlurCommit = null;
        if (!keepNodeInvokeSuppression)
        {
            _suppressNodeInvoke = false;
        }

        _restoreFocusAfterEdit = restoreFocus;
    }

    private async Task FocusContainerAsync()
    {
        if (Js is null || string.IsNullOrEmpty(_containerRef.Id))
        {
            return;
        }

        try
        {
            await _containerRef.FocusAsync();
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server 断线后容器已不可聚焦。
        }
    }

    #endregion
}
