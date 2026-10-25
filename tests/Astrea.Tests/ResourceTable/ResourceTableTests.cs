using Astrea.Tests.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable BL0006 // RenderTree frames are required for framework-level render assertions.

// BL0005：组件参数不应在组件外设置。此处为单元测试直接驱动组件参数
// （无 bUnit/渲染器），属必要模式，按测试上下文抑制。
#pragma warning disable BL0005

namespace Astrea.Components.ResourceTable.Tests;

[TestClass]
public class TableColumnTests
{
    [TestMethod]
    public void Constructor_SetsTitle()
    {
        var column = new TableColumn("名称");

        Assert.AreEqual("名称", column.Title);
    }

    [TestMethod]
    public void Defaults_ResizableTrue_MinWidthNull_WidthNull_KeyEmpty()
    {
        var column = new TableColumn("ID");

        Assert.IsTrue(column.Resizable);
        Assert.IsNull(column.MinWidth);
        Assert.IsNull(column.Width);
        Assert.AreEqual(string.Empty, column.Key);
    }
}

[TestClass]
public class ResourceTableColumnsParameterTests
{
    [TestMethod]
    public void ColumnsSetter_AcceptsEmptyCollection()
    {
        var component = new ResourceTable();

        component.Columns = Array.Empty<TableColumn>();

        // 空集合不得被静默忽略（REQ-F-096：空列集合正常渲染）
        Assert.AreEqual(0, component.Columns.Count());
    }

    [TestMethod]
    public void ColumnsSetter_AcceptsNullAsEmpty()
    {
        var component = new ResourceTable();

        component.Columns = null!;

        Assert.AreEqual(0, component.Columns.Count());
    }

    [TestMethod]
    public void ColumnsSetter_PreservesOrder()
    {
        var component = new ResourceTable();
        var columns = new[] { new TableColumn("A"), new TableColumn("B"), new TableColumn("C") };

        component.Columns = columns;

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, component.Columns.Select(c => c.Title).ToList());
    }

    [TestMethod]
    public void ColumnsSetter_SnapshotDoesNotTrackSourceMutations()
    {
        var component = new ResourceTable();
        var list = new List<TableColumn> { new("A") };

        component.Columns = list;
        list.Add(new TableColumn("B"));

        Assert.AreEqual(1, component.Columns.Count());
    }
}

[TestClass]
public class ResourceTableRenderTests
{
    [TestMethod]
    public async Task ResizableColumnsFalse_DoesNotRenderHandles()
    {
        var result = await RenderAsync(
            new[] { new TableColumn("A"), new TableColumn("B"), new TableColumn("C") },
            resizableColumns: false);

        Assert.AreEqual(0, Handles(result).Count);
    }

    [TestMethod]
    public async Task ResizableColumnsTrue_RendersHandlesOnlyForResizableNonLastColumns()
    {
        var result = await RenderAsync(
            new[]
            {
                new TableColumn("A"),
                new TableColumn("B") { Resizable = false },
                new TableColumn("C"),
            },
            resizableColumns: true);

        var handles = Handles(result);
        Assert.AreEqual(1, handles.Count);
        Assert.AreEqual("0", handles[0].Attributes["data-column-index"]);
    }

    [TestMethod]
    public async Task ResizeHandle_ExposesKeyboardSeparatorSemantics()
    {
        var result = await RenderAsync(
            new[] { new TableColumn("A") { Width = "120px", MinWidth = 40 }, new TableColumn("B") },
            resizableColumns: true);

        var handle = Handles(result).Single();
        Assert.AreEqual("separator", handle.Attributes["role"]);
        Assert.AreEqual("vertical", handle.Attributes["aria-orientation"]);
        Assert.AreEqual("0", handle.Attributes["tabindex"]);
        StringAssert.Contains((string)handle.Attributes["aria-label"]!, "A");
        Assert.AreEqual("40", handle.Attributes["aria-valuemin"]);
        Assert.AreEqual("10000", handle.Attributes["aria-valuemax"]);
        Assert.AreEqual("120", handle.Attributes["aria-valuenow"]);
    }

    [TestMethod]
    public async Task EmptyColumns_RenderNoHeaderCellAndKeepBody()
    {
        var result = await RenderAsync(Array.Empty<TableColumn>(), resizableColumns: false);

        Assert.AreEqual(0, result.Elements.Count(element => element.Tag == "th"));
        StringAssert.Contains(result.Text, "body-marker");
    }

    [TestMethod]
    public async Task AdditionalAttributesClass_MergesWithComponentClass()
    {
        var result = await RenderAsync(
            new[] { new TableColumn("A") },
            resizableColumns: false,
            new Dictionary<string, object>
            {
                ["class"] = "app-class",
                ["data-testid"] = "resource-table",
            });

        var root = result.Elements[0];
        Assert.AreEqual("ast-resource-table ast-rt-container app-class", root.Attributes["class"]);
        Assert.AreEqual("resource-table", root.Attributes["data-testid"]);
    }

    [TestMethod]
    public async Task Toolbar_RendersActionButtonsAndDisablesUndoRedo()
    {
        var result = await RenderAsync(
            new[] { new TableColumn("A"), new TableColumn("B") },
            resizableColumns: false,
            showRedoButton: true,
            canUndo: false,
            canRedo: true);

        var buttons = result.Elements.Where(element => element.Tag == "button").ToList();
        Assert.AreEqual(6, buttons.Count);
        var undo = buttons.Single(button => Equals(button.Attributes.GetValueOrDefault("title"), "Undo"));
        var redo = buttons.Single(button => Equals(button.Attributes.GetValueOrDefault("title"), "Redo"));
        Assert.IsTrue(undo.Attributes.ContainsKey("disabled"));
        Assert.IsFalse(redo.Attributes.ContainsKey("disabled"));
    }

    [TestMethod]
    public async Task Toolbar_HidesButtonsWhenShowButtonIsFalseAndOmitsUndoRedoByDefault()
    {
        var hidden = await RenderAsync(
            new[] { new TableColumn("A") },
            resizableColumns: false,
            showButton: false);
        Assert.AreEqual(0, hidden.Elements.Count(element => element.Tag == "button"));

        var defaultToolbar = await RenderAsync(new[] { new TableColumn("A") }, resizableColumns: false);
        Assert.AreEqual(4, defaultToolbar.Elements.Count(element => element.Tag == "button"));
    }

    [TestMethod]
    public async Task Labels_AreConfigurable()
    {
        var result = await RenderAsync(
            new[] { new TableColumn("A"), new TableColumn("B") },
            resizableColumns: true,
            showRedoButton: true,
            addButtonLabel: "新增",
            removeButtonLabel: "删除",
            moveUpButtonLabel: "上移",
            moveDownButtonLabel: "下移",
            undoButtonLabel: "撤销",
            redoButtonLabel: "重做",
            resizeHandleLabelFormat: "调整 {0} 列宽");

        var titles = result.Elements
            .Where(element => element.Tag == "button")
            .Select(element => (string)element.Attributes["title"]!)
            .ToArray();
        Assert.IsTrue(new[] { "新增", "删除", "上移", "下移", "撤销", "重做" }.All(titles.Contains));
        Assert.AreEqual("调整 A 列宽", Handles(result).Single().Attributes["aria-label"]);
    }

    [TestMethod]
    public async Task ToolbarIcons_UseDefaultsAndAllowOverrides()
    {
        var defaults = await RenderAsync(new[] { new TableColumn("A") }, resizableColumns: false);
        Assert.IsTrue(defaults.Elements.Any(element => element.Tag == "svg" &&
            Equals(element.Attributes.GetValueOrDefault("class"), "ast-rt-icon")));

        var customIcon = (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "custom-add-icon");
            builder.CloseElement();
        });
        var custom = await RenderAsync(
            new[] { new TableColumn("A") },
            resizableColumns: false,
            iconAdd: customIcon);
        Assert.IsTrue(custom.Elements.Any(element => element.Tag == "span" &&
            Equals(element.Attributes.GetValueOrDefault("class"), "custom-add-icon")));
    }

    private static List<RenderedElement> Handles(RenderResult result)
        => result.Elements
            .Where(element => Equals(element.Attributes.GetValueOrDefault("class"), "ast-rt-resize-handle"))
            .ToList();

    private static async Task<RenderResult> RenderAsync(
        IEnumerable<TableColumn> columns,
        bool resizableColumns,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        bool showButton = true,
        bool showRedoButton = false,
        bool canUndo = false,
        bool canRedo = false,
        RenderFragment? iconAdd = null,
        string addButtonLabel = "Add",
        string removeButtonLabel = "Remove",
        string moveUpButtonLabel = "Move Up",
        string moveDownButtonLabel = "Move Down",
        string undoButtonLabel = "Undo",
        string redoButtonLabel = "Redo",
        string resizeHandleLabelFormat = "Resize column {0}")
    {
        using var renderer = new TreeTestRenderer();
        var component = new ResourceTable();
        var componentId = renderer.Attach(component);
        var parameters = new Dictionary<string, object?>
        {
            [nameof(ResourceTable.Columns)] = columns,
            [nameof(ResourceTable.TableBody)] = (RenderFragment)(builder => builder.AddContent(0, "body-marker")),
            [nameof(ResourceTable.ResizableColumns)] = resizableColumns,
            [nameof(ResourceTable.ShowButton)] = showButton,
            [nameof(ResourceTable.ShowRedoButton)] = showRedoButton,
            [nameof(ResourceTable.CanUndo)] = canUndo,
            [nameof(ResourceTable.CanRedo)] = canRedo,
            [nameof(ResourceTable.AddButtonLabel)] = addButtonLabel,
            [nameof(ResourceTable.RemoveButtonLabel)] = removeButtonLabel,
            [nameof(ResourceTable.MoveUpButtonLabel)] = moveUpButtonLabel,
            [nameof(ResourceTable.MoveDownButtonLabel)] = moveDownButtonLabel,
            [nameof(ResourceTable.UndoButtonLabel)] = undoButtonLabel,
            [nameof(ResourceTable.RedoButtonLabel)] = redoButtonLabel,
            [nameof(ResourceTable.ResizeHandleLabelFormat)] = resizeHandleLabelFormat,
        };
        if (additionalAttributes is not null)
        {
            parameters[nameof(ResourceTable.AdditionalAttributes)] = additionalAttributes;
        }

        if (iconAdd is not null)
        {
            parameters[nameof(ResourceTable.IconAdd)] = iconAdd;
        }

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));

        var frames = renderer.Frames(componentId);
        return new RenderResult(GetElements(frames), GetText(frames));
    }

    private static List<RenderedElement> GetElements(ArrayRange<RenderTreeFrame> frames)
    {
        var elements = new List<RenderedElement>();
        RenderedElement? current = null;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames.Array[index];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    current = new RenderedElement(frame.ElementName);
                    elements.Add(current);
                    break;
                case RenderTreeFrameType.Attribute when current is not null:
                    current.Attributes[frame.AttributeName] = frame.AttributeValue;
                    break;
            }
        }

        return elements;
    }

    private static string GetText(ArrayRange<RenderTreeFrame> frames)
    {
        var text = new System.Text.StringBuilder();
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames.Array[index];
            if (frame.FrameType == RenderTreeFrameType.Text)
            {
                text.Append(frame.TextContent);
            }
        }

        return text.ToString();
    }

    private sealed class RenderedElement
    {
        public RenderedElement(string tag) => Tag = tag;

        public string Tag { get; }

        public Dictionary<string, object?> Attributes { get; } = new(StringComparer.Ordinal);
    }

    private sealed record RenderResult(List<RenderedElement> Elements, string Text);
}

[TestClass]
public class ResourceTableColumnResizedCallbackTests
{
    [TestMethod]
    public async Task HandleColumnResized_UsesColumnKey_WhenKeyPresent()
    {
        var component = new ResourceTable();
        component.Columns = new[]
        {
            new TableColumn("A") { Key = "col-a" },
            new TableColumn("B") { Key = "col-b" },
        };

        ColumnResizedEventArgs? received = null;
        component.OnColumnResized = EventCallback.Factory.Create<ColumnResizedEventArgs>(this, args => received = args);

        await component.HandleColumnResized(1, 120);

        Assert.IsNotNull(received);
        Assert.AreEqual(1, received!.ColumnIndex);
        Assert.AreEqual("col-b", received.ColumnKey);
        Assert.AreEqual(120, received.NewWidth);
    }

    [TestMethod]
    public async Task HandleColumnResized_OutOfRange_DoesNotNotify()
    {
        var component = new ResourceTable();
        component.Columns = new[] { new TableColumn("A") };

        ColumnResizedEventArgs? received = null;
        component.OnColumnResized = EventCallback.Factory.Create<ColumnResizedEventArgs>(this, args => received = args);

        await component.HandleColumnResized(5, 80);
        await component.HandleColumnResized(-1, 80);

        Assert.IsNull(received);
    }

    [TestMethod]
    public async Task HandleColumnResized_InvalidWidth_DoesNotNotify()
    {
        var component = new ResourceTable();
        component.Columns = new[] { new TableColumn("A") };

        ColumnResizedEventArgs? received = null;
        component.OnColumnResized = EventCallback.Factory.Create<ColumnResizedEventArgs>(this, args => received = args);

        await component.HandleColumnResized(0, -1);
        await component.HandleColumnResized(0, double.NaN);
        await component.HandleColumnResized(0, double.PositiveInfinity);

        Assert.IsNull(received);
    }

    [TestMethod]
    public async Task HandleColumnResized_BelowMinWidth_ClampsToMinWidth()
    {
        var component = new ResourceTable();
        component.Columns = new[] { new TableColumn("A") { Key = "a", MinWidth = 60 } };

        ColumnResizedEventArgs? received = null;
        component.OnColumnResized = EventCallback.Factory.Create<ColumnResizedEventArgs>(this, args => received = args);

        await component.HandleColumnResized(0, 20);

        Assert.IsNotNull(received);
        Assert.AreEqual("a", received!.ColumnKey);
        Assert.AreEqual(60, received.NewWidth);
    }

    [TestMethod]
    public async Task HandleColumnResized_NoSubscriber_NoThrow()
    {
        var component = new ResourceTable();
        component.Columns = new[] { new TableColumn("A") };

        // 未订阅回调（REQ-F-092 AC2）：不得产生错误
        await component.HandleColumnResized(0, 100);
    }
}

#pragma warning restore BL0006
