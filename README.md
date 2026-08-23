# Astrea

Enterprise-grade Blazor UI component library targeting .NET 6, built around correctness, accessibility, and testability. The first component is **Tree**, a full-featured tree with multi-select, drag & drop, inline editing, and ARIA keyboard navigation.

## Tree Features

- **Multi-select** — Ctrl toggles selection; Shift ranges may cross root boundaries in the same Tree, reject disabled endpoints, and skip disabled intermediate nodes.
- **Drag & drop** — `before` / `inside` / `after` placement; application veto; built-in rejection of self-reference, cross-tree, cycles, stale operations, and duplicate completion.
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
<Tree DataSource="_roots" aria-label="Project files" />

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
| `SelectedIds` | `IReadOnlyCollection<string>?` | `null` | `null` = uncontrolled; non-`null` is the authoritative controlled selection |
| `SelectedIdsChanged` | `EventCallback<IReadOnlyCollection<string>>` | — | Proposes the next controlled selection; does not commit it before the app supplies it |
| `ExpandedIds` | `IReadOnlyCollection<string>?` | `null` | `null` = uncontrolled; non-`null` is the authoritative controlled expansion |
| `ExpandedIdsChanged` | `EventCallback<IReadOnlyCollection<string>>` | — | Proposes the next controlled expansion; does not commit it before the app supplies it |
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

### Controlled state contract

- `SelectedIds="null"` and `ExpandedIds="null"` select uncontrolled mode. Tree owns that state and reports actual user changes.
- A non-`null` collection selects controlled mode, including an empty collection. It remains the displayed source of truth.
- In controlled mode, a user action proposes at most one next collection. The display does not change until the app supplies that collection, including when no changed callback is attached or the app rejects or delays the proposal.
- Replacing a controlled collection is an application update and does not emit a second user-change event.
- `DataSource="null"` and an empty data source both keep an empty, accessible Tree container. Tree does not fabricate a node.
- The redesign does not add a hidden synthetic-root mode. Supply the nodes that should appear at the first visible level directly as `DataSource` roots.

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
| `View` | Read-only Tree query view; returned collections are not mutable internal state |
| `SelectAsync`, `FocusAsync`, `BeginEditAsync`, `RefreshAsync` | Validated Tree commands identified by stable node id |

All state-changing public paths use the same ownership, disabled-node, selection-mode, revision, and session validation. A query object does not expose selection, expansion, editing, or structure mutation methods.

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
- **View state migration**: `IsSelected`, `IsFocused`, and `IsEditing` are never writable state authorities. `IsExpanded` is initial-bind compatibility only; subsequent expansion is owned by `ExpandedIds` or the uncontrolled Tree state. `IsDisabled` remains application data.
- **Legacy node facade methods**: query helpers migrate to `Tree.View`; state-changing helpers migrate to validated Tree commands and are not an alternative state authority.

### Tree query view (via `Tree.View`)

- **Query**: selected ids, focused id/node, revision, roots, stable-id lookup, and flattened visible nodes.
- **Commands**: selection, focus, expansion, editing, and refresh go through the Tree command facade rather than this query view.

### Enums and Contexts

- `TreeSelectionMode`: `Single`, `Multiple`.
- `TreeDropRelation`: `Before`, `Inside`, `After`.
- `TreeEditContext` (in `EditTemplate`): `Node`, `Value`, `SetValue(value)`, `CommitAsync()`, `Cancel()`, `ValidationMessage`.
- `TreeIconContext` (in `IconTemplate`): `Node`, `IsExpanded`, `HasChildren`, `IsDisabled`.

Each edit context belongs to exactly one edit session. Once another edit starts, its node is replaced or removed, or the component is disposed, calls from the older context cannot commit or cancel the current edit.

### API migration contract

| Legacy surface | Frozen direction |
|---|---|
| `Tree.Controller`, writable `Controller.Selection`, or mutable selected/expanded collections | Queries move to `Tree.View`; query results are immutable snapshots or true read-only views |
| State-changing methods on `TreeController` | Move to validated asynchronous Tree commands that use stable node ids |
| State-changing `TreeNode` facade methods | Move to the corresponding validated Tree command |
| Writable `TreeNode.IsSelected`, `IsFocused`, or `IsEditing` | Removed as authoritative state; query-only compatibility members may be transitional |
| `TreeNode.IsExpanded` as a second source of truth | Initial-bind compatibility only; `ExpandedIds`/Tree-owned expansion is authoritative afterward |
| Synchronous veto delegates | At most one obsolete transition adapter; asynchronous application decisions are the final contract, and configuring old and new forms together is an error |
| Tuple event payloads | Named EventArgs types |

Application decisions may complete asynchronously. Tree does not commit while waiting, revalidates the node and operation before applying the result, leaves state unchanged when the result is stale, and does not silently swallow decision errors. A drag operation is consumed at most once after acceptance, rejection, cancellation, disablement, or disposal.

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
        if (args.Key == "F2" && _tree?.View?.FocusedId is { } nodeId)
            await _tree.BeginEditAsync(nodeId);
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
