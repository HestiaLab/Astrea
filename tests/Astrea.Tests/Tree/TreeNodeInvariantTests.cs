using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeNodeInvariantTests
{
    [TestMethod]
    public void Add_SelfReference_Throws()
    {
        var node = new TreeNode("a");

        Assert.ThrowsExactly<InvalidOperationException>(() => node.Add(node));
    }

    [TestMethod]
    public void Add_AncestorAsDescendant_Throws()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        root.Add(child);

        Assert.ThrowsExactly<InvalidOperationException>(() => child.Add(root));
    }

    [TestMethod]
    public void Add_GrandchildToAncestor_Throws()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        Assert.ThrowsExactly<InvalidOperationException>(() => grand.Add(root));
    }

    [TestMethod]
    public void Insert_AncestorAsDescendant_Throws()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        root.Add(child);

        Assert.ThrowsExactly<InvalidOperationException>(() => child.Insert(root, 0));
    }

    [TestMethod]
    public void Constructor_BlankExplicitId_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TreeNode("a", id: "   "));
    }

    [TestMethod]
    public void Constructor_NullId_GeneratesId()
    {
        var node = new TreeNode("a", id: null);

        Assert.IsFalse(string.IsNullOrEmpty(node.Id));
    }

    [TestMethod]
    public void Text_Setter_Blank_Throws()
    {
        var node = new TreeNode("a");

        Assert.ThrowsExactly<ArgumentException>(() => node.Text = " ");
    }

    [TestMethod]
    public void AddChildren_FromLiveChildrenCollection_MovesEveryNode()
    {
        var source = new TreeNode("source");
        var target = new TreeNode("target");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        source.AddChildren(new[] { a, b, c });

        target.AddChildren(source.Children);

        Assert.AreEqual(0, source.Count);
        CollectionAssert.AreEqual(new[] { a, b, c }, target.Children.ToArray());
        Assert.IsTrue(target.Children.All(node => ReferenceEquals(node.Parent, target)));
    }

    [TestMethod]
    public void InsertChildren_FromLiveChildrenCollection_MovesEveryNodeInOrder()
    {
        var source = new TreeNode("source");
        var target = new TreeNode("target");
        var before = new TreeNode("before");
        var after = new TreeNode("after");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        source.AddChildren(new[] { a, b, c });
        target.AddChildren(new[] { before, after });

        target.InsertChildren(source.Children, 1);

        Assert.AreEqual(0, source.Count);
        CollectionAssert.AreEqual(new[] { before, a, b, c, after }, target.Children.ToArray());
        for (var index = 0; index < target.Children.Count; index++)
        {
            Assert.AreSame(target, target.Children[index].Parent);
            Assert.AreEqual(index, target.Children[index].Index);
        }
    }

    [TestMethod]
    public void RemoveChildren_FromLiveChildrenCollection_RemovesEveryNode()
    {
        var parent = new TreeNode("parent");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        parent.AddChildren(new[] { a, b, c });

        parent.RemoveChildren(parent.Children);

        Assert.AreEqual(0, parent.Count);
        Assert.IsTrue(new[] { a, b, c }.All(node => node.Parent is null));
    }

    [TestMethod]
    public void AddChildren_WhenLaterNodeWouldCreateCycle_LeavesAllNodesUnchanged()
    {
        var root = new TreeNode("root");
        var target = new TreeNode("target");
        var movable = new TreeNode("movable");
        root.Add(target);
        root.Add(movable);

        Assert.ThrowsExactly<InvalidOperationException>(() => target.AddChildren(new[] { movable, root }));

        CollectionAssert.AreEqual(new[] { target, movable }, root.Children.ToArray());
        Assert.AreEqual(0, target.Count);
        Assert.AreSame(root, movable.Parent);
    }

    [TestMethod]
    public void InsertChildren_WhenLaterNodeWouldCreateCycle_LeavesAllNodesUnchanged()
    {
        var root = new TreeNode("root");
        var target = new TreeNode("target");
        var movable = new TreeNode("movable");
        root.Add(target);
        root.Add(movable);

        Assert.ThrowsExactly<InvalidOperationException>(() => target.InsertChildren(new[] { movable, root }, 0));

        CollectionAssert.AreEqual(new[] { target, movable }, root.Children.ToArray());
        Assert.AreEqual(0, target.Count);
        Assert.AreSame(root, movable.Parent);
        Assert.AreEqual(1, movable.Index);
    }
}
