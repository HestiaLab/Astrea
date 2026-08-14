# Astrea

Enterprise-grade Blazor UI component library targeting .NET 6, built around correctness, accessibility, and testability. The first component is **Tree**, a full-featured tree with multi-select, drag & drop, inline editing, and ARIA keyboard navigation.

## Tree Features

- **Multi-select** — Ctrl toggles selection, Shift selects a range; disabling multi-select converges to the focused node.
- **Drag & drop** — `before` / `inside` / `after` placement; `BeforeDrop` veto; built-in rejection of self-reference, cross-tree, and cycles.
- **Inline editing** — F2 to enter, Enter to commit, Esc to cancel; `BeforeRename` veto.
- **Context menu notification** — the tree only notifies (node + position); the app renders the menu and it does not change selection or focus.
- **ARIA keyboard navigation** — Arrow keys / Home / End / Space / Enter, single tab stop, `aria-activedescendant`.
- **Controlled or uncontrolled** — `SelectedIds` / `ExpandedIds` `null` = uncontrolled, non-`null` = controlled.
- **Optional virtualization** — fixed row height only; `EnableVirtualization` defaults to `false`.
- **Robustness** — disabled nodes, empty state, focus recovery, state preserved across `DataSource` replacement by stable id.

## Quick Start

1. Reference the `Astrea` project and add the namespace to `_Imports.razor`:

```razor
@using Astrea.Components.Tree
```

2. Link the stylesheet in `wwwroot/index.html`:

```html
<link href="_content/Astrea/astrea-tree.css" rel="stylesheet" />
```

3. Minimal example:

```razor
<Tree DataSource="_roots" />

@code {
    private readonly List<TreeNode> _roots = new()
    {
        new TreeNode("Root") { IsExpanded = true }
    };
}
```

## Tree Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `DataSource` | `IReadOnlyList<TreeNode>?` | required | Root nodes |
| `SelectionMode` | `TreeSelectionMode` | `Single` | `Single` or `Multiple` |
| `SelectedIds` | `IReadOnlyCollection<string>?` | `null` | Controlled selection; non-`null` = controlled |
| `SelectedIdsChanged` | `EventCallback<IReadOnlyCollection<string>>` | — | Fired when controlled selection changes |
| `ExpandedIds` | `IReadOnlyCollection<string>?` | `null` | Controlled expansion; non-`null` = controlled |
| `ExpandedIdsChanged` | `EventCallback<IReadOnlyCollection<string>>` | — | Fired when controlled expansion changes |
| `AllowDrag` | `bool` | `false` | Enable drag & drop |
| `EnableVirtualization` | `bool` | `false` | Fixed-row-height virtualization |
| `ItemSize` | `float` | `32f` | Row height in px; must be > 0 when virtualizing |
| `UseInternalKeyboard` | `bool` | `true` | Built-in ARIA keyboard navigation |
| `ShowLink` | `bool` | `true` | Show parent–child connector lines |
| `Indent` | `int` | `20` | Indent per level in px |
| `OnlyHighlightText` | `bool` | `true` | Highlight only the label on selection |
| `NodeTemplate` | `RenderFragment<TreeNode>?` | `null` | Custom node content (incompatible with virtualization) |
| `IconTemplate` | `RenderFragment<TreeIconContext>?` | `null` | Custom node icon |
| `EditTemplate` | `RenderFragment<TreeEditContext>?` | `null` | Custom edit UI (incompatible with virtualization) |
| `BeforeExpand` | `Func<TreeNode, bool>?` | `null` | Veto expansion; `false` blocks |
| `BeforeCollapse` | `Func<TreeNode, bool>?` | `null` | Veto collapse; `false` blocks |
| `BeforeDrop` | `Func<TreeDropEventArgs, bool>?` | `null` | Veto a drop; `false` blocks |
| `BeforeRename` | `Func<TreeNode, string, bool>?` | `null` | Veto a rename; `false` blocks |
| `BeforeEditName` | `Func<TreeNode, bool>?` | `null` | Veto entering edit mode; `false` blocks |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | — | Extra HTML attributes merged onto the `role="tree"` container |

## Tree Events

| Event | EventArgs | Description |
|---|---|---|
| `OnSelectionChanged` | `TreeSelectionChangedEventArgs` | Fired once when the selection actually changes (`OldSelectedIds`, `SelectedIds`) |
| `OnExpandedChanged` | `TreeExpandedChangedEventArgs` | Fired when expansion changes (`Node`, `IsExpanded`) |
| `OnNodeClicked` | `TreeNodeEventArgs` | Fired on click of a non-disabled node |
| `OnNodeInvoked` | `TreeNodeEventArgs` | Fired on double-click activation |
| `OnContextMenu` | `TreeContextMenuEventArgs` | Right-click notification (`Node`, `MouseArgs`); tree does not render a menu |
| `OnDrop` | `TreeDropEventArgs` | Fired once after protocol, structure, and guard checks pass (`SourceNodes`, `TargetNode`, `Relation`) |
| `OnEditCommitted` | `TreeEditEventArgs` | Fired when an edit is committed (`Node`, `Value`) |
| `OnEditCancelled` | `TreeNodeEventArgs` | Fired when an edit is cancelled |
| `OnEditRejected` | `TreeEditRejectedEventArgs` | Fired when a rename is vetoed (`Node`, `Value`, `Reason`) |
| `OnKeyDown` | `KeyboardEventArgs` | Fired before internal keyboard navigation |

## Public Methods

| Member | Description |
|---|---|
| `Controller` | Read-only access to `TreeController` (query + operation API) |
| `BeginEdit(TreeNode node)` | Enter edit mode for a node; returns `false` when vetoed |
| `Refresh()` | Invalidate caches and re-render after in-place data changes |

## Core Types

### TreeNode

```csharp
var node = new TreeNode("Label", icon: null, bindData: null, id: "stable-id");
node.Add(new TreeNode("Child"));
```

- **Identity**: `Id` (app-provided ids survive rebuilds; auto-assigned otherwise), `Text` (rejects null/empty), `BindData`, `PlaceHolder`.
- **Structure**: `Parent`, `Children`, `Index`, `Level`, `IsRoot`, `HasChildren`, `IsLeaf`, `Root`, `Count`, `Siblings()`, `GetPathToRoot()`, `IsDescendantOf(node)`, `IsAncestorOf(node)`.
- **Structure change**: `Add`, `Remove`, `Insert`, `AddChildren`, `InsertChildren`, `RemoveChildren`. Cyclic attachment (self, or an ancestor below its descendant) throws.
- **Style**: `SetCssClass(string)`, `CssClassResolver` (dynamic segment), `CssClass`.
- **Icons**: `Icon`, `UnFoldIcon`, `ExpandedIcon`, `CollapsedIcon`, `ExpandIcon`, `FoldIcon`, `IsExpandIconDisable`, `IsFoldIconDisable`, `IsExpandDisable`.
- **View state**: `IsExpanded` (writable), `IsSelected` and `IsFocused` (derived from the controller, read-only), `IsDisabled` (writable), `IsEditing` (maintained by the component).
- **Facade methods** (delegate to the controller, no-op when unbound): `GetSelectedNodes()`, `SetFocus()`, `UnFocus()`, `UnSelectAll()`, `SelectAllExcludeSelf()`, `Toggle()`, `ExpandAll()`, `CollapseAll()`, `ExpandAllParents()`, `FindNode(id)`, `FindNode(bindData)`, `FindNodes(bindDatas)`.

### TreeController (via `Tree.Controller`)

- **Query**: `SelectedIds`, `FocusedId`, `FocusedNode`, `Revision`, `Roots`, `FindById(id)`, `FindByBindData(bindData)`, `FindByText(text)`, `FindAll(predicate)`, `Flatten()`, `FlattenVisible()`, `GetSelectedNodes()`.
- **Selection**: `Select(node)`, `AddToSelection(node)`, `ToggleSelection(node)`, `ReplaceSelection(nodes)`, `ClearSelection()`, `ConvergeToSingleSelection()`, `SelectRange(from, to)`, `ClearSelectionSubtree(root)`, `SelectAll(root, excludeSelf)`, `SetFocus(node)`, `ClearFocus()`.
- **Expansion**: `Expand(node)`, `Collapse(node)`, `ExpandAll()`, `CollapseAll()`, `ExpandAllParents(node)`.
- **Edit**: `BeginEdit(node)`, `TryRename(node, newValue)`, `EndEdit(node)`.
- **Drag**: `CanDrop(sources, target)`.

### Enums and Contexts

- `TreeSelectionMode`: `Single`, `Multiple`.
- `TreeDropRelation`: `Before`, `Inside`, `After`.
- `TreeEditContext` (in `EditTemplate`): `Node`, `Value`, `SetValue(value)`, `CommitAsync()`, `Cancel()`, `ValidationMessage`.
- `TreeIconContext` (in `IconTemplate`): `Node`, `IsExpanded`, `HasChildren`, `IsDisabled`.

## Examples

### Multi-select

```razor
<Tree DataSource="_roots" SelectionMode="TreeSelectionMode.Multiple"
      OnSelectionChanged="OnSelectionChanged" />

@code {
    private void OnSelectionChanged(TreeSelectionChangedEventArgs args)
        => Console.WriteLine($"selection: {args.SelectedIds.Count} node(s)");
}
```

### Drag & drop with veto

```razor
<Tree DataSource="_roots" AllowDrag="true"
      BeforeDrop="BeforeDrop" OnDrop="OnDrop" />

@code {
    private bool BeforeDrop(TreeDropEventArgs args)
        => args.TargetNode.Text != "Resources"; // forbid dropping into "Resources"

    private void OnDrop(TreeDropEventArgs args)
    {
        // args.SourceNodes / args.TargetNode / args.Relation (Before | Inside | After)
        // The app decides the final structure and updates its data source.
    }
}
```

### Inline editing

```razor
<Tree DataSource="_roots" BeforeRename="BeforeRename"
      OnEditCommitted="OnEditCommitted" OnKeyDown="OnKeyDown" @ref="_tree" />

@code {
    private Tree? _tree;

    private bool BeforeRename(TreeNode node, string value) => !string.IsNullOrWhiteSpace(value);

    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "F2" && _tree?.Controller?.FocusedNode is { } node)
            _tree.BeginEdit(node);
    }

    private void OnEditCommitted(TreeEditEventArgs args) => args.Node.Text = args.Value;
}
```

### Context menu notification

```razor
<Tree DataSource="_roots" OnContextMenu="OnContextMenu" />

@code {
    private void OnContextMenu(TreeContextMenuEventArgs args)
    {
        // args.Node / args.MouseArgs — the tree only notifies; render your own menu.
    }
}
```

### Fixed-row-height virtualization

```razor
<div style="height: 320px;">
    <Tree DataSource="_largeDataSource" EnableVirtualization="true" ItemSize="32" />
</div>
```

Virtualization requires a bounded-height scroll container and a fixed row height. `ItemSize` must match the node height, and `NodeTemplate` / `EditTemplate` cannot be combined with virtualization.

### Custom icon and edit templates

```razor
<Tree DataSource="_roots">
    <IconTemplate Context="icon">
        <span class="@(icon.IsExpanded ? "folder-open" : "folder")"></span>
    </IconTemplate>
    <EditTemplate Context="edit">
        <input value="@edit.Value"
               @oninput="e => edit.SetValue(e.Value?.ToString() ?? "")"
               @onkeydown="e => OnEditKey(e, edit)" />
    </EditTemplate>
</Tree>

@code {
    private Task OnEditKey(KeyboardEventArgs e, TreeEditContext edit)
    {
        if (e.Key == "Enter")
        {
            return edit.CommitAsync();
        }

        if (e.Key == "Escape")
        {
            edit.Cancel();
        }

        return Task.CompletedTask;
    }
}
```

## Accessibility (WAI-ARIA)

Tree follows the [WAI-ARIA Tree View Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/): a single tab stop on the container (`role="tree"` with `tabindex="0"`), `role="treeitem"` nodes with `tabindex="-1"`, and focus tracked through `aria-activedescendant`. `aria-level`, `aria-selected`, `aria-expanded`, and `aria-disabled` are emitted. Disabled nodes are navigable but not selectable, expandable, or activatable.

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md). Requirements are traceable to [docs/SRS.md](docs/SRS.md) (REQ-F-xxx / REQ-NF-xxx).

Verification commands (all must pass after any change):

```sh
dotnet build Astrea.sln
dotnet test Astrea.sln --no-build
dotnet format Astrea.sln --verify-no-changes --no-restore
python3 scripts/verify_srs_consistency.py
```
