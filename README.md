# Astrea

Enterprise-grade Blazor UI component library targeting .NET 6, built around correctness, accessibility, and testability. The current release delivers **Tree** and **ResourceTable** per [SRS v0.8.0](docs/SRS.md); all other planned components are [Deferred](docs/Roadmap.md) rather than part of the current release contract.

## Current Release Scope

| Component | Status | SRS |
|---|---|---|
| Tree | Active | §3.10 |
| ResourceTable | Active | §3.12 |
| Button, Alert, Divider, Input, Label, Modal, Progress, Select, Table | Deferred | Appendix B / [Roadmap](docs/Roadmap.md) |

## Demo Site

The Blazor WebAssembly demo is published as a GitHub Pages project site at <https://hestialab.github.io/Astrea/>. The deploy workflow rewrites `<base href>` to `/Astrea/` and copies the rewritten `index.html` to `404.html`, so direct links and refreshes reach the Blazor router.

All in-app links must remain relative to the `<base>` path (for example `components/tree`; use an empty `href` for the catalog home). Avoid root-absolute paths such as `/components/tree`, which resolve outside the project site. Pushes to `main` or `dev` deploy automatically; use the `Deploy demo to GitHub Pages` workflow's `workflow_dispatch` trigger to redeploy manually.

## Tree Features

- **Multi-select** — Ctrl toggles selection; Shift ranges may cross root boundaries in the same Tree, reject disabled endpoints, and skip disabled intermediate nodes. Keyboard: Space toggles the focused node; Shift+Arrow/Home/End extends a range from the keyboard anchor.
- **Drag & drop** — `before` / `inside` / `after` placement; application veto; built-in rejection of self-reference, cross-tree, cycles, stale operations, and duplicate completion.
- **Inline editing** — F2 to enter, Enter to commit, Esc to cancel; `BeforeRename` veto.
- **Context menu notification** — the tree only notifies (node + position); the app renders the menu and it does not change selection or focus.
- **ARIA keyboard navigation** — Arrow keys / Home / End / Space / Enter, single tab stop, `aria-activedescendant`.
- **Controlled or uncontrolled** — `SelectedIds` / `ExpandedIds` `null` = uncontrolled, non-`null` = controlled.
- **Optional virtualization** — fixed row height only; `EnableVirtualization` defaults to `false`.
- **Robustness** — disabled nodes, empty state, focus recovery, state preserved across `DataSource` replacement by stable id.

## Quick Start

> **Package identity (0.8.0):** the NuGet package ID, assembly name, and static asset base path are `Astrea.Components` (formerly `Astrea`). Update `PackageReference Include="Astrea.Components"` and change `_content/Astrea/` links to `_content/Astrea.Components/`; public namespaces remain `Astrea.Components.*`. Version 0.8.0 is the first release under the new package identity; component behavior is unchanged.

1. Install the NuGet package (or reference `src/Astrea.Components/Astrea.Components.csproj` for local development):

```sh
dotnet add package Astrea.Components --version 0.8.0
```

2. Add the namespace to `_Imports.razor`:

```razor
@using Astrea.Components.Tree
```

3. Link the stylesheet in `wwwroot/index.html`:

```html
<link href="_content/Astrea.Components/astrea-tree.css" rel="stylesheet" />
```

4. Minimal example:

```razor
<Tree DataSource="_roots" aria-label="Project files" />

@code {
    private readonly List<TreeNode> _roots = new()
    {
        new TreeNode("Root", isExpanded: true)
    };
}
```

### GitHub Packages

Releases are also published to this repository's GitHub Packages feed. GitHub Packages requires authentication even for public packages, so restoring from it needs a personal access token with `read:packages`. Add the source to your `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="astrea-github" value="https://nuget.pkg.github.com/HestiaLab/index.xml" />
  </packageSources>
  <packageSourceCredentials>
    <astrea-github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_TOKEN" />
    </astrea-github>
  </packageSourceCredentials>
</configuration>
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
| `AutoDetectStructureChanges` | `bool` | `true` | Detect in-place structure changes on each render; disable for large trees and call `RefreshAsync` after in-place mutations |
| `ItemSize` | `float` | `32f` | Row height in px; must be > 0 when virtualizing |
| `UseInternalKeyboard` | `bool` | `true` | Built-in ARIA keyboard navigation |
| `ShowLink` | `bool` | `true` | Draw parent–child connector lines in the indentation area |
| `BeforeExpandAsync` | `Func<TreeNode, ValueTask<bool>>?` | `null` | Async expansion veto; configure instead of `BeforeExpand` |
| `BeforeCollapseAsync` | `Func<TreeNode, ValueTask<bool>>?` | `null` | Async collapse veto; configure instead of `BeforeCollapse` |
| `BeforeDropAsync` | `Func<TreeDropEventArgs, ValueTask<bool>>?` | `null` | Async drop veto; configure instead of `BeforeDrop` |
| `BeforeRenameAsync` | `Func<TreeNode, string, ValueTask<bool>>?` | `null` | Async rename veto; configure instead of `BeforeRename` |
| `BeforeEditNameAsync` | `Func<TreeNode, ValueTask<bool>>?` | `null` | Async edit veto; configure instead of `BeforeEditName` |
| `Indent` | `int` | `20` | Indent per level in px |
| `OnlyHighlightText` | `bool` | `true` | Highlight only the label on selection |
| `LoadingText` | `string` | `"Loading items..."` | Virtualized loading placeholder text |
| `EditLabelFormat` | `string` | `"Edit {0}"` | Default edit input accessible name; `{0}` is the node text |
| `RenameRejectedText` | `string` | `"Rename rejected"` | Fallback validation message when a rename is vetoed |
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
- In-place structure changes are detected by default. For large trees, set `AutoDetectStructureChanges="false"` and call `RefreshAsync()` after mutations. Direct in-place `TreeNode.IsExpanded` changes also require `RefreshAsync()`; prefer `ExpandedIds` or `SetExpandedAsync`.
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
| `View` | Read-only Tree query view; `Flatten`/`FlattenVisible` return true read-only views over internal caches |
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
- **View state**: `IsSelected` and `IsFocused` are derived queries; `IsEditing` has an internal setter. `IsExpanded` is read-only to applications: set initial expansion with the `isExpanded` constructor argument, then use `ExpandedIds` or `SetExpandedAsync`. `IsDisabled` remains application data.
- **Legacy node facade methods**: read-only query helpers remain available; state-changing helpers are internal to the validated Tree command boundary.

### Tree query view (via `Tree.View`)

- **Query**: selected ids, focused id/node, revision, stable-id lookup, and flattened visible nodes.
- **Commands**: selection, focus, expansion, editing, and refresh go through the Tree command facade rather than this query view.

### Enums and Contexts

- `TreeSelectionMode`: `Single`, `Multiple`.
- `TreeDropRelation`: `Before`, `Inside`, `After`.
- `TreeEditContext` (in `EditTemplate`): `Node`, `Value`, `SetValue(value)`, `CommitAsync()`, `CancelAsync()`, `ValidationMessage`.
- `TreeIconContext` (in `IconTemplate`): `Node`, `IsExpanded`, `HasChildren`, `IsDisabled`.

Each edit context belongs to exactly one edit session. Once another edit starts, its node is replaced or removed, or the component is disposed, calls from the older context cannot commit or cancel the current edit.

### API migration contract

| Legacy surface | Frozen direction |
|---|---|
| `Tree.Controller`, writable `Controller.Selection`, or mutable selected/expanded collections | Queries use `Tree.View`; `TreeController` and `TreeState` are internal implementation details |
| State-changing methods on `TreeController` | Internal; use validated asynchronous Tree commands with stable node ids |
| State-changing `TreeNode` facade methods | Internal; use the corresponding validated Tree command |
| Writable `TreeNode.IsSelected`, `IsFocused`, or `IsEditing` | Removed as authoritative state; `IsEditing` has an internal setter |
| `TreeNode.IsExpanded` as a second source of truth | Read-only to applications; initial state via `isExpanded` constructor argument, then `ExpandedIds`/`SetExpandedAsync` |
| Synchronous veto delegates | Configure `Before*` or `Before*Async` on Tree, not both; controller-level guards were removed in 0.7.0 |
| Tuple event payloads | Named EventArgs types |

Application decisions may complete asynchronously. Tree does not commit while waiting, revalidates the node and operation before applying the result, leaves state unchanged when the result is stale, and does not silently swallow decision errors. A drag operation is consumed at most once after acceptance, rejection, cancellation, disablement, or disposal.

`TreeController`, `TreeState`, `DragProtocol`, `TreeKeyboardNavigation`, and `TreeDropValidation` are internal as of 0.7.0. Applications interact through `Tree`, `TreeView`, `TreeNode`, events, and context types.

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
            return edit.CancelAsync();
        }

        return Task.CompletedTask;
    }
}
```

## Accessibility (WAI-ARIA)

Tree follows the [WAI-ARIA Tree View Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/): a single tab stop on the container (`role="tree"` with `tabindex="0"`), non-focusable `role="treeitem"` nodes, and focus tracked through `aria-activedescendant`. Clicking a node moves DOM focus to the container. `aria-level`, `aria-selected`, `aria-expanded`, and `aria-disabled` are emitted. Disabled nodes are navigable but not selectable, expandable, or activatable.

## ResourceTable

ResourceTable renders a column-driven header with an application-provided body. It supports pointer and keyboard column resizing, toolbar actions, undo/redo buttons, and empty column collections.

### Quick start

```razor
@using Astrea.Components.ResourceTable
```

```html
<link href="_content/Astrea.Components/astrea-resource-table.css" rel="stylesheet" />
```

```razor
<div style="height: 360px;">
    <ResourceTable Columns="_columns"
                   TableBody="RenderRows"
                   ResizableColumns="true"
                   OnColumnResized="HandleColumnResized" />
</div>

@code {
    private readonly List<TableColumn> _columns = new()
    {
        new("ID") { Key = "id", Width = "60px", MinWidth = 40 },
        new("Name") { Key = "name", Width = "160px", MinWidth = 60 },
        new("Type") { Key = "type" }
    };

    private RenderFragment RenderRows => builder =>
    {
        builder.OpenElement(0, "tr");
        builder.OpenElement(1, "td");
        builder.AddContent(2, "sample");
        builder.CloseElement();
        builder.CloseElement();
    };

    private void HandleColumnResized(ColumnResizedEventArgs args)
    {
        // args.ColumnIndex / args.ColumnKey / args.NewWidth
    }
}
```

### ResourceTable Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Columns` | `IEnumerable<TableColumn>` | required | Column definitions; an empty collection renders no header cell |
| `TableBody` | `RenderFragment?` | required | Body markup rendered inside `tbody` |
| `ToolBar` | `RenderFragment?` | `null` | Extra toolbar content |
| `ShowButton` | `bool` | `true` | Render add/remove/move buttons |
| `ShowRedoButton` | `bool` | `false` | Render undo/redo buttons |
| `ContainerCss` | `string?` | `null` | Additional CSS class for the root container |
| `ResizableColumns` | `bool` | `true` | Render resize handles and enable the resize module |
| `CanUndo` / `CanRedo` | `bool` | `false` | Disable state for undo/redo buttons |
| `OnAddClicked` / `OnRemoveClicked` / `OnMoveUpClicked` / `OnMoveDownClicked` | `EventCallback` | — | Toolbar action notifications |
| `OnUndoClicked` / `OnRedoClicked` | `EventCallback` | — | Undo/redo notifications |
| `OnKeyDown` | `EventCallback<KeyboardEventArgs>` | — | Container keyboard forwarding |
| `OnColumnResized` | `EventCallback<ColumnResizedEventArgs>` | — | Fired after a pointer or keyboard resize |
| `AddButtonLabel` / `RemoveButtonLabel` / `MoveUpButtonLabel` / `MoveDownButtonLabel` / `UndoButtonLabel` / `RedoButtonLabel` | `string` | `Add` / `Remove` / `Move Up` / `Move Down` / `Undo` / `Redo` | Toolbar button accessible names/tooltips |
| `ResizeHandleLabelFormat` | `string` | `"Resize column {0}"` | Resize handle accessible name; `{0}` is the column title |
| `IconAdd` / `IconRemove` / `IconMoveUp` / `IconMoveDown` / `IconUndo` / `IconRedo` | `RenderFragment?` | `null` | Override the built-in inline SVG icons |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | — | Extra HTML attributes; app classes are appended to the component class |

`TableColumn` declares `Title`, `Width`, `Key`, `MinWidth`, and `Resizable` (the last column never gets a resize handle).

### Keyboard and accessibility

- Each resize handle is a focusable `role="separator"` with `aria-orientation="vertical"` and an accessible name.
- ArrowLeft/ArrowRight adjust the focused boundary by 10 px, honoring `MinWidth` and preserving the two-column total width.
- Resize keydown events do not stop propagation; the application still receives them through `OnKeyDown`.
- `ResizableColumns="false"` removes all handles and disposes the resize listeners. The module reinitializes when column definitions or min widths change.

### Theme variables

Components expose documented `--ast-*` variables. Override them on the component root or any ancestor.

Tree:

| Variable | Default | Purpose |
|---|---|---|
| `--ast-tree-font-size` | `14px` | Node text size |
| `--ast-tree-line-height` | `1.6` | Node line height |
| `--ast-tree-color-text` | `#1f2328` | Node text color |
| `--ast-tree-node-height` | `32px` | Minimum node height |
| `--ast-tree-color-hover` | `rgba(0,0,0,0.05)` | Hover background |
| `--ast-tree-color-focus` | `rgba(24,144,255,0.15)` | Focus background |
| `--ast-tree-color-active` | `rgba(24,144,255,0.25)` | Selected background |
| `--ast-tree-color-border` | `#d0d7de` | Edit input border |
| `--ast-tree-icon-size` | `16px` | Icon box size |
| `--ast-tree-control-indent` | `20px` | Expand-control slot width |
| `--ast-tree-font-weight-bold` | `600` | Bold node text |

ResourceTable:

| Variable | Default | Purpose |
|---|---|---|
| `--ast-rt-font-family` | `sans-serif` | Table font family |
| `--ast-rt-font-size` | `14px` | Table font size |
| `--ast-rt-color-text` | `#333` | Text color |
| `--ast-rt-color-bg` | `#fff` | Surface/button background |
| `--ast-rt-color-header-bg` | `#f5f5f5` | Header background |
| `--ast-rt-color-border` | `#ddd` | Cell/button border |
| `--ast-rt-color-border-strong` | `#989ca4` | Scroll container border |
| `--ast-rt-color-accent` | `#268cda` | Hover/focus accent |
| `--ast-rt-color-hover-bg` | `#e0eef9` | Button hover background |
| `--ast-rt-color-selected-bg` | `#c3c3ff` | App-selected row background |
| `--ast-rt-color-resize` | `#d0d7de` | Resize indicator |
| `--ast-rt-color-resizing-bg` | `#f8f9fa` | Header background while resizing |
| `--ast-rt-radius` | `4px` | Button radius |
| `--ast-rt-btn-size` | `24px` | Toolbar button size |
| `--ast-rt-icon-size` | `14px` | Toolbar icon size |
| `--ast-rt-resize-width` | `24px` | Resize handle hit width |

## Versioning

Astrea follows [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html). While the version is below 1.0.0, a minor release may contain documented breaking behavior changes; see [CHANGELOG.md](CHANGELOG.md). A 1.0.0 release freezes the public behavior contract for the components listed as Active in the SRS. The package builds with the .NET 10 LTS SDK pinned in `global.json` and publishes `net6.0` assets.

The published assets target `net6.0` to honor the SRS compatibility contract. .NET 6 is out of support; consumers are responsible for runtime security updates or for upgrading the host runtime.

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md). Requirements are traceable to [docs/SRS.md](docs/SRS.md) (REQ-F-xxx / REQ-NF-xxx).

Verification commands (all must pass after any change):

```sh
dotnet build Astrea.sln
dotnet test Astrea.sln --no-build
dotnet format Astrea.sln --verify-no-changes --no-restore
python3 scripts/verify_srs_consistency.py
node --test tests/Astrea.JsTests/*.test.mjs
python3 scripts/verify_package.py
```

Install the optional accessibility tooling and run the browser matrix:

```sh
npm ci --prefix tests/Astrea.A11y
ASTREA_BROWSER_ENGINE=firefox dotnet test Astrea.sln --no-build --filter "TestCategory=Browser"
```

Use `python3 scripts/verify_srs_consistency.py --release` for a release gate; it fails while any Active requirement has a tracked evidence gap in [docs/requirements-test-matrix.md](docs/requirements-test-matrix.md). Actual browser, axe, and Blazor Server results are recorded in [docs/release-evidence.md](docs/release-evidence.md).

Manual release validation still required for the last two gaps:

- [Accessibility validation checklist](docs/accessibility-validation-checklist.md) — WCAG 2.2 AA, keyboard, and screen-reader sessions.
- [Browser version verification plan](docs/browser-version-verification-plan.md) — second recent major version per browser.
