using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    public async Task HandleColumnResized_NullKey_WhenOutOfRange()
    {
        var component = new ResourceTable();
        component.Columns = new[] { new TableColumn("A") };

        ColumnResizedEventArgs? received = null;
        component.OnColumnResized = EventCallback.Factory.Create<ColumnResizedEventArgs>(this, args => received = args);

        await component.HandleColumnResized(5, 80);

        Assert.IsNotNull(received);
        Assert.AreEqual(5, received!.ColumnIndex);
        Assert.IsNull(received.ColumnKey);
        Assert.AreEqual(80, received.NewWidth);
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
