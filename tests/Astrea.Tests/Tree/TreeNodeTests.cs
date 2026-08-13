using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeNodeStructureTests
{
    [TestMethod]
    public void Constructor_WithoutId_GeneratesUniqueId()
    {
        var a = new TreeNode("A");
        var b = new TreeNode("B");

        Assert.IsFalse(string.IsNullOrEmpty(a.Id));
        Assert.IsFalse(string.IsNullOrEmpty(b.Id));
        Assert.AreNotEqual(a.Id, b.Id);
    }

    [TestMethod]
    public void Constructor_WithId_PreservesProvidedId()
    {
        var node = new TreeNode("A", id: "stable-1");

        Assert.AreEqual("stable-1", node.Id);
    }

    [TestMethod]
    public void Add_SetsParentAndIndex()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");

        root.Add(child);

        Assert.AreSame(root, child.Parent);
        Assert.AreEqual(0, child.Index);
        Assert.IsTrue(root.HasChildren);
        Assert.IsFalse(root.IsLeaf);
    }

    [TestMethod]
    public void Add_MultipleChildren_AssignsSequentialIndex()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");

        root.Add(a);
        root.Add(b);
        root.Add(c);

        Assert.AreEqual(0, a.Index);
        Assert.AreEqual(1, b.Index);
        Assert.AreEqual(2, c.Index);
        Assert.AreEqual(3, root.Count);
    }

    [TestMethod]
    public void Add_DuplicateChild_IsIgnored()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");

        root.Add(child);
        root.Add(child);

        Assert.AreEqual(1, root.Count);
    }

    [TestMethod]
    public void Remove_ReparentsAndReindexes()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        root.Add(a);
        root.Add(b);
        root.Add(c);

        root.Remove(b);

        Assert.IsNull(b.Parent);
        Assert.AreEqual(2, root.Count);
        Assert.AreEqual(0, a.Index);
        Assert.AreEqual(1, c.Index);
    }

    [TestMethod]
    public void Insert_AtMiddle_ReindexesFollowers()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        var c = new TreeNode("c");
        root.Add(a);
        root.Add(c);

        var b = new TreeNode("b");
        root.Insert(b, 1);

        Assert.AreEqual(0, a.Index);
        Assert.AreEqual(1, b.Index);
        Assert.AreEqual(2, c.Index);
    }

    [TestMethod]
    public void Level_IsDerivedFromParentChain()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        Assert.AreEqual(0, root.Level);
        Assert.AreEqual(1, child.Level);
        Assert.AreEqual(2, grand.Level);
    }

    [TestMethod]
    public void Root_ReturnsTopmostAncestor()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        Assert.AreSame(root, grand.Root);
        Assert.AreSame(root, child.Root);
        Assert.AreSame(root, root.Root);
    }

    [TestMethod]
    public void Siblings_ReturnsAllChildrenOfParent()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);

        var siblings = a.Siblings().ToList();

        Assert.AreEqual(2, siblings.Count);
        CollectionAssert.Contains(siblings, a);
        CollectionAssert.Contains(siblings, b);
    }

    [TestMethod]
    public void GetPathToRoot_ReturnsAncestorsExcludingSelf()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        var path = grand.GetPathToRoot().ToList();

        Assert.AreEqual(2, path.Count);
        Assert.AreSame(child, path[0]);
        Assert.AreSame(root, path[1]);
    }

    [TestMethod]
    public void IsDescendantOf_ReturnsTrueForAncestor()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        Assert.IsTrue(grand.IsDescendantOf(root));
        Assert.IsTrue(child.IsDescendantOf(root));
        Assert.IsFalse(root.IsDescendantOf(child));
        Assert.IsFalse(grand.IsDescendantOf(grand));
    }

    [TestMethod]
    public void IsAncestorOf_ReturnsTrueForDescendant()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);

        Assert.IsTrue(root.IsAncestorOf(grand));
        Assert.IsTrue(root.IsAncestorOf(child));
        Assert.IsFalse(child.IsAncestorOf(root));
    }
}

[TestClass]
public class TreeNodeIconAndCssTests
{
    [TestMethod]
    public void FoldIcon_Leaf_ReturnsNodeIcon()
    {
        var node = new TreeNode("leaf", icon: "icon-a");

        Assert.AreEqual("icon-a", node.FoldIcon);
    }

    [TestMethod]
    public void FoldIcon_ExpandedWithUnFoldIcon_ReturnsUnFoldIcon()
    {
        var node = new TreeNode("folder", icon: "closed");
        node.UnFoldIcon = "open";
        node.Add(new TreeNode("child"));
        node.IsExpanded = true;

        Assert.AreEqual("open", node.FoldIcon);
    }

    [TestMethod]
    public void FoldIcon_Collapsed_ReturnsNodeIcon()
    {
        var node = new TreeNode("folder", icon: "closed");
        node.UnFoldIcon = "open";
        node.Add(new TreeNode("child"));
        node.IsExpanded = false;

        Assert.AreEqual("closed", node.FoldIcon);
    }

    [TestMethod]
    public void ExpandIcon_Leaf_ReturnsNull()
    {
        var node = new TreeNode("leaf");
        node.ExpandedIcon = "exp";
        node.CollapsedIcon = "col";

        Assert.IsNull(node.ExpandIcon);
    }

    [TestMethod]
    public void ExpandIcon_TogglesByExpandedState()
    {
        var node = new TreeNode("folder");
        node.ExpandedIcon = "exp";
        node.CollapsedIcon = "col";
        node.Add(new TreeNode("child"));

        node.IsExpanded = false;
        Assert.AreEqual("col", node.ExpandIcon);

        node.IsExpanded = true;
        Assert.AreEqual("exp", node.ExpandIcon);
    }

    [TestMethod]
    public void CssClass_BaseOnly_WhenNoResolver()
    {
        var node = new TreeNode("a");
        node.SetCssClass("bold");

        Assert.AreEqual("bold", node.CssClass);
    }

    [TestMethod]
    public void CssClass_AppendsDynamicSegment()
    {
        var node = new TreeNode("a");
        node.SetCssClass("bold");
        node.CssClassResolver = _ => "ua-connected-text";

        Assert.AreEqual("bold ua-connected-text", node.CssClass);
    }

    [TestMethod]
    public void CssClass_EmptyBase_ReturnsOnlyDynamic()
    {
        var node = new TreeNode("a");
        node.CssClassResolver = _ => "ua-connected-text";

        Assert.AreEqual("ua-connected-text", node.CssClass);
    }
}

[TestClass]
public class TreeNodeCloneTests
{
    [TestMethod]
    public void Clone_ProducesIndependentCopyWithFreshId()
    {
        var root = new TreeNode("root", icon: "icon", bindData: "data", id: "id-root");
        var child = new TreeNode("child");
        root.Add(child);
        root.IsExpanded = true;

        var clone = (TreeNode)root.Clone();

        Assert.AreNotSame(root, clone);
        Assert.AreEqual("root", clone.Text);
        Assert.AreEqual("icon", clone.FoldIcon);
        Assert.AreEqual("data", clone.BindData);
        Assert.IsTrue(clone.IsExpanded);
        Assert.AreEqual(1, clone.Count);
        Assert.AreEqual("child", clone.Children[0].Text);
        Assert.AreNotSame(child, clone.Children[0]);
    }

    [TestMethod]
    public void Clone_ShallowCopiesBindDataReference()
    {
        var data = new object();
        var root = new TreeNode("root", bindData: data);

        var clone = (TreeNode)root.Clone();

        Assert.AreSame(data, clone.BindData);
    }
}
