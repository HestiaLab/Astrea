using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.Browser;

internal static class AxeBrowserAudit
{
    public static async Task AssertNoSeriousViolationsAsync(IPage page, string scopeSelector)
    {
        var axePath = Path.Combine(
            BrowserAppHost.RepositoryRoot,
            "tests",
            "Astrea.A11y",
            "node_modules",
            "axe-core",
            "axe.min.js");
        if (!File.Exists(axePath))
        {
            Assert.Inconclusive("axe-core is not installed. Run: npm ci --prefix tests/Astrea.A11y");
        }

        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Content = await File.ReadAllTextAsync(axePath),
        });

        var raw = await page.EvaluateAsync<string>(
            @"async (selector) => {
                const target = document.querySelector(selector);
                if (!target) {
                    return JSON.stringify({ missing: true });
                }

                const result = await axe.run(target, {
                    runOnly: {
                        type: 'tag',
                        values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']
                    }
                });
                return JSON.stringify(result.violations.map(violation => ({
                    id: violation.id,
                    impact: violation.impact,
                    nodes: violation.nodes.map(node => node.target.join(' '))
                })));
            }",
            scopeSelector);

        using var document = JsonDocument.Parse(raw);
        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("missing", out var missing) &&
            missing.GetBoolean())
        {
            Assert.Fail($"Axe target was not found: {scopeSelector}");
        }

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
    }
}
