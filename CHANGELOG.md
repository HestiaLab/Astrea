# Changelog

All notable changes to Astrea are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

While the version is below 1.0.0, a minor release may contain documented breaking behavior changes. A 1.0.0 release freezes the public behavior contract for the components listed as Active in the SRS.

## [0.8.0] - 2026-10-26

### Breaking (0.x)

- Package identity: the NuGet package ID, assembly name, and static asset base path are now `Astrea.Components` (formerly `Astrea`). Update `PackageReference` and `_content/Astrea/` links; public namespaces remain `Astrea.Components.*`. Component behavior is unchanged.

## [0.7.0] - 2026-10-25

### Breaking (0.x)

- `TreeController`, `TreeState`, `DragProtocol`, `TreeKeyboardNavigation`, and `TreeDropValidation` are internal. Use `Tree`, `Tree.View`, Tree commands, events, and context types.
- Removed `Tree.BeginEdit(TreeNode)`; use `BeginEditAsync(node.Id)`.
- Removed controller-level synchronous guard properties; configure `Before*` or `Before*Async` on Tree only.
- `TreeNode.IsExpanded` is read-only to applications. Set initial expansion with the `isExpanded` constructor argument, then use `ExpandedIds` or `SetExpandedAsync`.

### Changed

- Converged the release scope to **Tree** and **ResourceTable**. Button, Alert, Divider, Input, Label, Modal, Progress, Select, and Table requirements are Deferred; see `docs/Roadmap.md`.
- Added the requirements-to-test matrix and the release evidence record.
- Pinned the build/CI SDK to .NET 10 LTS while keeping the published target framework at `net6.0`.
- Performance: cached `TreeNode.Children` and `TreeState.SelectedIds` read-only views, replaced O(k²) drag-source normalization with an ancestor set, pruned stale node DOM tokens, and added `AutoDetectStructureChanges` for large trees.
- Controlled expansion skips the full traversal and visible-cache invalidation when `ExpandedIds` is unchanged.
- Virtualization focus polling now stops after component disposal and handles disconnected JS calls.

### Fixed

- Tree: real browser drag/drop now uses one explicit camelCase JSON contract; SVG expand icons are valid drop targets.
- Tree: DOM focus stays on the `role="tree"` container; edit commit/cancel/blur restore focus.
- Tree: hierarchy indentation is correct for root, child, and grandchild levels.
- Tree: disabled nodes cannot be dragged, dropped on, edited, or renamed; `IsExpandDisable` is enforced on the component path.
- Tree: edit sessions are cleared when the data source is cleared, the edited node is removed, or the component is disposed; JS initialization rechecks disposal after each await.
- Tree: keyboard multi-select supports Space toggling and Shift+Arrow/Home/End ranges.
- ResourceTable: `ResizableColumns` controls both render and JS lifecycle; runtime changes and column changes reinitialize the resize module.
- ResourceTable: empty columns render no empty header cell; `AdditionalAttributes.class` is merged; resize handles are keyboard-operable separators with value semantics.
- ResourceTable: column resize hit area, `scope="col"`, and decorative icon semantics follow the accessibility audit.
- ResourceTable: out-of-range, non-finite, or negative resize widths are rejected; widths below `MinWidth` are clamped.
- `ShowLink` now draws parent-child connector lines in the indentation area.

### Added

- Cross-browser browser tests (Chromium, Edge, Firefox, WebKit), Blazor Server smoke tests, axe-core accessibility audit, and `docs/release-evidence.md`.
- `LICENSE` (MIT), package metadata, README/CHANGELOG packaging, and SDK pinning.
- `AutoDetectStructureChanges` and configurable `RenameRejectedText`.
- Manual accessibility and second-browser-version validation plans.

### Removed

- Unused `Moq` test dependency.
- Duplicate Tree stylesheet loading in the demo.

### Known release blockers

- REQ-NF-002: the second recent major browser version per engine is not separately verified.
- REQ-NF-004: a human screen-reader/WCAG validation session has not been performed.

`python3 scripts/verify_srs_consistency.py --release` fails while any tracked evidence gap remains.
