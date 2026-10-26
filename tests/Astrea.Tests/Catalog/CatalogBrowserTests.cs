using Astrea.Tests.Browser;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Astrea.Tests.Browser.BrowserWait;

namespace Astrea.Tests.Catalog;

[TestClass]
[TestCategory("Browser")]
[DoNotParallelize]
public sealed class CatalogBrowserTests
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
    public async Task Catalog_ListsActiveComponentsAndNavigates()
    {
        await using var session = await BrowserSession.OpenAsync(
            Browser,
            BaseUrl,
            "/",
            "[data-testid=component-catalog]");
        var page = session.Page;

        Assert.AreEqual(0, await page.Locator("[data-testid=demo-tree]").CountAsync());
        Assert.AreEqual(0, await page.Locator("[data-testid=demo-resource-table]").CountAsync());

        var treeCard = page.Locator(".demo-catalog-card[href='/components/tree']");
        var tableCard = page.Locator(".demo-catalog-card[href='/components/resource-table']");
        Assert.AreEqual(1, await treeCard.CountAsync());
        Assert.AreEqual(1, await tableCard.CountAsync());
        Assert.AreEqual(1, await page.Locator(".demo-deferred").CountAsync());
        Assert.AreEqual(0, await page.Locator(".demo-deferred a[href^='/components/']").CountAsync());

        await page.EvaluateAsync("window.scrollTo({ top: 400, behavior: 'instant' })");
        await treeCard.ClickAsync();
        await page.WaitForURLAsync("**/components/tree");
        await page.Locator("[data-testid=demo-tree]").WaitForAsync();
        await WaitUntilAsync(
            () => page.EvaluateAsync<bool>(
                "window.scrollY === 0 && document.activeElement?.hasAttribute('data-demo-page-heading') === true"),
            "Route navigation did not reset scroll and focus the page heading.");
        Assert.AreEqual("Tree", await page.Locator("h1[data-demo-page-heading]").InnerTextAsync());
        await AssertAnchorTargetsExistAsync(page);

        await page.Locator("a.demo-brand").ClickAsync();
        await page.Locator("[data-testid=component-catalog]").WaitForAsync();
        await WaitUntilAsync(
            () => page.EvaluateAsync<bool>(
                "window.scrollY === 0 && document.activeElement?.hasAttribute('data-demo-page-heading') === true"),
            "Returning to the catalog did not reset scroll and focus the page heading.");

        var routes = await page.Locator(".demo-catalog-card")
            .EvaluateAllAsync<string[]>("cards => cards.map(card => card.getAttribute('href'))");
        Assert.AreEqual(2, routes.Length);
        foreach (var route in routes)
        {
            await page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var heading = page.Locator("main h1[data-demo-page-heading]");
            await heading.WaitForAsync();
            Assert.IsFalse(string.IsNullOrWhiteSpace(await heading.InnerTextAsync()));
            await AssertAnchorTargetsExistAsync(page);
        }

        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task SidebarNavigation_IsolatesPerPageLogAndFocusesHeading()
    {
        await using var session = await BrowserSession.OpenAsync(
            Browser,
            BaseUrl,
            "/components/tree",
            "[data-testid=demo-tree]");
        var page = session.Page;

        await page.Locator("[data-testid=demo-tree] [data-node-id=df-1] .tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            async () => await page.Locator(".demo-log div").CountAsync() > 0,
            "Tree page did not record an event.");

        await page.EvaluateAsync("window.scrollTo({ top: 300, behavior: 'instant' })");
        await page.Locator(".demo-component-sidebar a.demo-component-nav__link[href='/components/resource-table']").ClickAsync();
        await page.Locator("[data-testid=demo-resource-table]").WaitForAsync();
        await WaitUntilAsync(
            () => page.EvaluateAsync<bool>(
                "window.scrollY === 0 && document.activeElement?.hasAttribute('data-demo-page-heading') === true"),
            "Sidebar navigation did not reset scroll and focus the page heading.");

        Assert.AreEqual(0, await page.Locator(".demo-log div").CountAsync());
        Assert.AreEqual("ResourceTable", await page.Locator("h1[data-demo-page-heading]").InnerTextAsync());
        session.AssertNoConsoleErrors();
    }

    private static async Task AssertAnchorTargetsExistAsync(IPage page)
    {
        var missing = await page.EvaluateAsync<string[]>(
            "() => Array.from(document.querySelectorAll('.demo-anchor-nav a'))" +
            ".filter(link => !document.getElementById(link.getAttribute('href').slice(1)))" +
            ".map(link => link.getAttribute('href'))");
        CollectionAssert.AreEqual(Array.Empty<string>(), missing);
        Assert.IsTrue(await page.Locator(".demo-anchor-nav a").CountAsync() > 0);
    }

    private static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser was not initialized.");

    private static string BaseUrl => _baseUrl ?? throw new InvalidOperationException("Demo URL was not initialized.");
}
