using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.Browser;

internal sealed class BrowserSession : IAsyncDisposable
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

    public static async Task<BrowserSession> OpenAsync(
        IBrowser browser,
        string baseUrl,
        string path,
        string readySelector)
    {
        var page = await browser.NewPageAsync();
        var session = new BrowserSession(page);
        await page.GotoAsync($"{baseUrl}{path}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(readySelector).WaitForAsync();
        return session;
    }

    public void AssertNoConsoleErrors()
        => Assert.AreEqual(0, _consoleErrors.Count, string.Join(Environment.NewLine, _consoleErrors));

    public async ValueTask DisposeAsync() => await Page.CloseAsync();
}
