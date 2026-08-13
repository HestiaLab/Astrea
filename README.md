# Astrea

企业级 Blazor UI 组件库（.NET 6），强调正确性、可访问性、可测试性。首个组件：**Tree**（全功能树）。

## Tree 特性

- **多选** — Ctrl 切换选择、Shift 范围选择；关闭多选自动收敛为焦点节点
- **拖拽重排** — 上/中/下放置位置；`BeforeDrop` 应用否决；内置拒绝自引用/跨树/循环
- **内联编辑** — F2 进入、Enter 提交、Esc 取消；`BeforeRename` 应用否决
- **右键通知** — 树只发通知（含节点与位置），菜单由应用自行渲染
- **ARIA 键盘导航** — 方向键 / Home / End / Space / Enter，roving tabindex
- **稳健性** — 禁用节点、空状态、焦点恢复、DataSource 替换后按稳定 Id 保持状态

## 快速开始

1. 引用 `Astrea` 项目，在 `_Imports.razor` 添加：

```razor
@using Astrea.Components.Tree
```

2. 在 `wwwroot/index.html` 引入样式：

```html
<link href="_content/Astrea/astrea-tree.css" rel="stylesheet" />
```

3. 最小示例：

```razor
<Tree DataSource="_roots" OnSelect="OnSelect" />

@code {
    private readonly List<TreeNode> _roots = new()
    {
        new TreeNode("根节点") { IsExpanded = true }
    };

    private void OnSelect(IReadOnlyCollection<TreeNode> nodes) { }
}
```

## Tree 参数与事件

### 参数

| 参数 | 类型 | 说明 |
|---|---|---|
| `DataSource` | `IReadOnlyList<TreeNode>?` | 树数据源（根节点集合） |
| `Settings` | `TreeSettings?` | 行为配置（多选/拖拽/虚拟化等） |
| `Callbacks` | `TreeCallbacks?` | 操作前否决回调 |

### 事件

| 事件 | 签名 | 说明 |
|---|---|---|
| `OnSelect` | `IReadOnlyCollection<TreeNode>` | 用户选择变化（外部 `ReplaceSelection` 不触发） |
| `OnClick` / `OnDblClick` | `TreeNode` | 单击 / 双击 |
| `OnContextMenu` | `(MouseEventArgs, TreeNode)` | 右键触发 |
| `OnDrop` | `DragNodesArgs` | 拖放（应用据此更新结构） |
| `OnInputEnter` | `(TreeNode, string)` | 编辑提交（节点 + 新值） |
| `OnExpand` / `OnCollapse` | `TreeNode` | 展开 / 折叠 |
| `OnKeyDown` | `KeyboardEventArgs` | 键盘事件（先于内部导航触发） |

### 公共方法

| 方法 | 说明 |
|---|---|
| `Controller` | 获取控制器（选择/焦点/查询 API） |
| `ReplaceSelection(nodes)` | 程序化替换选中（不触发 `OnSelect`） |
| `BeginEdit(node)` | 进入编辑态（供 F2 等触发） |
| `Refresh()` | 就地变更后失效缓存并重渲染 |

## 核心类型

### TreeNode

```csharp
var node = new TreeNode("标签", icon: null, bindData: null, id: "stable-id");
node.Add(new TreeNode("子节点"));
```

- **身份**：`Id`（应用提供则跨重建保持）、`Text`、`BindData`、`PlaceHolder`
- **结构**：`Parent` / `Children` / `Index` / `Level` / `IsRoot` / `HasChildren` / `IsLeaf` / `Root`；`Add` / `Remove` / `Insert` / `AddChildren` / `RemoveChildren`
- **状态**：`IsExpanded` / `IsVisible` / `IsSelected` / `IsFocused` / `IsEditing` / `IsDisabled`
- **图标**：`Icon` / `UnFoldIcon` / `ExpandedIcon` / `CollapsedIcon`
- **样式**：`SetCssClass(基础段)` + `CssClassResolver`（动态段）

### TreeSettings

| 属性 | 默认 | 说明 |
|---|---|---|
| `AllowDrag` | `false` | 允许拖拽 |
| `AllowMultiSelect` | `false` | 允许多选（Ctrl/Shift） |
| `UseInternalKeyboard` | `true` | 内部键盘导航 |
| `EnableVirtualization` | `true` | 虚拟化渲染 |
| `ShowLink` | `true` | 显示父子连接线 |
| `HideRootNode` | `false` | 隐藏根节点 |
| `Indent` / `ItemSize` | `20` / `32f` | 缩进像素 / 虚拟化单项高度 |
| `OnlyHighlightText` | `true` | 选中仅高亮文字 |
| `NodeTemplate` / `EditTemplate` | — | 自定义节点 / 编辑模板 |

### TreeCallbacks

| 回调 | 签名 | 返回 `false` 时 |
|---|---|---|
| `BeforeExpand` / `BeforeCollapse` | `TreeNode → bool` | 否决展开 / 折叠 |
| `BeforeDrop` | `DragNodesArgs → bool` | 否决拖放 |
| `BeforeRename` | `(TreeNode, string) → bool` | 否决编辑新值 |
| `BeforeEditName` | `TreeNode → bool` | 否决进入编辑 |
| `BeforeRemove` | `TreeNode → bool` | 否决移除 |

## 功能示例

### 多选

```razor
<Tree DataSource="_roots" Settings="_settings" OnSelect="OnSelect" />

@code {
    private readonly TreeSettings _settings = new() { AllowMultiSelect = true };
}
```

### 拖拽（含否决）

```razor
<Tree DataSource="_roots" Settings="_settings" Callbacks="_callbacks" OnDrop="OnDrop" />

@code {
    private readonly TreeSettings _settings = new() { AllowDrag = true };
    private readonly TreeCallbacks _callbacks = new();

    protected override void OnInitialized()
        => _callbacks.BeforeDrop = args => args.Target.Text != "资源"; // 禁止拖入"资源"

    private void OnDrop(DragNodesArgs args)
    {
        // 应用根据 args.Sources / args.Target / args.Position 更新树结构（如纳入撤销/重做）
    }
}
```

### 内联编辑

```razor
<Tree DataSource="_roots" Callbacks="_callbacks" OnInputEnter="OnInputEnter" OnKeyDown="OnKeyDown" @ref="_tree" />

@code {
    private Tree? _tree;
    private readonly TreeCallbacks _callbacks = new();

    protected override void OnInitialized()
        => _callbacks.BeforeRename = (node, value) => !string.IsNullOrWhiteSpace(value); // 否决空值

    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "F2" && _tree?.Controller?.Selection.FocusedNode is { } node)
            _tree.BeginEdit(node);
    }

    private void OnInputEnter((TreeNode, string) args) => args.Item1.Text = args.Item2;
}
```

### 右键通知

```razor
<Tree DataSource="_roots" OnContextMenu="OnContextMenu" />

@code {
    private void OnContextMenu((MouseEventArgs, TreeNode) args)
    {
        // 树只通知，菜单由应用渲染（args.Item1 为鼠标位置，args.Item2 为节点）
    }
}
```

## 可访问性（WAI-ARIA）

Tree 遵循 [WAI-ARIA Tree View Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/)：单一 tab 入口、roving tabindex、方向键移动焦点、`role=tree` / `role=treeitem` / `aria-level` / `aria-selected` / `aria-expanded` / `aria-disabled`。禁用节点可被导航识别，但不可选择/展开/激活。

## 开发与贡献

见 [CONTRIBUTING.md](CONTRIBUTING.md)。需求可追溯到 [docs/SRS.md](docs/SRS.md)（REQ-F-xxx）。

验证命令（改动后必须全绿）：

```sh
dotnet build Astrea.sln
dotnet test Astrea.sln --no-build
dotnet format Astrea.sln --verify-no-changes --no-restore
python3 scripts/verify_srs_consistency.py
```
