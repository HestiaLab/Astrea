using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Tests.Browser;

internal static class TreePageAssertions
{
    public static Task<string[]> SelectedIdsAsync(IPage page)
        => page.Locator("[data-testid=demo-tree] .tree-node.selected")
            .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.nodeId)");

    public static async Task WaitForSelectedIdsAsync(IPage page, params string[] expected)
    {
        string[] ids = Array.Empty<string>();
        for (var attempt = 0; attempt < 200; attempt++)
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

    public static async Task WaitForActiveDescendantAsync(ILocator tree, string nodeId)
    {
        var domId = await tree.Locator($"[data-node-id={nodeId}]").GetAttributeAsync("id");
        await BrowserWait.WaitUntilAsync(
            async () => await tree.GetAttributeAsync("aria-activedescendant") == domId,
            $"Active descendant did not become '{nodeId}'.");
    }

    public static async Task<string> WaitForValidActiveDescendantAsync(ILocator tree)
    {
        string? activeId = null;
        for (var attempt = 0; attempt < 200; attempt++)
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

    public static async Task WaitForActiveNodeAsync(ILocator tree, string expectedNodeId)
    {
        string? activeId = null;
        string? actualNodeId = null;
        for (var attempt = 0; attempt < 200; attempt++)
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

    public static async Task DragAsync(IPage page, ILocator source, ILocator target, float y)
    {
        await source.ScrollIntoViewIfNeededAsync();
        await target.ScrollIntoViewIfNeededAsync();

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
}
