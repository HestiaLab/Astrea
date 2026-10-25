using Astrea.Tests.Browser;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.ServerSmoke;

[TestClass]
[TestCategory("Browser")]
[DoNotParallelize]
public sealed class ServerSmokeBrowserTests
{
    private static BrowserAppHost? _app;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        _app = await BrowserAppHost.StartAsync(
            Path.Combine("tests", "Astrea.ServerSmoke", "Astrea.ServerSmoke.csproj"),
            context.WriteLine);
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
    public async Task ServerHost_PrerendersInitialComponentContent()
    {
        using var client = new HttpClient();

        var html = await client.GetStringAsync(BaseUrl);

        StringAssert.Contains(html, "data-testid=\"server-tree\"");
        StringAssert.Contains(html, "data-testid=\"server-resource-table\"");
        StringAssert.Contains(html, "Server root");
        Assert.IsFalse(html.Contains("server-interactive", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ServerTree_IsInteractiveAfterConnection()
    {
        await using var session = await PageSession.OpenAsync(Browser, BaseUrl, "[data-testid=server-tree]");
        var page = session.Page;
        var tree = page.Locator("[data-testid=server-tree]");
        var child = tree.Locator("[data-node-id=server-child]");
        await child.Locator(".tree-node-content__main").ClickAsync();
        await WaitUntilAsync(
            () => child.EvaluateAsync<bool>("element => element.classList.contains('selected')"),
            "Server tree did not accept a click after the circuit connected.");

        var childDomId = await child.GetAttributeAsync("id");
        await WaitUntilAsync(
            async () => await tree.GetAttributeAsync("aria-activedescendant") == childDomId,
            "Server tree did not update its active descendant after selection.");

        var rootDomId = await tree.Locator("[data-node-id=server-root]").GetAttributeAsync("id");
        await page.Keyboard.PressAsync("ArrowUp");
        await WaitUntilAsync(
            async () => await tree.GetAttributeAsync("aria-activedescendant") == rootDomId,
            "Server tree keyboard navigation did not move focus to the parent.");
        session.AssertNoConsoleErrors();
    }

    [TestMethod]
    public async Task ServerResourceTable_KeyboardResizeNotifies()
    {
        await using var session = await PageSession.OpenAsync(Browser, BaseUrl, "[data-testid=server-resource-table]");
        var page = session.Page;
        var table = page.Locator("[data-testid=server-resource-table]");
        var handle = table.Locator(".ast-rt-resize-handle").First;
        await handle.ScrollIntoViewIfNeededAsync();
        var firstHeader = table.Locator("thead th").First;
        var before = await firstHeader.EvaluateAsync<double>("element => element.getBoundingClientRect().width");

        double after = before;
        for (var attempt = 0; attempt < 20 && Math.Abs(after - before) < 1; attempt++)
        {
            await handle.FocusAsync();
            await page.Keyboard.PressAsync("ArrowRight");
            await Task.Delay(100);
            after = await firstHeader.EvaluateAsync<double>("element => element.getBoundingClientRect().width");
        }

        Assert.IsTrue(Math.Abs(after - (before + 10)) < 1.5, $"Server resize moved width from {before} to {after}.");
        await WaitUntilAsync(
            async () => (await page.Locator("[data-testid=server-resize-count]").InnerTextAsync()).Contains('1'),
            "Server ResourceTable did not notify OnColumnResized.");
        session.AssertNoConsoleErrors();
    }

    private static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser was not initialized.");

    private static string BaseUrl => _app?.BaseUrl ?? throw new InvalidOperationException("Server host was not initialized.");

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

    private sealed class PageSession : IAsyncDisposable
    {
        private readonly List<string> _consoleErrors = new();

        private PageSession(IPage page)
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

        public static async Task<PageSession> OpenAsync(IBrowser browser, string baseUrl, string readySelector)
        {
            var page = await browser.NewPageAsync();
            var session = new PageSession(page);
            await page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator(readySelector).WaitForAsync();
            await page.Locator("[data-testid=server-interactive]").WaitForAsync();
            return session;
        }

        public void AssertNoConsoleErrors()
            => Assert.AreEqual(0, _consoleErrors.Count, string.Join(Environment.NewLine, _consoleErrors));

        public async ValueTask DisposeAsync() => await Page.CloseAsync();
    }
}
