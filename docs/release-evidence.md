# Astrea 发布验证证据

> 需求状态与证据映射见 [requirements-test-matrix.md](requirements-test-matrix.md)。本文件记录实际执行的浏览器、可访问性与 Blazor Web App 验证结果。

## 0.9.0 候选（未发布，2026-10-30）

> 本节记录 0.9.0 候选版本的本地验证结果；该版本尚未发布到 GitHub Packages。

### 环境

| 项目 | 值 |
|---|---|
| 验证日期 | 2026-10-30 |
| 本地操作系统 | Windows 11（10.0.26300） |
| 构建 SDK | .NET SDK 10.0.401，目标框架 net10.0 |
| ASP.NET Core 包 | 10.0.14 |
| Playwright | 1.64.1 |
| axe-core | 4.14.0（MPL-2.0，仅测试依赖） |
| 应用 | `src/Astrea.Demo`（standalone WebAssembly）与 `tests/Astrea.ServerSmoke`（Blazor Web App，Interactive Server） |

### 浏览器矩阵

命令：`ASTREA_BROWSER_ENGINE=<engine> dotnet test Astrea.sln --no-build -c Release --filter "TestCategory=Browser"`。

| 引擎 | 版本 | 用例数 | 结果 | 备注 |
|---|---:|---:|---|---|
| Edge（msedge） | 156.0.4314.50 | 25 | 通过 | Windows 本地默认引擎 |
| Chromium（bundled） | 156.0.8078.4 | 25 | 通过 | `ASTREA_BROWSER_CHANNEL=bundled` |
| Firefox | 157.0 | 25 | 通过 | Playwright Firefox |
| WebKit | 27.2 | 25 | 通过（合成拖放） | Playwright WebKit；原生 HTML5 拖放启动不可自动化，Tree 拖放用例改为派发合成 `DragEvent`，仍经过真实 JS/C# 链路 |

每个浏览器用例都断言页面控制台无错误。CI 在 Ubuntu 上以 Chromium/Firefox/WebKit 三引擎矩阵运行同一组 Browser 分类测试；Edge 由 Windows 本地验证记录。

### 覆盖内容

- Tree：点击后的 active-descendant 焦点、方向键/Home/End、Tab 离开、Space 与 Shift+范围多选、编辑 Enter/Escape/blur 焦点恢复、拖放 before/inside/after、否决与循环拒绝、右键通知、虚拟化 5 万节点。
- ResourceTable：指针与键盘列宽调整、最小宽度、总宽守恒、`ResizableColumns` 运行期开关、工具栏与撤销/重做通知、空列、图标覆盖、键盘事件转发、`--ast-rt-*` 主题变量覆盖。
- 内置文本：Tree `LoadingText` / `EditLabelFormat`，ResourceTable 工具栏与 resize handle 标签参数；渲染测试验证自定义值。
- Blazor Web App（Interactive Server）：预呈现初始 HTML、`_blazor` 协商与 WebSocket 连接、连接后 Tree 可交互、ResourceTable 键盘调整并触发 `OnColumnResized`。
- 构建与测试：`dotnet build -c Release` 0 警告 0 错误；非浏览器测试 225/225 通过；`dotnet format --verify-no-changes` 通过；`verify_srs_consistency.py` 通过（保留 2 条既有发布 Gap 警告）。
- JS 单元测试：`node --test tests/Astrea.JsTests/*.test.mjs` 7/7 通过。
- NuGet 漏洞审计：`dotnet list Astrea.sln package --vulnerable --include-transitive` 报告 0 个易受攻击包。

### NuGet 包与外部消费

- `scripts/verify_package.py` 运行 `dotnet pack -c Release` 生成 `Astrea.Components.0.9.0.nupkg`，包含 `lib/net10.0/Astrea.Components.dll`、`README.md`、`LICENSE`、`CHANGELOG.md` 与四个 `staticwebassets/` 资源；脚本断言包内不含 `lib/net6.0` 资产。
- 探针使用临时 NuGet 缓存和本地包源，复制干净的 Blazor Web App 与 standalone WebAssembly 应用，将 ProjectReference 替换为 `PackageReference Include="Astrea.Components" Version="0.9.0"`：
  - Blazor Web App：还原、构建、启动；HTTP 检查预呈现页面与 `_content/Astrea.Components/*` 静态资源。
  - standalone WebAssembly：还原、发布；检查发布目录包含全部四个 `_content/Astrea.Components/*` 静态资源。
- 结果：**PACKAGE PROBE PASSED（Astrea.Components 0.9.0）**。
- SDK 策略：`global.json` 固定 .NET SDK 10.0.401（`latestPatch`）；发布资产为 net10.0。

## 0.8.0 历史记录（2026-10-26）

### 环境

| 项目 | 值 |
|---|---|
| 验证日期 | 2026-10-26 |
| 本地操作系统 | Windows 11（10.0.26300） |
| 构建 SDK | .NET SDK 10.0.401，目标框架 net6.0 |
| Playwright | 1.61.0 |
| axe-core | 4.14.0（MPL-2.0，仅测试依赖） |
| 应用 | `src/Astrea.Demo`（WebAssembly）与 `tests/Astrea.ServerSmoke`（Blazor Server） |

### 浏览器矩阵

命令：`dotnet test Astrea.sln --no-build --filter "TestCategory=Browser"`，通过 `ASTREA_BROWSER_ENGINE` 选择引擎。

| 引擎 | 版本 | 用例数 | 结果 | 备注 |
|---|---:|---:|---|---|
| Edge（msedge） | 156.0.4314.42 | 25 | 通过 | Windows 本地默认引擎 |
| Chromium（bundled） | 149.0.7827.55 | 25 | 通过 | `ASTREA_BROWSER_CHANNEL=bundled` |
| Firefox | 151.0 | 25 | 通过 | Playwright Firefox |
| WebKit | 26.5 | 25 | 通过（合成拖放） | Playwright WebKit；原生 HTML5 拖放启动不可自动化，Tree 拖放用例改为派发合成 `DragEvent`，仍经过真实 JS/C# 链路 |

每个浏览器用例都断言页面控制台无错误。CI 在 Ubuntu 上以 Chromium/Firefox/WebKit 三引擎矩阵运行同一组 Browser 分类测试；Edge 由 Windows 本地验证记录。

### 覆盖内容

- Tree：点击后的 active-descendant 焦点、方向键/Home/End、Tab 离开、Space 与 Shift+范围多选、编辑 Enter/Escape/blur 焦点恢复、拖放 before/inside/after、否决与循环拒绝、右键通知、虚拟化 5 万节点。
- ResourceTable：指针与键盘列宽调整、最小宽度、总宽守恒、`ResizableColumns` 运行期开关、工具栏与撤销/重做通知、空列、图标覆盖、键盘事件转发、`--ast-rt-*` 主题变量覆盖。
- 内置文本：Tree `LoadingText` / `EditLabelFormat`，ResourceTable 工具栏与 resize handle 标签参数；渲染测试验证自定义值。
- Blazor Server：预呈现初始 HTML、连接后 Tree 可交互、ResourceTable 键盘调整并触发 `OnColumnResized`。

### NuGet 包与外部消费

- `scripts/verify_package.py` 运行 `dotnet pack -c Release` 生成 `Astrea.Components.0.8.0.nupkg`，包含 `lib/net6.0/Astrea.Components.dll`、`README.md`、`LICENSE` 与四个 `staticwebassets/` 资源。
- 包元数据包含 MIT 许可证、README、版本 0.8.0、仓库 URL 与包标签。
- 探针使用临时 NuGet 缓存和本地包源，复制干净的 Blazor Server 与 WebAssembly 应用，将 ProjectReference 替换为 `PackageReference Include="Astrea.Components" Version="0.8.0"`：
  - Blazor Server：还原、构建、启动；HTTP 检查预呈现页面与 `_content/Astrea.Components/*` 静态资源。
  - Blazor WebAssembly：还原、发布；检查发布目录包含全部四个 `_content/Astrea.Components/*` 静态资源。
- 结果：**PACKAGE PROBE PASSED（Astrea.Components 0.8.0）**。
- SDK 策略：`global.json` 固定 .NET 10 LTS；发布资产为 net6.0。

## 共享审计与发布门禁

### axe 自动审计

`TreeBrowserTests.Accessibility_Axe_NoSeriousViolations` 在 0.8.0 与 0.9.0 候选的四引擎矩阵上运行，范围限定为 Tree 与 ResourceTable 组件容器，规则标签为 `wcag2a`、`wcag2aa`、`wcag21a`、`wcag21aa`、`wcag22aa`。结果：**0 个 serious/critical 违规**。

- 应用页面的演示文案、布局与示例内容不属于组件库责任边界，未纳入组件审计范围。
- 自动审计不能替代人工屏幕阅读器验证。

### 人工键盘检查清单

自动浏览器用例已覆盖以下键盘路径，0.9.0 候选矩阵记录如下：

| 路径 | 结果 |
|---|---|
| Tab 进入/离开 Tree | 通过（自动） |
| 方向键、Home、End 移动 active descendant | 通过（自动） |
| Space 切换、Shift+方向键范围多选 | 通过（自动） |
| F2 编辑、Enter 提交、Escape 取消、blur 提交后的焦点恢复 | 通过（自动） |
| ResourceTable resize handle 聚焦与 ArrowLeft/ArrowRight 调整 | 通过（自动） |
| 屏幕阅读器人工会话（NVDA/JAWS/VoiceOver） | **未执行**（发布阻断 Gap，见 REQ-NF-004） |

### 已知限制

- Playwright WebKit 不能自动启动原生 HTML5 拖放；WebKit 的拖放验证使用合成 `DragEvent`，未覆盖原生拖放手势。
- Edge 仅在 Windows 本地验证；CI 矩阵覆盖 Chromium/Firefox/WebKit。
- 每个引擎当前只验证一个最新版本；REQ-NF-002 要求的“最近两个主版本”中的第二个版本仍为发布阻断 Gap。
- 未执行人工辅助技术会话；REQ-NF-004 仍为发布阻断 Gap。
- REQ-NF-003 已随 Table 组件 Deferred，当前版本无 Active 性能基准要求。

### 待执行的最终验证计划

| Gap | 执行文档 | 状态 |
|---|---|---|
| REQ-NF-002 第二浏览器主版本 | [browser-version-verification-plan.md](browser-version-verification-plan.md) | 计划就绪，待执行 |
| REQ-NF-004 人工屏幕阅读器/WCAG | [accessibility-validation-checklist.md](accessibility-validation-checklist.md) | 清单就绪，待执行 |

两项都需要仓库所有者/QA 在真实浏览器与辅助技术环境中执行；执行后更新本文件与需求矩阵，并用 `--release` 复核。

### 当前发布阻断项

| 需求 | 剩余工作 |
|---|---|
| REQ-NF-002 | 每个浏览器最近两个主版本中的第二个版本尚未单独验证（当前矩阵覆盖各引擎最新版） |
| REQ-NF-004 | 人工屏幕阅读器/WCAG 验证 |

`python3 scripts/verify_srs_consistency.py --release` 会在这 2 个 Gap 关闭前失败；0.9.0 候选尚未发布。
