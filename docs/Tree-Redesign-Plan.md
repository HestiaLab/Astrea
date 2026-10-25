# Astrea Tree 组件重设计与修订方案

## 1. 文档信息

| 项目 | 内容 |
|---|---|
| 状态 | Historical（历史设计计划；当前状态见 SRS v0.7.0 与需求矩阵） |
| 适用范围 | `src/Astrea/Components/Tree/`、`src/Astrea/wwwroot/astrea-tree.*`、Tree 测试、Demo、README 与 SRS 中的 Tree 契约 |
| 目标框架 | .NET 6.0，兼容 Blazor Server 与 Blazor WebAssembly |
| 当前基线 | `dotnet build Astrea.sln` 0 警告 0 错误；`dotnet test Astrea.sln --no-build` 174/174 通过；JS 单测 2/2 通过 |
| 本文性质 | Tree 整改合同、实施顺序与验收基线；Phase 0 的可观察行为和公共 API 迁移方向已冻结 |

> 历史说明（2026-10-25）：本文档记录 Tree 重设计阶段的方案与验收合同，不是当前发布状态的权威来源。当前需求与证据由 [docs/SRS.md](SRS.md) v0.7.0、[requirements-test-matrix.md](requirements-test-matrix.md) 与 [release-evidence.md](release-evidence.md) 跟踪；文中 174/174 等基线数字已过时。

## 2. 目标

在不引入业务依赖、不改变“应用决定最终树结构”这一原则的前提下，修正 Tree 当前在状态所有权、树结构不变量、浏览器真实焦点、JS 生命周期、拖拽协议、模板扩展与虚拟化方面的设计问题，使组件满足以下目标：

1. 组件状态只有一个权威来源，外部输入、内部交互与事件通知之间不存在双写或绕过路径。
2. 任意公共操作都不能构造循环树、重复身份、跨 Tree 节点引用或禁用节点非法状态。
3. 键盘焦点是浏览器和辅助技术可观察的真实焦点，不只是 `IsFocused` CSS 标记。
4. 参数在运行期变化时，Controller、渲染状态和 JS 注册同步更新。
5. 拖拽消息以稳定身份和拖拽会话为依据，生产端与消费端使用同一份源节点事实。
6. 自定义图标和编辑模板能够完整接入组件行为，不强制特定图标技术。
7. 虚拟化是可选优化，不改变 Tree 的焦点、选择、编辑和可访问性语义。
8. 公共 API 对普通 Blazor 使用方可理解、可绑定、可测试，不要求依赖可写的内部 Controller。

## 3. 非目标

本轮不实现以下内容：

- REQ-F-065 懒加载；该需求当前为 Deferred，应在基础状态模型稳定后单独设计。
- 跨 Tree 拖放。
- Tree 自行修改应用的数据结构或内置撤销/重做。
- 触屏专用拖拽协议。
- 与 Tree 无关的其他 Astrea 组件。
- 提升目标框架或引入业务依赖。

## 4. 已确认的现状与设计缺口

| # | 现状证据 | 设计缺口 | 影响 |
|---|---|---|---|
| 1 | `Tree.Render.cs:29-39` 给容器 `tabindex="0"`，`Tree.Render.cs:91-95` 又给逻辑焦点节点 `tabindex="0"` | 同时混用容器焦点与 roving tabindex；节点状态变化后没有移动真实 DOM 焦点；容器无条件阻止所有按键默认行为 | 可能出现两个 Tab 入口、辅助技术焦点与视觉焦点不一致，且 Tab 可能无法离开 Tree |
| 2 | `TreeNode.cs:177-240` 公开修改结构但不阻止自身或祖先挂接 | 树结构不变量不受保护 | 可能形成循环，导致 `Root`、`Level` 和遍历无限循环或栈溢出 |
| 3 | `TreeController.cs:110-120` 对重复 ID 使用覆盖写入 | 稳定身份冲突被静默接受 | 查询、状态恢复、拖拽目标和 DOM 身份可能指向错误节点 |
| 4 | `TreeController.Selection` 和多种可写方法均公开 | 外部可绕过节点归属、禁用状态和单选模式校验 | 选择/焦点状态可包含其他 Tree 的节点，所谓“唯一真相”不成立 |
| 5 | `Tree.cs:82-99` 只在 DataSource 引用变化时重建 Controller | `Callbacks` 等运行期变化不能完整生效 | 参数显示值与实际行为分离 |
| 6 | `Tree.cs:102-108` 仅在 `firstRender` 初始化拖拽 | `AllowDrag` 或 DataSource 后续启用时不注册；关闭时不注销 | 运行期参数变化无效，JS 生命周期不对称 |
| 7 | `Tree.DragDrop.cs:55` 忽略 JS 传回的源 ID，改用 `_dragNodes` | JS 与 C# 各自持有一份拖拽源状态 | 陈旧事件、选择变化或伪造消息可能造成源集合不一致 |
| 8 | `DragNodesArgs.cs:28-34` 将未知位置解释为 Bottom | 协议错误被降级为合法操作 | 损坏消息可能产生错误重排 |
| 9 | `TreeSettings.EditTemplate` 仅传入 `TreeNode`，而提交/取消方法均为 private | 自定义编辑模板无法访问编辑缓冲、提交、取消或校验结果 | 模板 API 名义可扩展、实际不可完成完整编辑流程 |
| 10 | `TreeNode` 使用字符串图标，`Tree.Render.cs:183-190` 固定渲染 SVG `<use>` | 图标技术被锁定为 SVG symbol；无图标时仍保留空位 | 与 REQ-F-064 的技术中立和无无意义空位要求冲突 |
| 11 | `Tree.Render.cs:58-61` 使用 `10 + i` 作为循环 sequence | 把运行时索引当作源码位置 | 插入、删除和重排时可能产生不稳定或低效的 Blazor diff |
| 12 | `TreeSettings.EnableVirtualization` 默认 true，同时允许任意模板 | 固定 `ItemSize` 与可变行高模板冲突 | 测量、滚动、焦点和编辑行为可能失真 |
| 13 | 禁用节点点击后 `Tree.cs:154-155` 仍无条件发出选择结果和点击通知 | 命令没有返回“是否改变”结果，组件无法准确决定通知 | 空通知、重复通知或禁用节点语义不清 |
| 14 | `TreeController.cs:69-101` 只检查根节点的 `IsVisible` | 属性名称与实际作用范围不一致 | 消费方必须知道隐式前置条件 |
| 15 | 节点 DOM `id` 直接使用业务 `TreeNode.Id`，JS 又调用 `document.getElementById` | 两个 Tree 使用相同逻辑 ID 时发生全局 DOM 冲突 | 拖拽、ARIA 引用和元素查询可能跨组件串线 |
| 16 | Tree 未提供 `AdditionalAttributes` | Tree 不能完整满足 REQ-F-001 的通用组件契约 | 使用方无法可靠提供 `aria-label`、测试属性或宿主 HTML 属性 |
| 17 | SRS 文件头仍标记 0.3.0，而变更记录已经出现 0.4.0 Tree 扩充 | 需求基线元数据不一致 | 实施和验收无法明确引用同一版本基线 |

## 5. 总体设计决策

### 5.1 保留的设计

以下方向正确，应保留：

- Tree 组件负责渲染和事件接入，纯 C# Controller 负责查询、校验和状态转换。
- 节点使用应用提供的稳定逻辑 ID；未提供时可生成进程内 ID。
- 拖放成功后只通知应用，不直接修改应用数据结构。
- 选择与焦点是不同概念；移动焦点不得隐式改变选择。
- 核心纯 C# 逻辑继续由 MSTest 覆盖。

### 5.2 状态所有权

采用“每个 Tree 实例一个 `TreeState`”的状态模型，不再以 `TreeNode.IsSelected`、`TreeNode.IsFocused`、`TreeNode.IsEditing` 作为权威状态。

`TreeState` 至少持有：

- `SelectedIds`：选中的稳定 ID 集合。
- `ExpandedIds`：展开的稳定 ID 集合。
- `FocusedId`：Tree 内部当前焦点 ID。
- `EditSession`：当前编辑节点 ID、原值、缓冲值与提交状态。
- `Revision`：结构刷新后递增，用于拒绝陈旧拖拽会话。

状态规则：

1. `TreeNode` 只表达节点数据和结构；交互状态由 `TreeState` 表达。
2. Controller 是 `TreeState` 的唯一写入口；组件和外部调用均不能直接写集合。
3. 所有写操作先校验节点归属、禁用状态、当前模式和结构 revision，再原子提交。
4. 所有写操作返回明确结果，例如 `Changed`、`Rejected`、`NoOp`，组件只在 `Changed` 时发送变化通知。
5. 数据源替换时按稳定 ID 对状态求交集；不存在的选择、展开、焦点和编辑会话被清理，不把清理报告为用户操作。

### 5.3 受控与非受控模式

选择和展开同时支持 Blazor 的受控与非受控模式，但同一状态不能同时由 Node 属性和组件参数控制。

推荐契约：

```csharp
[Parameter] public IReadOnlyCollection<string>? SelectedIds { get; set; }
[Parameter] public EventCallback<IReadOnlyCollection<string>> SelectedIdsChanged { get; set; }
[Parameter] public IReadOnlyCollection<string>? ExpandedIds { get; set; }
[Parameter] public EventCallback<IReadOnlyCollection<string>> ExpandedIdsChanged { get; set; }
```

语义：

- 参数为 `null`：非受控模式，由组件内部 `TreeState` 持有状态，并通过 Changed 事件通知应用。
- 参数非 `null`：受控模式，参数是显示状态的权威来源；用户操作只提出变化并触发 Changed，下一次渲染以应用回传参数为准。
- 受控模式下，组件在 Changed 没有订阅者、应用拒绝候选集合或延迟回传时都不得先行改变显示状态。
- 应用更换受控集合属于外部同步，不作为用户操作重复触发业务事件。
- 禁止再从 `TreeNode.IsSelected`、`IsFocused`、`IsEditing` 同步状态。
- `TreeNode.IsExpanded` 若为兼容迁移保留，只能作为首次绑定的初始值；稳定版前应由 `ExpandedIds` 完全替代，避免长期双写。

### 5.4 TreeNode 边界

`TreeNode` 保留节点构建能力，但必须维护最小结构不变量：

- 拒绝 `node.Add(node)`。
- 拒绝把祖先挂到后代。
- 拒绝空白 Text 和空白显式 ID；Text 后续赋值也执行同样校验。
- 同一父节点重复 Add 为幂等 no-op；跨父移动必须保持 Parent、Index 一致。
- `Children` 返回稳定只读视图，不能暴露可强制转换后修改的内部 List。
- Controller 的 `Bind/Refresh` 对整棵树执行完整校验：重复 ID、循环、重复节点引用、Parent/Index 不一致均产生确定性异常，不允许静默修复。
- 结构变更优先通过 Controller 的 `AddNode`、`RemoveNode`、`MoveNodes` 等入口；外部直接修改后仍可调用 `Refresh`，但 `Refresh` 必须重新验证并递增 revision。

### 5.5 公共查询视图边界

`Tree.Controller` 不再作为公共入口。新的 `Tree.View` 只提供只读查询，不暴露可写的 `TreeSelection` 或实际可向下转换的内部可变集合。所有修改类命令通过 Tree 门面进入同一校验与提交边界：

```csharp
public IReadOnlyList<TreeNode> VisibleNodes { get; }
public IReadOnlyCollection<string> SelectedIds { get; }
public string? FocusedId { get; }
public TreeNode? FindById(string id);
```

不接受任意 `TreeNode` 作为跨边界身份；公共命令优先接受稳定 ID。若为迁移保留 Node 重载，必须先验证该实例就是当前查询索引中的实例。查询面不提供选择、展开、编辑或结构修改方法。

### 5.6 焦点与键盘模型

采用容器真实焦点 + `aria-activedescendant` 模型：

- `role="tree"` 容器是唯一 Tab 入口，`tabindex="0"`。
- 所有 `role="treeitem"` 节点使用 `tabindex="-1"`。
- 容器通过 `aria-activedescendant` 指向当前已渲染节点的 DOM ID。
- ArrowUp、ArrowDown、ArrowLeft、ArrowRight、Home、End、Space 和 Enter 由组件处理；只有这些已识别且实际处理的按键调用 `preventDefault`。
- Tab、Shift+Tab、浏览器快捷键和未识别按键不得被阻止。
- 焦点变化后容器仍持有真实 DOM 焦点，辅助技术通过 active descendant 获得当前节点名称和状态。
- DataSource 为空时容器仍保持稳定可访问，但不设置失效的 `aria-activedescendant`。
- 节点 DOM ID 使用组件实例前缀生成，例如 `ast-tree-{treeInstanceId}-node-{safeToken}`；逻辑 ID 单独放入 `data-node-id`，不得直接作为全局 DOM ID。

虚拟化开启时，新的 active descendant 必须已经渲染。跨视口导航的顺序为：请求滚动目标索引 → 等待目标行渲染 → 更新 `aria-activedescendant`。在这一链路具备真实浏览器证据前，不得宣称虚拟化模式满足完整键盘需求。

### 5.7 事件模型

将原始输入事件、状态变化和业务激活分开：

- `OnSelectionChanged`：仅在用户操作实际改变选择集合后触发一次，参数包含旧集合、新集合和输入来源。
- `OnExpandedChanged`：仅在用户展开状态实际改变后触发。
- `OnNodeInvoked`：表示 Enter、双击等业务激活；禁用节点不触发。
- `OnNodeClicked`：如保留，只表示原始指针通知，不等同于激活，文档必须明确。
- `OnContextMenu`：继续只通知，不改变选择和焦点。
- `OnDrop`：只在拖拽协议、结构规则和应用 guard 全部通过后触发一次。
- `OnEditCommitted`、`OnEditCancelled`、`OnEditRejected`：明确区分提交、取消和否决；REQ-F-088 AC2 要求的“应用收到否决结果”必须有可观察通道。

所有公开事件参数使用命名 EventArgs 类型，不继续使用 `(TreeNode, string)`、`(MouseEventArgs, TreeNode)` 等元组，以保证 API 可发现性和后续兼容扩展。

### 5.8 Guard 回调

现有 `TreeCallbacks` 是可变对象，且同步 `Func<..., bool>` 无法支持异步应用规则。推荐将 guard 作为直接参数或不可变配置，并使用 `ValueTask<bool>`：

```csharp
[Parameter] public Func<TreeExpandRequest, ValueTask<bool>>? BeforeExpand { get; set; }
[Parameter] public Func<TreeCollapseRequest, ValueTask<bool>>? BeforeCollapse { get; set; }
[Parameter] public Func<TreeDropRequest, ValueTask<bool>>? BeforeDrop { get; set; }
[Parameter] public Func<TreeRenameRequest, ValueTask<bool>>? BeforeRename { get; set; }
```

约束：

- guard 运行期间不提交状态。
- guard 返回后必须重新检查 revision 和目标身份，避免等待期间数据源已变化。
- guard 抛出异常时操作保持未提交，异常按 Blazor 事件异常路径上抛，不静默接受。
- guard 返回后若组件已释放、节点已被替换/移除或操作会话已失效，该结果只能被拒绝或视为 no-op，不得改变新状态。
- `BeforeRemove` 当前没有生产调用点；除非 SRS 增加 Tree 内部移除操作，否则删除该死 API。

### 5.9 拖拽协议

拖拽消息必须携带并验证以下字段：

```text
protocolVersion
sessionId
sourceTreeId
sourceNodeIds
sourceRevision
targetTreeId
targetNodeId
relation = before | inside | after
```

规则：

1. JS 和 C# 只使用消息中的 `sourceNodeIds`，不再维护第二份 `_dragNodes` 真相。
2. C# 根据稳定 ID 重新解析节点，拒绝不存在、重复、跨 Tree、陈旧 revision 或不属于当前拖拽会话的消息。
3. 未知 relation 必须拒绝，使用 `TryParse`，不得回退为 after。
4. “跨树”按 Tree 实例 ID 判断，不按每个根节点的 `Root` 判断；同一 Tree 的多个根之间默认允许拖放。
5. 多选集合同时含祖先和后代时，推荐只保留最高层祖先作为实际移动源，避免同一子树被移动两次；该行为在实施前写入 SRS 或公共文档。
6. before/after 以目标稳定 ID + relation 表达，不在协议中传递含糊的 DOM 索引。
7. 指针反馈由 JS 层维护，不再为每次 dragover 调用 C# `StateHasChanged`；C# 只处理开始、提交、取消等语义边界。
8. JS 查询限制在当前 Tree 容器内，不调用全局 `document.getElementById(nodeId)` 查找业务节点。
9. destroy 时移除全部监听、插入线、预览、requestAnimationFrame 和对象引用。
10. 拖拽会话是一次性令牌；接受、拒绝、取消、`AllowDrag=false` 或释放组件都必须使其失效，后续重放不得再触发 `OnDrop`。

### 5.10 JS 生命周期

组件分别持有并释放以下资源：

- ES module `IJSObjectReference`。
- initialize 返回的实例对象。
- `DotNetObjectReference<Tree>`。

状态转换：

| 原状态 | 新状态 | 动作 |
|---|---|---|
| 未注册 | `AllowDrag=true` 且容器已渲染 | 初始化 |
| 已注册 | `AllowDrag=false` | destroy 并释放实例 |
| 已注册 | Tree 容器身份变化 | 先 destroy，再以新身份初始化 |
| 任意 | 组件 Dispose | 幂等释放全部资源 |

Blazor Server 释放阶段允许捕获并忽略仅表示连接已断开的 `JSDisconnectedException`，不得吞掉其他异常。

### 5.11 图标与模板

图标从字符串 SVG 引用改为技术中立模板：

```csharp
[Parameter] public RenderFragment<TreeIconContext>? IconTemplate { get; set; }
```

`TreeIconContext` 至少包含 Node、IsExpanded、HasChildren 和 IsDisabled。默认实现可以继续支持 SVG symbol 便利参数，但必须是可选适配层，不是唯一机制。没有图标时不渲染图标容器和保留空位；装饰图标使用 `aria-hidden="true"`。

编辑模板改为：

```csharp
[Parameter] public RenderFragment<TreeEditContext>? EditTemplate { get; set; }
```

`TreeEditContext` 至少提供：

- `Node`
- `Value`
- `SetValue(string value)`
- `CommitAsync()`
- `Cancel()`
- `ValidationMessage` 或拒绝结果

默认模板由组件管理焦点；自定义模板必须通过上下文注册焦点目标，或由契约明确要求模板自行聚焦。组件在自定义模板分支不得继续聚焦未捕获的默认 `_editInputRef`。

### 5.12 虚拟化

- `EnableVirtualization` 默认值改为 false。
- 只有固定行高模式允许启用现有 `Virtualize<TreeNode>`；`ItemSize` 必须大于 0，并与 CSS 节点高度保持一致。
- 使用可变高度 NodeTemplate 或 EditTemplate 时，组件拒绝开启固定行高虚拟化，或文档明确该组合不受支持；不得静默产生错误布局。
- 虚拟化模式必须验证展开/折叠后索引、Home/End、跨视口方向键、编辑焦点、拖拽目标、DataSource 替换和空数据。
- 5 万节点 Demo 只是性能探针，不代替可访问性和交互正确性测试。

### 5.13 可见性与合成根范围

推荐删除通用 `TreeNode.IsVisible`；应用通过 DataSource 是否包含节点决定显示。如果确需保留过滤能力，必须对所有层级定义一致语义，而不能只过滤根节点。

本次整改不引入 `HideRootNode` 或“隐藏合成根”公共能力。当前 Active SRS 未要求该行为，当前公共实现也不提供该参数；应用应把希望显示为一级的节点直接作为 DataSource 根集合。若未来需要合成根，必须先增加独立 Active 需求和验收条件，不得以隐式兼容行为加入。

### 5.14 AdditionalAttributes 与宿主元素

Tree 增加：

```csharp
[Parameter(CaptureUnmatchedValues = true)]
public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
```

按 REQ-F-001 的冲突规则合并到 `role="tree"` 容器：

- 应用 class 与组件 class 同时保留。
- 应用 style 覆盖同名 CSS 声明，其他组件默认样式继续生效。
- `role`、`tabindex`、`aria-activedescendant` 及组件计算的状态 ARIA 属性由组件保证。
- 其他属性使用应用值。
- 文档示例必须演示 `aria-label` 或 `aria-labelledby`，确保 Tree 有可访问名称。

## 6. 推荐公共 API 轮廓

以下轮廓冻结公共能力与迁移方向；不影响语义的具体命名可在 API 审阅阶段微调：

```csharp
public sealed partial class Tree : ComponentBase, IAsyncDisposable
{
    [Parameter, EditorRequired] public IReadOnlyList<TreeNode> DataSource { get; set; }
    [Parameter] public TreeSelectionMode SelectionMode { get; set; }
    [Parameter] public IReadOnlyCollection<string>? SelectedIds { get; set; }
    [Parameter] public EventCallback<IReadOnlyCollection<string>> SelectedIdsChanged { get; set; }
    [Parameter] public IReadOnlyCollection<string>? ExpandedIds { get; set; }
    [Parameter] public EventCallback<IReadOnlyCollection<string>> ExpandedIdsChanged { get; set; }
    [Parameter] public bool AllowDrag { get; set; }
    [Parameter] public bool EnableVirtualization { get; set; }
    [Parameter] public float ItemSize { get; set; }
    [Parameter] public RenderFragment<TreeNode>? NodeTemplate { get; set; }
    [Parameter] public RenderFragment<TreeIconContext>? IconTemplate { get; set; }
    [Parameter] public RenderFragment<TreeEditContext>? EditTemplate { get; set; }
    [Parameter] public EventCallback<TreeSelectionChangedEventArgs> OnSelectionChanged { get; set; }
    [Parameter] public EventCallback<TreeNodeEventArgs> OnNodeInvoked { get; set; }
    [Parameter] public EventCallback<TreeContextMenuEventArgs> OnContextMenu { get; set; }
    [Parameter] public EventCallback<TreeDropEventArgs> OnDrop { get; set; }
    [Parameter] public EventCallback<TreeEditEventArgs> OnEditCommitted { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}
```

程序化修改只通过 Tree 门面完成；`Tree.View` 仅提供只读查询：

```csharp
Task<TreeOperationResult> SelectAsync(string id, TreeSelectionIntent intent = TreeSelectionIntent.Replace);
Task<TreeOperationResult> FocusAsync(string id);
Task<TreeOperationResult> BeginEditAsync(string id);
Task<TreeOperationResult> RefreshAsync();
```

不再要求使用方读取 `Controller.Selection.FocusedNode`、直接修改 Selection，或传递任意 Node 集合给 `ReplaceSelection`。

## 7. 兼容与迁移策略

当前仓库未发现语义版本发布 tag，建议在稳定发布前进行一次明确的 API 收口，而不是长期维护两套状态模型。实施前仍需确认是否已有外部 NuGet 或项目消费者；若已有，使用一个过渡版本。

| 旧 API | 新 API/行为 | 处理方式 |
|---|---|---|
| `TreeSettings.AllowMultiSelect` | `SelectionMode` | 先标记 Obsolete，一个过渡版本后删除 |
| `TreeSettings.AllowDrag` | `AllowDrag` 直接参数 | 同上 |
| `TreeSettings.EnableVirtualization` | `EnableVirtualization` 直接参数，默认 false | 行为变更写入发布说明 |
| `TreeCallbacks` | 直接异步 guard 参数 | 保留同步适配器一个过渡版本，禁止两者同时配置 |
| `OnSelect` | `OnSelectionChanged` + `SelectedIdsChanged` | 提供适配通知，明确不会对 no-op 触发 |
| `OnInputEnter` | `OnEditCommitted` | EventArgs 替代元组 |
| `OnContextMenu` 元组 | `TreeContextMenuEventArgs` | EventArgs 替代元组 |
| `Controller.Selection` 可写对象 | 只读状态 + 校验命令 | 直接移除可写入口；编译错误优于静默不一致 |
| `Tree.Controller` 及其公开修改方法 | `Tree.View` 只读查询 + Tree 异步命令门面 | 查询迁移到 `View`，写入迁移到使用稳定 ID 的 Tree 命令 |
| 可向下转换的选择/展开集合 | 不可变快照或真正只读视图 | 移除对内部集合的引用泄漏 |
| `TreeNode.IsSelected/IsFocused/IsEditing` | `TreeState` | 先标记 Obsolete 且停止作为权威状态，稳定版前删除 |
| 字符串 `Icon` 系列 | `IconTemplate`/默认 SVG 适配器 | 保留默认适配器，模板成为主契约 |
| `DragPosition` class | 受限 enum 或可拒绝解析的值类型 | 未知输入必须失败 |
| `Refresh()` | `RefreshAsync()` 返回结果 | 明确验证失败和 revision 变化 |

迁移期必须定义优先级；推荐新旧 API 同时提供时直接抛出配置异常，避免猜测哪个生效。

## 8. 分阶段实施计划

### Phase 0：需求和 API 基线冻结

**目标：** 在代码变更前确认所有可观察行为和破坏性 API 决策。

**文档：**

- 修改 `docs/SRS.md`：修正版本元数据；核对 REQ-F-061、062、063、064、066、077、086、087、088、089；明确受控状态、多根范围/拖拽、禁用范围节点、祖先/后代同时选中拖拽、异步应用决策、编辑否决反馈和拖拽单次消费语义；记录隐藏合成根不属于本次范围。
- 不把 `TreeState`、`aria-activedescendant`、具体类名等实现细节写入 SRS；这些只保留在设计文档。
- 修改 `README.md`：在代码完成后同步最终 API，不提前宣传未实现能力。

**验收：**

- `python3 scripts/verify_srs_consistency.py` 输出 `ALL CHECKS PASSED`。
- API 审阅确认受控/非受控模式、破坏性迁移范围、合成根范围决策和拖拽源归一化规则。

### Phase 1：结构不变量与 TreeState

**目标：** 建立单一状态源和节点归属校验，不涉及 DOM 与 JS。

**主要文件：**

- 修改 `src/Astrea/Components/Tree/TreeNode.cs`。
- 修改 `src/Astrea/Components/Tree/TreeController.cs`。
- 修改 `src/Astrea/Components/Tree/TreeSelection.cs`，最终由 `TreeState` 替代或降为内部实现。
- 新建 `src/Astrea/Components/Tree/TreeState.cs`。
- 新建 `src/Astrea/Components/Tree/TreeOperationResult.cs`。
- 新建/扩充 `tests/Astrea.Tests/Tree/TreeNodeTests.cs`、`TreeControllerTests.cs`、`TreeSelectionTests.cs`。

**RED 测试：** 自身挂接、祖先回挂、重复 ID、重复节点实例、外部节点选择、禁用节点程序化选择、状态恢复求交集、Refresh 失败不部分提交、revision 递增。

**验收：** 纯 C# 状态不变量测试全绿；Controller 不再公开可写 Selection。

### Phase 2：公共参数、事件与运行期参数同步

**目标：** 落地受控/非受控状态与命名 EventArgs，消除可变 Settings/Callbacks 的行为漂移。

**主要文件：**

- 修改 `src/Astrea/Components/Tree/Tree.cs`。
- 修改或替换 `TreeSettings.cs`、`TreeCallbacks.cs`。
- 新建 `TreeEventArgs.cs`、`TreeRequests.cs`。
- 增加组件级参数生命周期测试。

**RED 测试：** DataSource 同引用就地变化、DataSource 新引用同 ID、SelectedIds 外部替换不回报用户事件、受控值未回传时显示不变、AllowMultiSelect 关闭收敛、Callbacks/guard 替换、禁用/no-op 不通知。

**验收：** 每个用户操作至多产生一次对应通知；外部状态更新不伪装成用户操作。

### Phase 3：渲染身份、焦点与键盘

**目标：** 修复真实 DOM 焦点、单一 Tab 入口、sequence、ARIA 和 AdditionalAttributes。

**主要文件：**

- 修改 `src/Astrea/Components/Tree/Tree.Render.cs`。
- 修改 `src/Astrea/Components/Tree/TreeKeyboardNavigation.cs`。
- 修改 `src/Astrea/wwwroot/astrea-tree.css`。
- 增加组件渲染测试和浏览器焦点测试。

**RED 测试：** 只有容器可 Tab 进入；Tab/Shift+Tab 可离开；方向键更新 active descendant；空 Tree 不引用不存在元素；两个 Tree 使用相同逻辑 ID 不产生 DOM 冲突；插入/删除/重排后节点身份稳定；ARIA level/selected/expanded/disabled 正确；AdditionalAttributes 冲突规则正确。

**验收：** 真实浏览器中焦点、键盘和辅助技术可观察状态一致，不能只凭纯 C# 测试判定通过。

### Phase 4：JS 生命周期与拖拽协议

**目标：** 统一拖拽消息事实，修复动态启停、作用域查询和资源释放。

**主要文件：**

- 修改 `src/Astrea/Components/Tree/Tree.DragDrop.cs`。
- 修改 `src/Astrea/Components/Tree/DragNodesArgs.cs` 或以新协议类型替代。
- 新建 `src/Astrea/Components/Tree/Tree.Interop.cs`。
- 修改 `src/Astrea/wwwroot/astrea-tree.js`。
- 增加 JS 纯函数测试、C# 消费端敌对输入测试和真实浏览器拖拽测试。

**RED 测试：** 未知 relation、陈旧 revision、未知 ID、重复 ID、跨 Tree、同 Tree 跨根、自引用、循环、祖先+后代源归一化、AllowDrag 动态启停、重复初始化、Dispose 幂等、两个 Tree 隔离。

**验收：** 生产端可生成、消费端可验证、当前打包资产可运行三层证据齐全；不得只测试 `CanDrop` 纯函数。

### Phase 5：编辑与图标模板

**目标：** 让默认模板和自定义模板共享完整行为契约。

**主要文件：**

- 修改 `Tree.Render.cs`、`Tree.cs`、`TreeNode.cs`。
- 新建 `TreeEditContext.cs`、`TreeIconContext.cs`。
- 增加默认模板、自定义模板和无图标布局测试。

**RED 测试：** 默认输入自动聚焦；Enter 只提交一次；blur 重入不重复提交；Escape 取消不通知结果；guard 否决保持编辑且产生拒绝反馈；自定义模板可更新/提交/取消；无图标不保留空位；装饰图标不加入可访问名称。

**验收：** REQ-F-064 与 REQ-F-088 每个 AC 都有可定位证据。

### Phase 6：虚拟化

**目标：** 在非虚拟化正确基线上恢复可选固定行高虚拟化。

**主要文件：**

- 修改 `Tree.Render.cs`、`TreeSettings.cs` 或新公共参数类型。
- 扩展 JS 滚动/定位能力，但不让 JS持有业务状态。
- 增加大数据与跨视口浏览器测试。

**RED 测试：** 关闭虚拟化默认行为；非法 ItemSize；可变高度模板组合；Home/End 跳转；跨视口 Arrow 导航；展开折叠后焦点；编辑节点进入视口；5 万节点基本交互。

**验收：** 非虚拟化和虚拟化模式语义一致；性能结果记录环境、数据和测量方式，不以单次 Demo 日志代替发布证据。

### Phase 7：文档、Demo、包消费与发布验证

**目标：** 确保源码、文档、Demo 和最终消费资产描述同一套行为。

**主要文件：**

- 修改 `src/Astrea.Demo/Pages/Index.razor`。
- 修改 `README.md`。
- 更新 `docs/SRS.md` 的追溯和变更记录。
- 按需要增加临时外部消费者项目；验证后删除，不留仓库产物。

**验收：**

- README 示例由普通外部消费者可编译，不使用 internal API。
- Demo 覆盖受控选择、动态 AllowDrag、两个 Tree 相同逻辑 ID、自定义图标和编辑模板。
- 打包后由外部 Blazor Server 与 Blazor WebAssembly 消费探针编译并运行。
- 发布记录列出实际验证的浏览器和版本。

## 9. 测试分层

| 层级 | 工具方向 | 覆盖内容 | 不能替代 |
|---|---|---|---|
| 纯 C# 单元测试 | 现有 MSTest | 结构不变量、状态转换、选择范围、拖放校验、状态恢复 | DOM、CSS、焦点、JS 生命周期 |
| 组件渲染测试 | 评估兼容 .NET 6 的测试方案后引入，仅作为测试依赖 | 参数更新、事件次数、DOM 属性、模板分支、sequence 回归 | 浏览器布局和真实焦点 |
| JS 单元测试 | 无 DOM 或轻量 DOM 测试 | 位置解析、消息构造、作用域查询、会话状态 | Blazor 回调与打包资产 |
| 真实浏览器测试 | 建议建立独立 BrowserTests 项目 | Tab、方向键、ARIA active descendant、虚拟化、拖拽、编辑焦点、CSS 几何 | 发布包外部消费 |
| 外部消费者探针 | 临时 Blazor Server/WASM 项目引用打包产物 | 公共 API、静态资源路径、包内容、运行兼容 | 多浏览器一致性 |

新增测试依赖前必须记录维护状态、安全历史、依赖树和许可证；依赖仅进入测试项目，不进入 `Astrea` 运行时包。

## 10. 每阶段验证命令

每次代码或 SRS 变更后执行完整门禁：

```sh
dotnet build Astrea.sln
dotnet test Astrea.sln --no-build
dotnet format Astrea.sln --verify-no-changes --no-restore
python3 scripts/verify_srs_consistency.py
```

涉及 JS、真实焦点、拖拽、虚拟化或静态资源时，还必须执行对应 JS 测试、浏览器测试和打包资产验证；仅上述四条命令全绿不足以证明这些行为正确。

## 11. 风险与控制

| 风险 | 概率/影响 | 控制措施 |
|---|---|---|
| 公共 API 破坏已有消费者 | 中/高 | 实施前确认外部消费者；必要时提供一个 Obsolete 过渡版本；发布说明提供迁移表 |
| 受控/非受控状态产生事件循环 | 中/高 | 参数应用与用户命令分开；外部参数同步不触发用户事件；增加父子往返测试 |
| `aria-activedescendant` 与虚拟化目标未渲染 | 高/高 | 虚拟化后置；先完成非虚拟化焦点模型；跨视口采用滚动→渲染→激活顺序 |
| 异步 guard 返回时状态已变化 | 中/高 | 捕获 revision，返回后重新校验，提交前单一线性化点 |
| 拖拽协议重写造成视觉回归 | 中/中 | 检测逻辑、反馈 CSS 和提交协议分层测试；真实浏览器验证 top/inside/bottom |
| 兼容层继续保留双状态 | 中/高 | 兼容层只做单向适配；新旧参数冲突时失败；设定删除版本，不无限延期 |
| 组件测试库不支持目标框架 | 中/中 | 引入前做最小外部试验；不为了测试升级生产目标框架 |
| 方案范围过大导致一次性重写 | 中/高 | 按 Phase 独立提交和审阅；每阶段保持构建、测试和 SRS 门禁全绿 |

## 12. 审阅决策点

本次整改已冻结以下决策：

1. 在首次稳定发布前进行一次 API 收口；旧同步 guard 如保留，只作为一个过渡版的 Obsolete 适配器，与新 API 同时配置时确定性报错。
2. 容器焦点 + `aria-activedescendant` 是唯一焦点模型。
3. `SelectedIds`/`ExpandedIds` 为 `null` 时是非受控模式，非 `null` 时是 proposal-only 受控模式。
4. `TreeNode.IsSelected`、`IsFocused`、`IsEditing` 不再是可写权威状态；`IsExpanded` 如为迁移保留，只表达首次绑定的初始值。
5. 同一 Tree 多根之间允许范围选择和拖放；范围端点禁用时整次拒绝，中间禁用节点跳过。
6. 多选拖拽采用“祖先覆盖后代”的源归一化规则。
7. 删除没有 Active SRS 行为和生产调用点的 `BeforeRemove`。
8. 虚拟化默认关闭，固定行高是启用前提。
9. 建立独立真实浏览器测试项目；新增依赖仍须按 `AGENTS.md` 审批。
10. 本次整改不引入隐藏合成根能力。

## 13. 完成定义

只有同时满足以下条件，Tree 重设计才算完成：

- SRS 的 Tree Active 需求与实现、README、Demo 一致。
- Tree 结构不变量、状态所有权和外部参数同步有纯 C# 或组件测试证据。
- 键盘、真实焦点、ARIA、拖拽、编辑和虚拟化有真实浏览器证据。
- 两个 Tree 使用相同逻辑节点 ID 时完全隔离。
- 运行期启停拖拽不会遗漏注册、重复注册或泄漏 JS/.NET 对象引用。
- 未知、陈旧、跨 Tree 或结构非法的拖拽消息被确定性拒绝。
- 自定义图标与编辑模板可以完成文档承诺的全部流程。
- 完整质量门禁全绿，构建保持 0 警告 0 错误。
- 外部 Blazor Server 与 Blazor WebAssembly 消费探针能够引用最终包并运行核心 Tree 场景。
- 公共 API 变更已记录迁移说明，不再暴露可绕过不变量的可写内部状态。
