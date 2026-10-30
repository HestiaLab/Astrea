# Astrea 可选人工可访问性验证清单（WCAG 2.2 AA）

> **状态：可选人工 QA，不是发布门禁。** REQ-NF-004 已改为 axe-core + 键盘自动化检查；本清单不阻断 0.9.0 发布，也不构成完整 WCAG 2.2 AA 符合性声明。执行人应为未参与实现的人员；执行完成后把结果记录到本文件与 [release-evidence.md](release-evidence.md)，作为额外证据保留。

## 1. 范围与环境

- 组件：Tree、ResourceTable（当前版本 Active 组件）。
- 应用：`src/Astrea.Demo`（standalone Blazor WebAssembly）与 `tests/Astrea.ServerSmoke`（Blazor Web App，Interactive Server）。
- 必须至少覆盖以下组合中的三类，其中必须包含一个真实 Safari + VoiceOver 组合：
  - Windows + NVDA + Firefox 或 Chrome；
  - Windows + JAWS + Edge 或 Chrome；
  - macOS + VoiceOver + Safari。
- 记录：执行日期、执行人、操作系统、浏览器精确版本、辅助技术版本、页面缩放。

自动证据（已完成，供对照，不替代人工）：

- axe-core 4.14.0，WCAG 2.x 标签，Tree/ResourceTable 容器 0 serious/critical；
- 三引擎浏览器键盘/焦点/拖放/多选/编辑用例；
- 详见 [release-evidence.md](release-evidence.md)。

## 2. 键盘检查（每个浏览器组合执行一次）

| 编号 | 场景 | 预期 | 结果/备注 |
|---|---|---|---|
| K1 | 从页面 Tab 进入 Tree，再 Tab 离开 | Tree 只有一个 Tab 入口；Tab/Shift+Tab 可离开 | |
| K2 | ArrowDown / ArrowUp | active descendant 在可见节点间移动；选中不变 | |
| K3 | Home / End | 移动到首/末可见节点 | |
| K4 | ArrowRight / ArrowLeft | 折叠节点展开；展开节点进入首子节点；子节点回到父节点；展开节点折叠 | |
| K5 | Space | 多选模式切换当前节点；单选模式选择当前节点 | |
| K6 | Shift+ArrowUp/Down、Shift+Home/End | 从锚点扩展范围；禁用中间节点被跳过；禁用端点拒绝范围但焦点移动 | |
| K7 | Enter | 激活当前节点；编辑提交的 Enter 不得额外触发一次激活 | |
| K8 | F2 → Enter | 默认编辑框获得焦点；提交后焦点回到 Tree 容器与被编辑节点 | |
| K9 | F2 → Escape | 取消编辑；不发送提交通知；焦点回到 Tree 容器 | |
| K10 | F2 → 点击其他控件（blur） | 提交一次；焦点按组件契约回到 Tree 容器 | |
| K11 | 禁用节点 | 可被方向键访问；不可选择、展开、激活；辅助技术读出禁用状态 | |
| K12 | 右键/上下文菜单键 | 触发一次上下文通知；不改变选中或焦点（若浏览器不派发键盘 contextmenu，记录为环境限制） | |
| K13 | ResourceTable 容器 Tab | 容器可聚焦；应用绑定的 OnKeyDown 收到按键 | |
| K14 | ResourceTable resize handle Tab + ArrowLeft/Right | handle 可聚焦；每次调整 10px；最小宽度约束；相邻列补偿；`aria-valuenow` 更新 | |
| K15 | ResourceTable 工具栏 | 每个按钮可聚焦、可触发、禁用态正确；Tab 可离开 | |
| K16 | 页面缩放 200%（Tree） | 内容和焦点可用；无二维滚动陷阱 | |
| K17 | 页面缩放 200%（ResourceTable） | 表格二维滚动由应用容器负责；组件自身不产生焦点陷阱 | |

## 3. 屏幕阅读器检查

### Tree

| 编号 | 检查 | 预期 | 结果/备注 |
|---|---|---|---|
| T1 | 角色与名称 | 容器为 `tree` 并有可访问名称；节点为 `treeitem` | |
| T2 | 层级与状态 | 读出 `aria-level`、`aria-expanded`、`aria-selected`、`aria-disabled` | |
| T3 | 焦点跟随 | 方向键后读出当前 active descendant，而非旧节点 | |
| T4 | 编辑框 | 默认编辑输入读作 “Edit <节点名>” 或应用配置的格式 | |
| T5 | 编辑结束 | 提交/取消后读出 Tree 与原节点，焦点不落到 body | |
| T6 | 多选 | Space 切换、Shift 范围选择后读出正确选中集合 | |
| T7 | 拖放 | 拖放开始/结束不产生错误播报；应用收到一次 OnDrop（Tree 拖放不要求键盘等价操作） | |

### ResourceTable

| 编号 | 检查 | 预期 | 结果/备注 |
|---|---|---|---|
| R1 | 表格语义 | 表格有应用提供的名称或标题；表头与数据单元格关联 | |
| R2 | 工具栏按钮 | 每个按钮有可访问名称；撤销/重做禁用态可读 | |
| R3 | resize handle | 读作 separator（垂直）；读出列名、最小值/当前值；方向键调整后值更新 | |
| R4 | 键盘转发 | 应用仍收到容器上的按键（例如 ArrowRight 日志） | |
| R5 | 空列 | 不产生误导性的空表头 | |

## 4. WCAG 2.2 AA 对照

| 标准 | 检查 | 结果/备注 |
|---|---|---|
| 1.3.1 信息与关系 | Tree 层级、表格表头关系、separator 语义 | |
| 1.4.3 对比度（文本） | 默认主题下的节点文本、表格文本、按钮图标 | |
| 1.4.11 非文本对比度 | 焦点指示、resize 指示、边框与状态 | |
| 2.1.1 键盘 | 第 2 节全部通过 | |
| 2.4.3 焦点顺序 | Tab 顺序与视觉顺序一致；无隐藏焦点 | |
| 2.4.7 焦点可见 | Tree 容器、节点 active 状态、resize handle、按钮均有可见指示 | |
| 2.5.8 目标尺寸 | 工具栏按钮 24×24；resize handle 24×28；节点行高 32 | |
| 3.2.1 焦点时 | 仅移动焦点不改变选择、展开或页面上下文 | |
| 4.1.2 名称/角色/值 | 第 3 节全部通过 | |
| 4.1.3 状态消息 | 选中/展开/禁用/编辑状态通过可访问属性表达 | |

## 5. 记录表

| 日期 | 执行人 | OS | 浏览器/版本 | 辅助技术/版本 | 执行范围 | 结果 | 备注 |
|---|---|---|---|---|---|---|---|
| | | | | | | | |

## 6. 可选人工验证记录步骤

1. 完成第 2～4 节并记录原始结果、截图或屏幕阅读器日志。
2. 在 [release-evidence.md](release-evidence.md) 记录实际浏览器/辅助技术版本与结论，标注为可选人工 QA。
3. 可选人工 QA 不改变 REQ-NF-004 的自动化发布门禁；如需调整门禁，必须另行变更需求。
4. 若发现组件缺陷，回到正常缺陷修复流程；不得用文档豁免 WCAG 阻断项。
