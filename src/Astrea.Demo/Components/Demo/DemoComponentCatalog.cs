namespace Astrea.Demo.Components.Demo;

public enum DemoComponentStatus
{
    Active,
    Deferred,
}

public sealed record DemoComponentInfo(
    string Slug,
    string Route,
    string Title,
    string Category,
    DemoComponentStatus Status,
    string Summary,
    IReadOnlyList<string> Capabilities,
    int Order);

public sealed record DemoChapterInfo(string Id, string Title, string Description);

public static class DemoComponentCatalog
{
    public static readonly DemoComponentInfo Tree = new(
        "tree",
        "components/tree",
        "Tree",
        "Data Display",
        DemoComponentStatus.Active,
        "多选、拖放、内联编辑、右键通知、受控展开与 5 万节点虚拟化。",
        new[] { "多选与键盘", "拖放", "编辑", "图标与主题", "受控状态", "虚拟化" },
        10);

    public static readonly DemoComponentInfo ResourceTable = new(
        "resource-table",
        "components/resource-table",
        "ResourceTable",
        "Data Display",
        DemoComponentStatus.Active,
        "列驱动表头、应用行模板、指针与键盘列宽调整、工具栏与撤销/重做通知。",
        new[] { "工具栏", "列宽调整", "空列", "主题变量", "自定义图标" },
        20);

    public static readonly IReadOnlyList<DemoComponentInfo> Active = new[] { Tree, ResourceTable };

    public static readonly IReadOnlyList<DemoComponentInfo> Deferred = new[]
    {
        new DemoComponentInfo("button", string.Empty, "Button", "Basic", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 110),
        new DemoComponentInfo("divider", string.Empty, "Divider", "Basic", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 120),
        new DemoComponentInfo("label", string.Empty, "Label", "Basic", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 130),
        new DemoComponentInfo("input", string.Empty, "Input", "Form", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 210),
        new DemoComponentInfo("select", string.Empty, "Select", "Form", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 220),
        new DemoComponentInfo("alert", string.Empty, "Alert", "Feedback", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 310),
        new DemoComponentInfo("modal", string.Empty, "Modal", "Feedback", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 320),
        new DemoComponentInfo("table", string.Empty, "Table", "Data Display", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 410),
        new DemoComponentInfo("progress", string.Empty, "Progress", "Data Display", DemoComponentStatus.Deferred, string.Empty, Array.Empty<string>(), 420),
    };

    public static readonly IReadOnlyList<IGrouping<string, DemoComponentInfo>> ActiveByCategory =
        Active.GroupBy(component => component.Category, StringComparer.Ordinal).ToArray();

    public static readonly IReadOnlyList<IGrouping<string, DemoComponentInfo>> DeferredByCategory =
        Deferred.GroupBy(component => component.Category, StringComparer.Ordinal).ToArray();
}
