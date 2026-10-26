using Astrea.Tests.Browser;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Astrea.Tests.Browser.BrowserWait;

namespace Astrea.Tests.ResourceTable;

[TestClass]
[TestCategory("Browser")]
[DoNotParallelize]
public sealed class ResourceTableBrowserTests
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
    public async Task ResourceTable_ToolbarButtons_Notify()
    {
        await using var session = await OpenTableAsync();
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
        await using var session = await OpenTableAsync();
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
        await using var session = await OpenTableAsync();
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
        await using var session = await OpenTableAsync();
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
        await using var session = await OpenTableAsync();
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

    [TestMethod]
    public async Task Accessibility_Axe_NoSeriousViolations()
    {
        await using var session = await OpenTableAsync();
        var page = session.Page;
        var table = page.Locator("[data-testid=demo-resource-table]");
        await table.Locator("tbody tr").First.ClickAsync();

        await AxeBrowserAudit.AssertNoSeriousViolationsAsync(page, "[data-testid=demo-resource-table]");
        session.AssertNoConsoleErrors();
    }

    private static Task<BrowserSession> OpenTableAsync()
        => BrowserSession.OpenAsync(Browser, BaseUrl, "/components/resource-table", "[data-testid=demo-resource-table]");

    private static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser was not initialized.");

    private static string BaseUrl => _baseUrl ?? throw new InvalidOperationException("Demo URL was not initialized.");
}
