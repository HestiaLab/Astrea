using System.Text.Json;
using Astrea.Tests.Browser;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.Tree;

[TestClass]
[TestCategory("Browser")]
[DoNotParallelize]
public sealed class TreeBrowserTests
{
    private static BrowserAppHost? _app;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static string? _baseUrl;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        _app = await BrowserAppHost.StartAsync(
            Path.Combine("src", "Astrea.Demo", "Astrea.Demo.csproj"),
            context.WriteLine);
        _baseUrl = _app.BaseUrl;

        _playwright = await Playwright.CreateAsync();
        _browser = await BrowserEngineLauncher.LaunchAsync(_playwright, context.WriteLine);
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task KeyboardFocusAndAria_UseOneTabStopAndExistingActiveDescendant()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var tree = session.Page.Locator("[data-testid=demo-tree]");
        await tree.FocusAsync();
        Assert.AreEqual("0", await tree.GetAttributeAsync("tabindex"));
        Assert.AreEqual(0, await tree.Locator("[role=treeitem][tabindex]").CountAsync());

        await session.Page.Keyboard.PressAsync("ArrowDown");
        var activeId = await tree.GetAttributeAsync("aria-activedescendant");
        Assert.IsFalse(string.IsNullOrEmpty(activeId));
        Assert.AreEqual(1, await tree.Locator($"#{activeId}").CountAsync());

        await tree.FocusAsync();
        await session.Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await tree.EvaluateAsync<bool>("tree => tree === document.activeElement || tree.contains(document.activeElement)"));
        await tree.FocusAsync();
        await session.Page.Keyboard.PressAsync("Shift+Tab");
        Assert.IsFalse(await tree.EvaluateAsync<bool>("tree => tree === document.activeElement || tree.contains(document.activeElement)"));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task EqualLogicalIdsAndDynamicDrag_RemainIsolated()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var ids = await session.Page.Locator("[data-node-id=project]")
            .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.id)");
        Assert.AreEqual(2, ids.Length);
        Assert.AreEqual(2, ids.Distinct(StringComparer.Ordinal).Count());

        var firstItem = session.Page.Locator("[data-testid=demo-tree] [role=treeitem]").First;
        Assert.AreEqual("true", await firstItem.GetAttributeAsync("draggable"));
        Assert.IsNull(await session.Page.Locator("[data-testid=demo-tree] [data-node-id=df-3]").GetAttributeAsync("draggable"));
        await session.Page.Locator("#toggle-drag").UncheckAsync();
        await firstItem.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        Assert.IsNull(await firstItem.GetAttributeAsync("draggable"));
        await session.Page.Locator("#toggle-drag").CheckAsync();
        Assert.AreEqual("true", await firstItem.GetAttributeAsync("draggable"));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task DefaultAndCustomEditing_FocusAndCommitOnce()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var tree = session.Page.Locator("[data-testid=demo-tree]");
        await tree.FocusAsync();
        await session.Page.Keyboard.PressAsync("F2");
        var defaultInput = tree.Locator("input.tree-node-edit");
        await defaultInput.WaitForAsync();
        Assert.IsTrue(await defaultInput.EvaluateAsync<bool>("input => input === document.activeElement"));
        await defaultInput.FillAsync("项目-浏览器测试");
        await defaultInput.PressAsync("Enter");
        await defaultInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.AreEqual(1, await session.Page.Locator(".demo-log div", new PageLocatorOptions
        {
            HasText = "项目-浏览器测试",
        }).CountAsync());

        await session.Page.Locator("#btn-custom-edit").ClickAsync();
        var customInput = session.Page.Locator("input.demo-custom-editor");
        await customInput.WaitForAsync();
        Assert.IsTrue(await customInput.EvaluateAsync<bool>("input => input === document.activeElement"));
        await customInput.FillAsync("自定义提交");
        await customInput.PressAsync("Enter");
        await customInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.AreEqual(1, await session.Page.Locator(".demo-log div", new PageLocatorOptions
        {
            HasText = "自定义提交",
        }).CountAsync());
        var customTree = session.Page.Locator("[aria-label='自定义编辑树']");
        await WaitUntilAsync(
            () => customTree.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Custom edit template commit did not restore focus to the tree.");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task VirtualizedFiftyThousandNodes_NavigateAcrossViewportWithValidAria()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        await session.Page.Locator("#btn-load-50k").ClickAsync();
        var tree = session.Page.Locator("[aria-label='虚拟化树']");
        await tree.Locator("[role=treeitem]").First.WaitForAsync();
        Assert.IsTrue(await tree.Locator("[role=treeitem]").CountAsync() < 100);

        await tree.FocusAsync();
        await session.Page.Keyboard.PressAsync("End");
        await WaitForValidActiveDescendantAsync(tree);
        Assert.IsTrue(await tree.EvaluateAsync<double>("element => element.scrollTop") > 0);

        await session.Page.Keyboard.PressAsync("Home");
        await WaitForActiveNodeAsync(tree, "perf-root");
        await session.Page.Keyboard.PressAsync("ArrowDown");
        await WaitForActiveNodeAsync(tree, "p1");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ShowLink_DrawsIndentConnectors()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var tree = session.Page.Locator("[data-testid=demo-tree]");
        var rootIndent = tree.Locator("[data-node-id=project] .tree-node-indent");
        var childIndent = tree.Locator("[data-node-id=specs] .tree-node-indent");

        var childBackground = await childIndent.EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage");
        Assert.IsTrue(childBackground.Contains("gradient", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("0px", await rootIndent.EvaluateAsync<string>("element => getComputedStyle(element).width"));
        Assert.AreEqual("20px", await childIndent.EvaluateAsync<string>("element => getComputedStyle(element).width"));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task VirtualizedEditing_AfterOffscreenNavigation_KeepsInputUsable()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        await page.Locator("#btn-load-50k").ClickAsync();
        var tree = page.Locator("[aria-label='虚拟化树']");
        await tree.Locator("[role=treeitem]").First.WaitForAsync();
        await tree.FocusAsync();
        await page.Keyboard.PressAsync("End");
        await WaitForValidActiveDescendantAsync(tree);

        await page.Keyboard.PressAsync("F2");
        var input = tree.Locator("input.tree-node-edit");
        await input.WaitForAsync();
        await WaitUntilAsync(
            () => input.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Virtualized edit input did not receive focus.");
        await input.FillAsync("虚拟化编辑");
        await input.PressAsync("Enter");
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await WaitForLogAsync(page, "虚拟化编辑");
        await WaitUntilAsync(
            () => tree.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Virtualized edit commit did not restore focus to the tree container.");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task NodesWithoutContentIcons_DoNotReserveIconContainers()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var firstTree = session.Page.Locator("[data-testid=demo-tree]");
        Assert.AreEqual(0, await firstTree.Locator(".tree-node-content .tree-node-icon").CountAsync());
        Assert.IsTrue(await session.Page.Locator("[aria-label='第二棵树'] .tree-node-content .tree-node-icon").CountAsync() > 0);
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ClickingTreeItem_KeepsDomFocusOnContainer_AndArrowKeysSyncActiveDescendant()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var tree = session.Page.Locator("[data-testid=demo-tree]");
        var item = tree.Locator("[data-node-id=df-1]");
        var itemDomId = await item.GetAttributeAsync("id");

        await item.Locator(".tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            async () => await tree.EvaluateAsync<bool>("element => element === document.activeElement") &&
                        await tree.GetAttributeAsync("aria-activedescendant") == itemDomId,
            "Clicking a tree item did not leave DOM focus and the active descendant on the tree container.");
        Assert.IsFalse(await item.EvaluateAsync<bool>("element => element === document.activeElement"));

        var nextDomId = await tree.Locator("[data-node-id=df-2]").GetAttributeAsync("id");
        await session.Page.Keyboard.PressAsync("ArrowDown");
        await WaitUntilAsync(
            async () => await tree.GetAttributeAsync("aria-activedescendant") == nextDomId,
            "ArrowDown did not move the active descendant.");
        Assert.IsTrue(await tree.EvaluateAsync<bool>("element => element === document.activeElement"));

        await session.Page.Keyboard.PressAsync("Tab");
        Assert.IsFalse(await tree.EvaluateAsync<bool>(
            "element => element === document.activeElement || element.contains(document.activeElement)"));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task EditingEnds_CommitCancelAndBlur_RestoreFocusToTree()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        var item = tree.Locator("[data-node-id=df-1]");
        var itemDomId = await item.GetAttributeAsync("id");
        await item.Locator(".tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            () => tree.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Tree did not receive focus before editing.");

        await page.Keyboard.PressAsync("F2");
        var input = tree.Locator("input.tree-node-edit");
        await input.WaitForAsync();
        Assert.AreEqual("Edit 页面 1", await input.GetAttributeAsync("aria-label"));
        await input.FillAsync("提交一");
        await input.PressAsync("Enter");
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await WaitForLogAsync(page, "重命名：页面 1 → 提交一");
        await WaitUntilAsync(
            async () => await tree.EvaluateAsync<bool>("element => element === document.activeElement") &&
                        await tree.GetAttributeAsync("aria-activedescendant") == itemDomId,
            "Enter commit did not restore focus to the tree and its edited node.");

        await page.Keyboard.PressAsync("F2");
        input = tree.Locator("input.tree-node-edit");
        await input.WaitForAsync();
        await input.FillAsync("取消值");
        await input.PressAsync("Escape");
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await WaitForLogAsync(page, "取消编辑：提交一");
        await WaitUntilAsync(
            () => tree.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Escape cancel did not restore focus to the tree.");

        await page.Keyboard.PressAsync("F2");
        input = tree.Locator("input.tree-node-edit");
        await input.WaitForAsync();
        await input.FillAsync("失焦提交");
        await page.Locator("#toggle-drag").ClickAsync();
        await input.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        await WaitForLogAsync(page, "重命名：提交一 → 失焦提交");
        await WaitUntilAsync(
            () => tree.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Blur commit did not restore focus to the tree.");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task DragDrop_BeforeInsideAfter_NotifiesOnceWithRelation()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        var source = tree.Locator("[data-node-id=df-1]");
        var target = tree.Locator("[data-node-id=df-2]");
        await source.Locator(".tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            () => source.EvaluateAsync<bool>("element => element.classList.contains('selected')"),
            "Drag source was not selected.");

        await DragAsync(page, source, target, y: 2);
        await WaitForLogAsync(page, "拖放：页面 1 → 页面 2（之前）");

        await DragAsync(page, source, target, y: 16);
        await WaitForLogAsync(page, "拖放：页面 1 → 页面 2（之内）");

        await DragAsync(page, source, target, y: 30);
        await WaitForLogAsync(page, "拖放：页面 1 → 页面 2（之后）");

        Assert.AreEqual(1, await CountLogAsync(page, "（之前）"));
        Assert.AreEqual(1, await CountLogAsync(page, "（之内）"));
        Assert.AreEqual(1, await CountLogAsync(page, "（之后）"));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task DragDrop_VetoedOrCyclic_DoesNotNotify()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        var source = tree.Locator("[data-node-id=df-1]");
        await source.Locator(".tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            () => source.EvaluateAsync<bool>("element => element.classList.contains('selected')"),
            "Drag source was not selected.");
        await DragAsync(page, source, tree.Locator("[data-node-id=resources]"), y: 16);
        await WaitForLogAsync(page, "否决拖放 → 资源");
        Assert.AreEqual(0, await CountLogAsync(page, "拖放："));

        var specs = tree.Locator("[data-node-id=specs]");
        await specs.Locator(".tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            () => specs.EvaluateAsync<bool>("element => element.classList.contains('selected')"),
            "Cycle drag source was not selected.");
        await DragAsync(page, specs, source, y: 16);
        await Task.Delay(750);
        Assert.AreEqual(0, await CountLogAsync(page, "拖放："));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task KeyboardMultiSelect_SpaceTogglesAndShiftArrowExtendsRange()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        await tree.FocusAsync();
        await WaitUntilAsync(
            async () => !string.IsNullOrEmpty(await tree.GetAttributeAsync("aria-activedescendant")),
            "Tree did not establish an active descendant on focus.");

        await page.Keyboard.PressAsync("Space");
        await WaitForSelectedIdsAsync(page, "project");
        await page.Keyboard.PressAsync("Space");
        await WaitForSelectedIdsAsync(page);

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await WaitForActiveDescendantAsync(tree, "df-1");
        await page.Keyboard.PressAsync("Space");
        await WaitForSelectedIdsAsync(page, "df-1");

        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.UpAsync("Shift");
        await WaitForActiveDescendantAsync(tree, "df-2");
        await WaitForSelectedIdsAsync(page, "df-1", "df-2");

        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.UpAsync("Shift");
        await WaitForActiveDescendantAsync(tree, "df-3");
        CollectionAssert.AreEquivalent(new[] { "df-1", "df-2" }, await SelectedIdsAsync(page));

        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.UpAsync("Shift");
        await WaitForActiveDescendantAsync(tree, "symbols");
        await WaitForSelectedIdsAsync(page, "df-1", "df-2", "symbols");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task Accessibility_Axe_NoSeriousViolations()
    {
        var axePath = Path.Combine(BrowserAppHost.RepositoryRoot, "tests", "Astrea.A11y", "node_modules", "axe-core", "axe.min.js");
        if (!File.Exists(axePath))
        {
            Assert.Inconclusive("axe-core is not installed. Run: npm ci --prefix tests/Astrea.A11y");
        }

        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        await tree.Locator("[data-node-id=df-1] .tree-node-content__main").ClickAsync();
        await WaitForActiveDescendantAsync(tree, "df-1");
        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Content = await File.ReadAllTextAsync(axePath),
        });

        var raw = await page.EvaluateAsync<string>(@"async () => {
            const targets = [
                document.querySelector('[data-testid=demo-tree]'),
                document.querySelector('[data-testid=demo-resource-table]')
            ].filter(Boolean);
            const violations = [];
            for (const target of targets) {
                const result = await axe.run(target, {
                    runOnly: {
                        type: 'tag',
                        values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']
                    }
                });
                for (const violation of result.violations) {
                    violations.push({
                        id: violation.id,
                        impact: violation.impact,
                        nodes: violation.nodes.map(node => node.target.join(' '))
                    });
                }
            }
            return JSON.stringify(violations);
        }");

        using var document = JsonDocument.Parse(raw);
        var blocking = document.RootElement.EnumerateArray()
            .Where(violation => violation.GetProperty("impact").GetString() is "serious" or "critical")
            .ToList();
        if (blocking.Count > 0)
        {
            var details = string.Join(
                Environment.NewLine,
                blocking.Select(violation =>
                    $"{violation.GetProperty("id").GetString()} ({violation.GetProperty("impact").GetString()}): " +
                    string.Join(", ", violation.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()))));
            Assert.Fail("axe serious/critical violations:" + Environment.NewLine + details);
        }

        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ContextMenu_NotifiesWithoutChangingSelectionOrFocus()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        var item = tree.Locator("[data-node-id=df-1]");
        await item.Locator(".tree-node-content__main").ClickAsync();
        await WaitForSelectedIdsAsync(page, "df-1");
        await WaitForActiveDescendantAsync(tree, "df-1");

        await item.Locator(".tree-node-content__main").ClickAsync(new LocatorClickOptions
        {
            Button = MouseButton.Right,
        });

        await WaitForLogAsync(page, "右键：页面 1");
        CollectionAssert.AreEquivalent(new[] { "df-1" }, await SelectedIdsAsync(page));
        await WaitForActiveDescendantAsync(tree, "df-1");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ResourceTable_ToolbarButtons_Notify()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var table = page.Locator("[data-testid=demo-resource-table]");
        await table.Locator("button[title='Add']").ClickAsync();
        await WaitForLogAsync(page, "添加：资源 4");

        var undo = table.Locator("button[title='Undo']");
        await WaitUntilAsync(async () => !await undo.IsDisabledAsync(), "Undo did not become enabled.");
        await undo.ClickAsync();
        await WaitForLogAsync(page, "撤销");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ResourceTable_ThemeVariable_OverridesBorderColor()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var table = page.Locator("[data-testid=demo-resource-table]");
        var button = table.Locator("button[title='Add']");
        var before = await button.EvaluateAsync<string>("element => getComputedStyle(element).borderColor");

        await table.EvaluateAsync("element => element.style.setProperty('--ast-rt-color-border', 'rgb(255, 0, 0)')");
        await WaitUntilAsync(
            async () => await button.EvaluateAsync<string>("element => getComputedStyle(element).borderColor") == "rgb(255, 0, 0)",
            "The ResourceTable border theme variable did not override the button border.");

        var after = await button.EvaluateAsync<string>("element => getComputedStyle(element).borderColor");
        Assert.AreNotEqual(before, after);
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ResourceTable_ResizeToggle_AddsAndRemovesKeyboardHandles()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var table = page.Locator("[data-testid=demo-resource-table]");
        Assert.AreEqual(3, await table.Locator(".ast-rt-resize-handle").CountAsync());
        var firstHandle = table.Locator(".ast-rt-resize-handle").First;
        Assert.AreEqual("separator", await firstHandle.GetAttributeAsync("role"));
        Assert.AreEqual("0", await firstHandle.GetAttributeAsync("tabindex"));
        StringAssert.Contains(await firstHandle.GetAttributeAsync("aria-label"), "ID");

        await page.Locator("#toggle-rt-resizable").UncheckAsync();
        await WaitUntilAsync(
            async () => await table.Locator(".ast-rt-resize-handle").CountAsync() == 0,
            "Disabling ResizableColumns did not remove resize handles.");

        await page.Locator("#toggle-rt-resizable").CheckAsync();
        await WaitUntilAsync(
            async () => await table.Locator(".ast-rt-resize-handle").CountAsync() == 3,
            "Re-enabling ResizableColumns did not restore resize handles.");
        firstHandle = table.Locator(".ast-rt-resize-handle").First;
        await firstHandle.ScrollIntoViewIfNeededAsync();
        await firstHandle.FocusAsync();
        for (var attempt = 0; attempt < 10 && await CountLogAsync(page, "列宽调整：列 0（id）") == 0; attempt++)
        {
            await page.Keyboard.PressAsync("ArrowRight");
            await Task.Delay(100);
        }

        await WaitForLogAsync(page, "列宽调整：列 0（id）");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ResourceTable_KeyboardResize_AdjustsWidthsAndForwardsKey()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var table = page.Locator("[data-testid=demo-resource-table]");
        var headers = table.Locator("thead th");
        var first = headers.Nth(0);
        var second = headers.Nth(1);
        var firstBefore = await WidthAsync(first);
        var secondBefore = await WidthAsync(second);
        var handle = table.Locator(".ast-rt-resize-handle").First;

        await handle.ScrollIntoViewIfNeededAsync();
        await handle.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForLogAsync(page, "列宽调整：列 0（id）");
        await WaitUntilAsync(
            async () => Math.Abs(await WidthAsync(first) - (firstBefore + 10)) < 1.5,
            "ArrowRight did not widen the focused column by the keyboard step.");
        Assert.IsTrue(Math.Abs(await WidthAsync(first) + await WidthAsync(second) - (firstBefore + secondBefore)) < 1.5);
        await WaitForLogAsync(page, "键盘：ArrowRight");

        await page.Keyboard.PressAsync("ArrowLeft");
        await WaitUntilAsync(
            async () => Math.Abs(await WidthAsync(first) - firstBefore) < 1.5,
            "ArrowLeft did not restore the column width.");
        Assert.AreEqual(2, await CountLogAsync(page, "列宽调整：列 0（id）"));
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ResourceTable_PointerResize_ConservesWidthAndNotifiesOnce()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var page = session.Page;
        var table = page.Locator("[data-testid=demo-resource-table]");
        var headers = table.Locator("thead th");
        var first = headers.Nth(0);
        var second = headers.Nth(1);
        var firstBefore = await WidthAsync(first);
        var secondBefore = await WidthAsync(second);
        var handle = table.Locator(".ast-rt-resize-handle").First;
        await handle.ScrollIntoViewIfNeededAsync();
        var box = await handle.BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = box!.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 20, y);
        await Task.Delay(150);
        await page.Mouse.UpAsync();

        await WaitForLogAsync(page, "列宽调整：列 0（id）");
        await WaitUntilAsync(
            async () => Math.Abs(await WidthAsync(first) - (firstBefore + 20)) < 2,
            "Pointer drag did not widen the first column.");
        Assert.IsTrue(Math.Abs(await WidthAsync(first) + await WidthAsync(second) - (firstBefore + secondBefore)) < 2);
        Assert.AreEqual(1, await CountLogAsync(page, "列宽调整：列 0（id）"));
        session.AssertNoConsoleErrors();
    }

    private static Task<double> WidthAsync(ILocator locator)
        => locator.EvaluateAsync<double>("element => element.getBoundingClientRect().width");

    private static Task<string[]> SelectedIdsAsync(IPage page)
        => page.Locator("[data-testid=demo-tree] .tree-node.selected")
            .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.nodeId)");

    private static async Task WaitForSelectedIdsAsync(IPage page, params string[] expected)
    {
        string[] ids = Array.Empty<string>();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            ids = await SelectedIdsAsync(page);
            if (ids.Length == expected.Length && expected.All(ids.Contains))
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail($"Selected nodes did not become [{string.Join(", ", expected)}]. Actual: [{string.Join(", ", ids)}]");
    }

    private static async Task WaitForActiveDescendantAsync(ILocator tree, string nodeId)
    {
        var domId = await tree.Locator($"[data-node-id={nodeId}]").GetAttributeAsync("id");
        await WaitUntilAsync(
            async () => await tree.GetAttributeAsync("aria-activedescendant") == domId,
            $"Active descendant did not become '{nodeId}'.");
    }

    private static async Task DragAsync(IPage page, ILocator source, ILocator target, float y)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("ASTREA_BROWSER_ENGINE"), "webkit", StringComparison.OrdinalIgnoreCase))
        {
            var sourceHandle = await source.ElementHandleAsync();
            var targetHandle = await target.ElementHandleAsync();
            Assert.IsNotNull(sourceHandle);
            Assert.IsNotNull(targetHandle);
            var relation = y < 10 ? "before" : y > 20 ? "after" : "inside";
            await page.EvaluateAsync(
                @"({ source, target, relation }) => {
                    const dataTransfer = new DataTransfer();
                    const rect = target.getBoundingClientRect();
                    const offsetY = relation === 'before' ? 2 : relation === 'inside' ? rect.height / 2 : rect.height - 2;
                    const clientX = rect.left + 20;
                    const clientY = rect.top + offsetY;
                    const event = (type) => {
                        const dragEvent = new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer });
                        // WebKit ignores clientX/clientY in the DragEvent constructor for synthetic events.
                        Object.defineProperty(dragEvent, 'clientX', { value: clientX });
                        Object.defineProperty(dragEvent, 'clientY', { value: clientY });
                        return dragEvent;
                    };
                    source.dispatchEvent(event('dragstart'));
                    target.dispatchEvent(event('dragover'));
                    target.dispatchEvent(event('drop'));
                    source.dispatchEvent(event('dragend'));
                }",
                new { source = sourceHandle, target = targetHandle, relation });
            return;
        }

        await source.DragToAsync(target, new LocatorDragToOptions
        {
            TargetPosition = new TargetPosition { X = 20, Y = y }
        });
    }

    private static Task<int> CountLogAsync(IPage page, string text)
        => page.Locator(".demo-log div", new PageLocatorOptions { HasText = text }).CountAsync();

    private static async Task WaitForLogAsync(IPage page, string text)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await CountLogAsync(page, text) > 0)
            {
                return;
            }

            await Task.Delay(50);
        }

        var logText = await page.Locator(".demo-log").First.InnerTextAsync();
        Assert.Fail($"Log entry '{text}' did not appear. Current log:{Environment.NewLine}{logText}");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, string message)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail(message);
    }

    private static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser was not initialized.");

    private static string BaseUrl => _baseUrl ?? throw new InvalidOperationException("Demo URL was not initialized.");

    private static async Task<string> WaitForValidActiveDescendantAsync(ILocator tree)
    {
        string? activeId = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            activeId = await tree.GetAttributeAsync("aria-activedescendant");
            if (!string.IsNullOrEmpty(activeId) && await tree.Locator($"#{activeId}").CountAsync() == 1)
            {
                return activeId;
            }

            await Task.Delay(50);
        }

        Assert.Fail($"aria-activedescendant did not resolve to a rendered element. Last value: {activeId}");
        return string.Empty;
    }

    private static async Task WaitForActiveNodeAsync(ILocator tree, string expectedNodeId)
    {
        string? activeId = null;
        string? actualNodeId = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            activeId = await tree.GetAttributeAsync("aria-activedescendant");
            if (!string.IsNullOrEmpty(activeId))
            {
                actualNodeId = await tree.EvaluateAsync<string?>(
                    "(_element, id) => document.getElementById(id)?.getAttribute('data-node-id')",
                    activeId);
                if (actualNodeId == expectedNodeId)
                {
                    return;
                }
            }

            await Task.Delay(50);
        }

        Assert.Fail($"Active descendant did not resolve to '{expectedNodeId}'. Last active id: {activeId}, node: {actualNodeId}");
    }

    private sealed class BrowserSession : IAsyncDisposable
    {
        private readonly List<string> _consoleErrors = new();

        private BrowserSession(IPage page)
        {
            Page = page;
            Page.Console += (_, message) =>
            {
                if (message.Type == "error")
                {
                    _consoleErrors.Add(message.Text);
                }
            };
            Page.PageError += (_, error) => _consoleErrors.Add(error);
        }

        public IPage Page { get; }

        public static async Task<BrowserSession> CreateAsync(IBrowser browser, string baseUrl)
        {
            var page = await browser.NewPageAsync();
            var session = new BrowserSession(page);
            await page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("[data-testid=demo-tree]").WaitForAsync();
            return session;
        }

        public void AssertNoConsoleErrors()
            => Assert.AreEqual(0, _consoleErrors.Count, string.Join(Environment.NewLine, _consoleErrors));

        public async ValueTask DisposeAsync() => await Page.CloseAsync();
    }
}
