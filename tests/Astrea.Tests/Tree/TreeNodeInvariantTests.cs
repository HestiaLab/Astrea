using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeNodeInvariantTests
{
    [TestMethod]
    public void Add_SelfReference_Throws()
    {
        var node = new TreeNode("a");

        Assert.ThrowsException<InvalidOperationException>(() => node.Add(node));
    }

    [TestMethod]
    public void Add_AncestorAsDescendant_Throws()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        root.Add(child);

        Assert.ThrowsException<InvalidOperationException>(() => child.Add(root));
    }

    [TestMethod]
    public void Add_GrandchildToAncestor_Throws()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        Assert.ThrowsException<InvalidOperationException>(() => grand.Add(root));
    }

    [TestMethod]
    public void Insert_AncestorAsDescendant_Throws()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        root.Add(child);

        Assert.ThrowsException<InvalidOperationException>(() => child.Insert(root, 0));
    }

    [TestMethod]
    public void Constructor_BlankExplicitId_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() => new TreeNode("a", id: "   "));
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

        Assert.ThrowsException<ArgumentException>(() => node.Text = " ");
    }
}
