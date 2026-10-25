using Microsoft.Playwright;

namespace Astrea.Tests.Browser;

internal static class BrowserEngineLauncher
{
    public static async Task<IBrowser> LaunchAsync(IPlaywright playwright, Action<string>? log = null)
    {
        var engine = Environment.GetEnvironmentVariable("ASTREA_BROWSER_ENGINE");
        var isFirefox = string.Equals(engine, "firefox", StringComparison.OrdinalIgnoreCase);
        var isWebKit = string.Equals(engine, "webkit", StringComparison.OrdinalIgnoreCase);
        string? channel = null;
        if (!isFirefox && !isWebKit)
        {
            var configuredChannel = Environment.GetEnvironmentVariable("ASTREA_BROWSER_CHANNEL");
            channel = string.IsNullOrWhiteSpace(configuredChannel)
                ? OperatingSystem.IsWindows() ? "msedge" : null
                : string.Equals(configuredChannel, "bundled", StringComparison.OrdinalIgnoreCase) ? null : configuredChannel;
        }

        var browserType = isFirefox
            ? playwright.Firefox
            : isWebKit
                ? playwright.Webkit
                : playwright.Chromium;
        var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Channel = channel,
        });
        log?.Invoke($"Browser engine: {(isFirefox ? "firefox" : isWebKit ? "webkit" : channel ?? "chromium")} {browser.Version}");
        return browser;
    }
}
