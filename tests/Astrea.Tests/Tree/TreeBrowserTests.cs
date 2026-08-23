using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.Tree;

[TestClass]
[TestCategory("Browser")]
[DoNotParallelize]
public sealed class TreeBrowserTests
{
    private static Process? _demoProcess;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static string? _baseUrl;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var port = GetAvailablePort();
        _baseUrl = $"http://127.0.0.1:{port}";
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "src", "Astrea.Demo", "Astrea.Demo.csproj"));
        startInfo.ArgumentList.Add("--no-build");
        // Match the demo's configuration to the test assembly's, so `dotnet run --no-build`
        // serves the output the test runner actually built (Debug locally, Release in CI).
        // Directory.GetParent mis-resolves trailing-separator paths; use DirectoryInfo instead.
        var buildConfiguration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(buildConfiguration);
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(_baseUrl);

        _demoProcess = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start Astrea.Demo.");
        _demoProcess.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                context.WriteLine(args.Data);
            }
        };
        _demoProcess.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                context.WriteLine(args.Data);
            }
        };
        _demoProcess.BeginOutputReadLine();
        _demoProcess.BeginErrorReadLine();
        await WaitForDemoAsync(_baseUrl, _demoProcess);

        _playwright = await Playwright.CreateAsync();
        var configuredChannel = Environment.GetEnvironmentVariable("ASTREA_BROWSER_CHANNEL");
        var channel = string.IsNullOrWhiteSpace(configuredChannel)
            ? OperatingSystem.IsWindows() ? "msedge" : null
            : string.Equals(configuredChannel, "bundled", StringComparison.OrdinalIgnoreCase) ? null : configuredChannel;
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Channel = channel,
        });
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_demoProcess is { HasExited: false })
        {
            _demoProcess.Kill(entireProcessTree: true);
            await _demoProcess.WaitForExitAsync();
        }

        _demoProcess?.Dispose();
    }

    [TestMethod]
    public async Task KeyboardFocusAndAria_UseOneTabStopAndExistingActiveDescendant()
    {
        await using var session = await BrowserSession.CreateAsync(Browser, BaseUrl);
        var tree = session.Page.Locator("[data-testid=demo-tree]");
        await tree.FocusAsync();
        Assert.AreEqual("0", await tree.GetAttributeAsync("tabindex"));
        Assert.AreEqual(0, await tree.Locator("[role=treeitem]:not([tabindex='-1'])").CountAsync());

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
        var activeId = await WaitForValidActiveDescendantAsync(tree);
        Assert.AreEqual("perf-root", await tree.Locator($"#{activeId}").GetAttributeAsync("data-node-id"));
        await session.Page.Keyboard.PressAsync("ArrowDown");
        await WaitForValidActiveDescendantAsync(tree);
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

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForDemoAsync(string baseUrl, Process process)
    {
        using var client = new HttpClient();
        for (var attempt = 0; attempt < 120; attempt++)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Astrea.Demo exited with code {process.ExitCode}.");
            }

            try
            {
                using var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Dev server is still starting.
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("Astrea.Demo did not become ready within 30 seconds.");
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
