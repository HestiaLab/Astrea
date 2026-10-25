# Astrea 浏览器第二主版本验证计划（REQ-NF-002）

> 目的：关闭 REQ-NF-002 的“第二个最近桌面主版本”Gap。当前自动化矩阵只验证每个引擎的最新版；本计划给出可执行的第二版本验证方式与记录模板。

## 1. 需求与当前证据

REQ-NF-002 要求支持发布时 Chrome、Edge、Firefox、Safari 的最近两个桌面主版本，并在发布记录中列出实际验证的浏览器及版本。

已完成的自动化证据（最新版）：

| 引擎 | 已测版本 | 用例数 | 结果 |
|---|---:|---:|---|
| Edge（msedge，Windows 本地） | 156.0.4314.42 | 20 | 通过 |
| Chromium（Playwright bundled） | 149.0.7827.55 | 20 | 通过 |
| Firefox（Playwright） | 151.0 | 20 | 通过 |
| WebKit（Playwright） | 26.5 | 20 | 通过 |

注意：Playwright Chromium 不是 Google Chrome，Playwright WebKit 不是 Apple Safari。发布声明涉及 Chrome/Safari 时，必须有真实 Chrome 与 Safari 的执行记录。

## 2. 发布时版本基线

在每次发布前确定并记录：

| 浏览器 | 最新主版本（N） | 第二主版本（N-1） | 执行方式 |
|---|---|---|---|
| Chrome | | | 自动（Playwright channel 或手动） |
| Edge | | | 自动（msedge channel）或手动 |
| Firefox | | | 自动（Playwright）或手动 |
| Safari | | | 手动（macOS + VoiceOver） |

“最近两个桌面主版本”指发布时稳定版通道的当前主版本与前一主版本；beta/dev 不计入，除非产品明确声明支持。

## 3. 推荐执行方案

### 3.1 最新版：自动化

```sh
npm ci --prefix tests/Astrea.A11y
ASTREA_BROWSER_ENGINE=chromium dotnet test Astrea.sln --filter "TestCategory=Browser"
ASTREA_BROWSER_ENGINE=firefox dotnet test Astrea.sln --filter "TestCategory=Browser"
ASTREA_BROWSER_ENGINE=webkit dotnet test Astrea.sln --filter "TestCategory=Browser"
```

CI 在 Ubuntu 上运行同一矩阵；Windows 本地使用 `ASTREA_BROWSER_CHANNEL=msedge` 覆盖 Edge。

### 3.2 第二主版本：真实浏览器人工/半自动

在装有 N-1 浏览器的机器上执行：

1. 启动 Demo 或 Server 冒烟宿主（`dotnet run --project src/Astrea.Demo` 或 `tests/Astrea.ServerSmoke`）。
2. 按 [accessibility-validation-checklist.md](accessibility-validation-checklist.md) 的 K1～K15 与核心组件场景执行。
3. 同时执行以下自动化/半自动检查（在 N-1 浏览器中手动触发并观察控制台）：
   - Tree：点击节点后 `document.activeElement` 必须是 `role="tree"` 容器；方向键后 `aria-activedescendant` 更新；Tab 可离开。
   - Tree：真实拖放 before/inside/after 各触发一次 OnDrop；否决/循环拒绝不触发。
   - Tree：编辑 Enter/Escape/blur 后焦点回到 Tree。
   - ResourceTable：指针拖拽与键盘 ArrowLeft/Right 调整列宽；最小宽度与总宽守恒。
   - 控制台无错误。
4. 记录浏览器精确版本、OS、执行人、日期、结果与截图/日志。

### 3.3 可选：第二版本自动化

Playwright .NET 的浏览器修订号与包版本绑定，无法用同一个 `Microsoft.Playwright` 版本同时启动两个内置浏览器版本。若必须全自动覆盖 N-1，可选：

- 为 N-1 建立独立的临时测试项目，固定到较旧的 `Microsoft.Playwright` 版本，复用同一批浏览器测试；
- 或对 Chrome/Edge 使用 channel（stable/beta）组合，Firefox/WebKit 仍需人工或独立 Playwright 版本。

这会增加测试项目、依赖与 CI 时间，需仓库所有者批准。推荐先用 3.2 的人工/半自动方式满足发布记录要求，除非产品要求全自动 N-1 门禁。

## 4. 核心场景清单（N-1）

| 编号 | 组件 | 场景 | 结果 |
|---|---|---|---|
| B1 | Tree | 点击节点后容器持有 DOM 焦点与 active descendant | |
| B2 | Tree | Arrow/Home/End 导航与 Tab 离开 | |
| B3 | Tree | Space 切换、Shift+Arrow 范围多选 | |
| B4 | Tree | F2 编辑 Enter/Escape/blur，焦点恢复 | |
| B5 | Tree | 拖放 before/inside/after、否决、循环拒绝 | |
| B6 | Tree | 禁用节点不可选择/展开/拖拽 | |
| B7 | Tree | 虚拟化 5 万节点 Home/End 导航 | |
| B8 | ResourceTable | 指针拖拽列宽；最小宽度与总宽守恒 | |
| B9 | ResourceTable | resize handle 键盘调整与 `aria-valuenow` | |
| B10 | ResourceTable | `ResizableColumns` 开关、工具栏通知、空列 | |
| B11 | 控制台 | 全程无错误 | |

## 5. 发布记录模板

| 日期 | 浏览器 | 精确版本 | OS | 执行人 | 执行方式 | 场景 | 结果 | 备注 |
|---|---|---|---|---|---|---|---|---|
| | Chrome | | | | 手动/自动 | B1～B11 | | |
| | Edge | | | | | B1～B11 | | |
| | Firefox | | | | | B1～B11 | | |
| | Safari | | | | 手动 | B1～B8 | | |

## 6. 关闭 Gap 的步骤

1. 确定发布时的 N/N-1 版本基线。
2. 对每个浏览器执行最新版自动化 + 第二版本人工/半自动检查。
3. 在 [release-evidence.md](release-evidence.md) 记录精确版本与结果。
4. 把 [requirements-test-matrix.md](requirements-test-matrix.md) 的 REQ-NF-002 改为指向发布记录的覆盖条目。
5. 运行 `python3 scripts/verify_srs_consistency.py --release`，确认 Gap 数量下降。
