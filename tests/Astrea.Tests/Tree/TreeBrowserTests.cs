using Astrea.Tests.Browser;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Astrea.Tests.Browser.BrowserWait;
using static Astrea.Tests.Browser.TreePageAssertions;

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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
        var tree = session.Page.Locator("[data-testid=demo-tree]");
        await tree.FocusAsync();
        await session.Page.Keyboard.PressAsync("F2");
        var defaultInput = tree.Locator("input.tree-node-edit");
        await defaultInput.WaitForAsync();
        Assert.IsTrue(await defaultInput.EvaluateAsync<bool>("input => input === document.activeElement"));
        await defaultInput.FillAsync("项目-浏览器测试");
        await defaultInput.PressAsync("Enter");
        await defaultInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.AreEqual(1, await CountLogAsync(session.Page, "项目-浏览器测试"));

        await session.Page.Locator("#btn-custom-edit").ClickAsync();
        var customInput = session.Page.Locator("input.demo-custom-editor");
        await customInput.WaitForAsync();
        Assert.IsTrue(await customInput.EvaluateAsync<bool>("input => input === document.activeElement"));
        await customInput.FillAsync("自定义提交");
        await customInput.PressAsync("Enter");
        await customInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
        Assert.AreEqual(1, await CountLogAsync(session.Page, "自定义提交"));
        var customTree = session.Page.Locator("[aria-label='自定义编辑树']");
        await WaitUntilAsync(
            () => customTree.EvaluateAsync<bool>("element => element === document.activeElement"),
            "Custom edit template commit did not restore focus to the tree.");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task VirtualizedFiftyThousandNodes_NavigateAcrossViewportWithValidAria()
    {
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
        var firstTree = session.Page.Locator("[data-testid=demo-tree]");
        Assert.AreEqual(0, await firstTree.Locator(".tree-node-content .tree-node-icon").CountAsync());
        Assert.IsTrue(await session.Page.Locator("[aria-label='第二棵树'] .tree-node-content .tree-node-icon").CountAsync() > 0);
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ClickingTreeItem_KeepsDomFocusOnContainer_AndArrowKeysSyncActiveDescendant()
    {
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
        await using var session = await OpenTreeAsync();
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
    public async Task ContextMenu_NotifiesWithoutChangingSelectionOrFocus()
    {
        await using var session = await OpenTreeAsync();
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
    public async Task Accessibility_Axe_NoSeriousViolations()
    {
        await using var session = await OpenTreeAsync();
        var page = session.Page;
        var tree = page.Locator("[data-testid=demo-tree]");
        await tree.Locator("[data-node-id=df-1] .tree-node-content__main").ClickAsync();
        await WaitForActiveDescendantAsync(tree, "df-1");

        await AxeBrowserAudit.AssertNoSeriousViolationsAsync(page, "[data-testid=demo-tree]");
        session.AssertNoConsoleErrors();
    }

    private static Task<BrowserSession> OpenTreeAsync()
        => BrowserSession.OpenAsync(Browser, BaseUrl, "/components/tree", "[data-testid=demo-tree]");

    private static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser was not initialized.");

    private static string BaseUrl => _baseUrl ?? throw new InvalidOperationException("Demo URL was not initialized.");
}
