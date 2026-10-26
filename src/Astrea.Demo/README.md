# Astrea Demo

可发布的组件演示站（Blazor WebAssembly）。目录页位于 `/`，每个 Active 组件有独立页面：

- `/components/tree`
- `/components/resource-table`

组件范围以仓库根目录 [README](../README.md) 与 [SRS](../docs/SRS.md) 为准；Deferred 组件没有演示页。

## 页面结构

- `Pages/Index.razor`：纯目录页，只读取 `DemoComponentCatalog`。
- `Pages/Components/*DemoPage.razor`：组件页，负责章节内容与运行状态。
- `Components/Demo/DemoComponentPage.razor`：统一页面 Shell（侧栏、面包屑、页内锚点、按页日志、底部运行状态）。
- `Components/Demo/DemoChapter.razor` / `DemoDetails.razor`：章节与默认折叠的次要信息。
- `Components/Demo/DemoComponentCatalog.cs`：Active / Deferred 组件的单一元数据来源。
- `wwwroot/css/`：`tokens` / `shell` / `demo-primitives` / `tree` / `resource-table`。
- `wwwroot/js/demo-shell.js`：只在路由切换时滚动到顶部并聚焦页面 `h1`。

## 新增一个 Active 组件

1. 在 `Components/Demo/DemoComponentCatalog.cs` 注册：
   ```csharp
   new DemoComponentInfo(
       "my-component",
       "components/my-component",
       "MyComponent",
       "Data Display",
       DemoComponentStatus.Active,
       "一句话说明。",
       new[] { "能力 A", "能力 B" },
       30);
   ```
   目录页、侧栏和路由冒烟测试会自动读取清单。

2. 在 `Pages/Components/` 新建页面：
   ```razor
   @page "/components/my-component"

   <DemoComponentPage Component="@myComponent" Chapters="@Chapters">
       <RuntimeStatus>
           <!-- 状态检查器；由页面持有目标组件的访问器 -->
       </RuntimeStatus>
       <ChildContent>
           <DemoChapter Info="@OverviewChapter">
               <!-- 可运行预览与关键控件保持可见 -->
               <DemoDetails Summary="查看代码">
                   <DemoCodeSample Code="@Sample" />
               </DemoDetails>
           </DemoChapter>
       </ChildContent>
   </DemoComponentPage>
   ```
   `Route` 是相对基路径的链接，不能以 `/` 开头；对应 `@page` 在前面补 `/`（如 `@page "/components/my-component"`）。页面状态和交互放在 `Components/Demo/` 下。

3. 新增浏览器测试：
   - 在 `tests/Astrea.Tests/` 下按组件新增测试类，打开对应路由并使用该组件的选择器。
   - axe 只审计组件容器。
   - `CatalogBrowserTests` 会遍历目录卡片路由并检查页内锚点，无需重复登记。

## 内容约定

- 每个能力一个短 ID 章节（如 `playground`、`editing`、`virtualization`）；页内锚点由清单 `Chapters` 自动生成。
- 主实验台承担核心交互；代码、键盘表、状态、日志默认折叠。
- 需要用户主动操作的预览保持可见，不要藏进折叠区。
- 日志按页面隔离：页面 Shell 创建 `DemoEventLogStore` 并通过 `CascadingValue` 提供；Demo 组件使用 `[CascadingParameter]`，不要注册全局单例。
- 底部 `DemoRuntimePanel` 只显示计数，展开后显示该页状态与最近 20 条事件。
- 不展示 Deferred 组件的可点击 Demo；目录页只保留不可点击的 Roadmap 简表。
- 不新增运行时依赖、外部字体、CDN 或图标库。

## GitHub Pages 部署

Demo 作为 GitHub Pages 项目站发布：<https://hestialab.github.io/Astrea/>。

- `deploy.yml` 在发布后把 `index.html` 的 `<base href>` 改写为 `/Astrea/`。
- 改写完成后，将 `index.html` 复制为 `404.html`，使直接访问或刷新 `/Astrea/components/<name>` 时仍由 Blazor Router 接管。
- 站内链接必须使用基路径相对地址：目录路由写成 `components/tree`，首页使用空 `href`。不要写 `/components/tree` 或 `/`，否则浏览器会跳出 `/Astrea/`。
- 推送 `main` / `dev` 会自动部署；需要手动重跑时，使用 `Deploy demo to GitHub Pages` workflow 的 `workflow_dispatch`。

## 本地验证

```sh
dotnet run --project src/Astrea.Demo/Astrea.Demo.csproj
dotnet build Astrea.sln
dotnet test Astrea.sln --no-build
dotnet format Astrea.sln --verify-no-changes --no-restore
python3 scripts/verify_srs_consistency.py
node --test tests/Astrea.JsTests/*.test.mjs
python3 scripts/verify_package.py
```

浏览器矩阵：

```sh
ASTREA_BROWSER_ENGINE=chromium dotnet test Astrea.sln --no-build --filter "TestCategory=Browser"
ASTREA_BROWSER_ENGINE=firefox  dotnet test Astrea.sln --no-build --filter "TestCategory=Browser"
ASTREA_BROWSER_ENGINE=webkit   dotnet test Astrea.sln --no-build --filter "TestCategory=Browser"
```
