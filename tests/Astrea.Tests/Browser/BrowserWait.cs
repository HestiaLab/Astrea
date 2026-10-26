using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.Browser;

internal static class BrowserWait
{
    public static async Task WaitUntilAsync(Func<Task<bool>> condition, string message)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail(message);
    }

    public static Task<int> CountLogAsync(IPage page, string text)
        => page.Locator(".demo-log div", new PageLocatorOptions { HasText = text }).CountAsync();

    public static async Task WaitForLogAsync(IPage page, string text)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (await CountLogAsync(page, text) > 0)
            {
                return;
            }

            await Task.Delay(50);
        }

        var logText = string.Join(" | ", await page.Locator(".demo-log div").AllTextContentsAsync());
        Assert.Fail($"Log entry '{text}' did not appear. Current log:{Environment.NewLine}{logText}");
    }

    public static Task<double> WidthAsync(ILocator locator)
        => locator.EvaluateAsync<double>("element => element.getBoundingClientRect().width");
}
