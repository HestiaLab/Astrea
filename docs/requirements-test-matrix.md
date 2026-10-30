# Astrea 需求-测试矩阵

> 需求状态与优先级以 [docs/SRS.md](SRS.md) 为准；本文件由 `scripts/verify_srs_consistency.py` 校验。
> 每条 Active P0/P1 需求必须具有可定位证据。以 `Gap:` 开头的条目是**发布阻断项**，必须在发布前关闭或经正式范围变更。
> 常规 CI 校验追踪完整性（Gap 允许存在但会告警）；发布验证使用 `python3 scripts/verify_srs_consistency.py --release`，任一 Gap 都会使发布门禁失败。
> 当前 Active：27 条功能需求 + 11 条非功能需求；Deferred：70 条功能需求 + 1 条非功能需求（REQ-NF-003）。

| 需求 | 优先级 | 状态 | 证据 / Gap |
|---|---|:---:|---|
| REQ-F-001 | 🔴 | Active | `TreeComponentLifecycleTests.NullDataSource_RendersStableAccessibleTreeContainer`; `ResourceTableRenderTests.AdditionalAttributesClass_MergesWithComponentClass` |
| REQ-F-002 | 🔴 | Active | `TreeControllerDisabledNodeTests.*`; `TreeControllerCommandTests.Select_DisabledNode_ReturnsRejected`; `TreeBrowserTests.EqualLogicalIdsAndDynamicDrag_RemainIsolated` |
| REQ-F-003 | 🔴 | Active | `TreeComponentLifecycleTests.SameDataSource_UnrelatedParameterUpdateDoesNotIncrementRevision`; `ControlledSelection_UserInteractionOnlyProposesNextState`; `ControlledExpansion_UserInteractionOnlyProposesNextState` |
| REQ-F-004 | 🟡 | Active | `astrea-tree.css` and `astrea-resource-table.css` document `--ast-*` variables; `TreeBrowserTests.ResourceTable_ThemeVariable_OverridesBorderColor`; README theme tables |
| REQ-F-005 | 🟡 | Active | Implementation: component-prefixed CSS scopes in `astrea-tree.css` / `astrea-resource-table.css`; manual host smoke; no automated host-isolation test |
| REQ-F-006 | 🔴 | Active | `TreeBrowserTests.ClickingTreeItem_KeepsDomFocusOnContainer_AndArrowKeysSyncActiveDescendant`; `TreeBrowserTests.EditingEnds_CommitCancelAndBlur_RestoreFocusToTree`; `ResourceTableRenderTests.ResizeHandle_ExposesKeyboardSeparatorSemantics`; `TreeBrowserTests.ResourceTable_ResizeToggle_AddsAndRemovesKeyboardHandles` |
| REQ-F-007 | 🔴 | Active | `TreeBrowserTests.KeyboardFocusAndAria_UseOneTabStopAndExistingActiveDescendant`; `ClickingTreeItem_KeepsDomFocusOnContainer_AndArrowKeysSyncActiveDescendant`; `EditingEnds_CommitCancelAndBlur_RestoreFocusToTree`; `ResourceTable_KeyboardResize_AdjustsWidthsAndForwardsKey` |
| REQ-F-008 | 🔴 | Active | `TreeComponentLifecycleTests.ControlledSelection_UserInteractionOnlyProposesNextState`; `ControlledExpansion_UserInteractionOnlyProposesNextState`; `TreeControllerCommandTests.SingleMode_AllPublicSelectionCommandsPreserveAtMostOneSelection` |
| REQ-F-009 | 🟡 | Active | Tree `LoadingText` / `EditLabelFormat` and ResourceTable button/resize label parameters; `TreeComponentLifecycleTests.EditLabelFormat_IsConfigurable`; `ResourceTableRenderTests.Labels_AreConfigurable` |
| REQ-F-010 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-011 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-012 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-013 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-014 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-015 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-016 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-017 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-018 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-019 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-020 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-021 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-022 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-023 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-024 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-025 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-026 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-027 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-028 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-029 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-030 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-031 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-032 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-033 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-034 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-035 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-036 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-037 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-038 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-039 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-040 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-041 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-042 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-043 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-044 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-045 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-046 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-047 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-048 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-049 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-050 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-051 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-052 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-053 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-054 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-055 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-056 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-057 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-058 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-059 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-060 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-061 | 🔴 | Active | `TreeNodeInvariantTests.*`; `TreeComponentLifecycleTests.Render_NodeIndentReflectsHierarchy`; `TreeControllerAccessibilityTests` disabled-node tests; `TreeBrowserTests.EqualLogicalIdsAndDynamicDrag_RemainIsolated` |
| REQ-F-062 | 🔴 | Active | `TreeControllerAccessibilityTests.Expand_DisabledNode_ReturnsFalse`; `Collapse_DisabledNode_ReturnsFalse`; `TreeComponentLifecycleTests.ControlledExpansion_UserInteractionOnlyProposesNextState` |
| REQ-F-063 | 🔴 | Active | `TreeControllerTests` selection/focus tests; `TreeKeyboardNavigationTests.Space_SelectsFocused`; `TreeComponentLifecycleTests.Enter_InvokesFocusedNodeWithoutChangingSelection` |
| REQ-F-064 | 🟡 | Active | `TreeBrowserTests.NodesWithoutContentIcons_DoNotReserveIconContainers`; `Tree.Render.RenderNodeIcon` implementation |
| REQ-F-065 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-066 | 🔴 | Active | `TreeKeyboardNavigationTests.*`; `TreeBrowserTests.KeyboardFocusAndAria_UseOneTabStopAndExistingActiveDescendant`; `TreeBrowserTests.KeyboardMultiSelect_SpaceTogglesAndShiftArrowExtendsRange` |
| REQ-F-067 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-068 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-069 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-070 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-071 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-072 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-073 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-074 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-075 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-076 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-077 | 🔴 | Active | `TreeControllerFocusRestoreTests.*`; `TreeComponentLifecycleTests.RemovingEditedNode_EndsEditingSession`; `TreeControllerCommandTests.Refresh_IncrementsRevision` |
| REQ-F-078 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-079 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-F-080 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-081 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-082 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-083 | 🔴 | Deferred | Deferred（附录 B） |
| REQ-F-084 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-085 | 🟢 | Deferred | Deferred（附录 B） |
| REQ-F-086 | 🟡 | Active | `TreeKeyboardNavigationTests.Space_MultipleMode_*`; `ArrowDown_ShiftMultiple_ExtendsRangeFromAnchor`; `ShiftArrow_MultipleMode_DisabledTargetRejectsRangeButFocusMoves`; `TreeBrowserTests.KeyboardMultiSelect_SpaceTogglesAndShiftArrowExtendsRange` |
| REQ-F-087 | 🟡 | Active | `DragProtocolTests.*`; `TreeDragSessionTests.*`; `TreeBrowserTests.DragDrop_BeforeInsideAfter_NotifiesOnceWithRelation`; `DragDrop_VetoedOrCyclic_DoesNotNotify` |
| REQ-F-088 | 🟡 | Active | `TreeControllerEditVetoTests.*`; `TreeComponentLifecycleTests.StaleEditContext_DoesNotCommitCurrentNode`; `ClearingDataSource_EndsEditingSession`; `RemovingEditedNode_EndsEditingSession`; `DisposeAsync_EndsEditingSession`; `TreeBrowserTests.EditingEnds_CommitCancelAndBlur_RestoreFocusToTree` |
| REQ-F-089 | 🟡 | Active | `TreeBrowserTests.ContextMenu_NotifiesWithoutChangingSelectionOrFocus`; `Tree.HandleRightClick` implementation |
| REQ-F-090 | 🔴 | Active | `ResourceTableColumnsParameterTests.*`; `ResourceTableRenderTests.ResizableColumnsTrue_RendersHandlesOnlyForResizableNonLastColumns`; `ResourceTable_PointerResize_ConservesWidthAndNotifiesOnce` |
| REQ-F-091 | 🟡 | Active | `astrea-resource-table.test.mjs` pointer/min-width/total-width tests; `TreeBrowserTests.ResourceTable_PointerResize_ConservesWidthAndNotifiesOnce`; `ResourceTableRenderTests.ResizableColumns*` |
| REQ-F-092 | 🟡 | Active | `ResourceTableColumnResizedCallbackTests.*`; `astrea-resource-table.test.mjs` notification tests; `TreeBrowserTests.ResourceTable_PointerResize_*`; `ResourceTable_KeyboardResize_*` |
| REQ-F-093 | 🟡 | Active | `ResourceTableRenderTests.Toolbar_RendersActionButtonsAndDisablesUndoRedo`; `Toolbar_HidesButtonsWhenShowButtonIsFalseAndOmitsUndoRedoByDefault`; `TreeBrowserTests.ResourceTable_ToolbarButtons_Notify` |
| REQ-F-094 | 🟡 | Active | `ResourceTableRenderTests.Toolbar_RendersActionButtonsAndDisablesUndoRedo`; `Toolbar_HidesButtonsWhenShowButtonIsFalseAndOmitsUndoRedoByDefault`; `TreeBrowserTests.ResourceTable_ToolbarButtons_Notify` |
| REQ-F-095 | 🟡 | Active | `TreeBrowserTests.ResourceTable_KeyboardResize_AdjustsWidthsAndForwardsKey`; `ResourceTable.HandleKeyDown` implementation |
| REQ-F-096 | 🔴 | Active | `ResourceTableRenderTests.EmptyColumns_RenderNoHeaderCellAndKeepBody`; `ResourceTableColumnsParameterTests.ColumnsSetter_AcceptsEmptyCollection` |
| REQ-F-097 | 🟢 | Active | `ResourceTableRenderTests.ToolbarIcons_UseDefaultsAndAllowOverrides` |
| REQ-NF-001 | 🔴 | Active | `src/Astrea.Components/Astrea.Components.csproj` TargetFramework net10.0; `scripts/verify_package.py` asserts `lib/net10.0` and no `lib/net6.0`; build/test suite runs on net10.0 |
| REQ-NF-002 | 🟡 | Active | Gap: latest Edge 156、真实 Chrome 156、Chromium 156、Firefox 157、WebKit 27.2 matrix passes; Chrome/Edge/Firefox N-1 and Safari execution pending — plan in `docs/browser-version-verification-plan.md` |
| REQ-NF-003 | 🟡 | Deferred | Deferred（附录 B） |
| REQ-NF-004 | 🔴 | Active | Gap: manual screen-reader/WCAG session pending — checklist in `docs/accessibility-validation-checklist.md`; automated axe 0 serious/critical plus keyboard browser tests recorded in `docs/release-evidence.md` |
| REQ-NF-005 | 🟡 | Active | `src/Astrea.Components/Astrea.Components.csproj` declares `PackageReference` dependencies on `Microsoft.AspNetCore.Components` 10.0.14 and `Microsoft.AspNetCore.Components.Web` 10.0.14; no third-party runtime packages |
| REQ-NF-006 | 🔴 | Active | `scripts/verify_package.py` packs Astrea.Components 0.9.0 and verifies clean Blazor Web App and standalone WebAssembly consumption; results in `docs/release-evidence.md` |
| REQ-NF-007 | 🟡 | Active | `README.md` current-release scope and ResourceTable docs; `docs/Roadmap.md`; SRS v0.9.0 |
| REQ-NF-008 | 🔴 | Active | `docs/requirements-test-matrix.md`; `scripts/verify_srs_consistency.py`; CI gates |
| REQ-NF-009 | 🔴 | Active | `ServerSmokeBrowserTests.*`; minimal Blazor Web App (Interactive Server) host in `tests/Astrea.ServerSmoke`; results in `docs/release-evidence.md` |
| REQ-NF-010 | 🔴 | Active | `TreeComponentLifecycleTests.NodeText_IsRenderedAsTextNotMarkup`; components perform no external network calls |
| REQ-NF-011 | 🔴 | Active | `TreeComponentLifecycleTests.ClearingDataSource_EndsEditingSession`; `DisposeAsync_EndsEditingSession`; `TreeInteropLifecycleTests.*`; `ResourceTableInteropTests.ResizeInterop_FollowsResizableColumnsAndColumnChanges` |
| REQ-NF-012 | 🟡 | Active | `CHANGELOG.md` and README versioning policy; package version 0.9.0 aligned with SRS; `global.json` pinned to .NET SDK 10.0.401 |
