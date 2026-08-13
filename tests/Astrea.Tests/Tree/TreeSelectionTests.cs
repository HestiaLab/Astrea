using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeSelectionTests
{
    [TestMethod]
    public void Select_SetsSingleNodeSelected()
    {
        var node = new TreeNode("a");
        var selection = new TreeSelection();

        selection.Select(node);

        Assert.IsTrue(node.IsSelected);
        Assert.IsTrue(selection.IsSelected(node));
        Assert.AreEqual(1, selection.SelectedNodes.Count);
    }

    [TestMethod]
    public void Select_ReplacesPreviousSelection()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var selection = new TreeSelection();
        selection.Select(a);

        selection.Select(b);

        Assert.IsFalse(a.IsSelected);
        Assert.IsTrue(b.IsSelected);
        Assert.AreEqual(1, selection.SelectedNodes.Count);
    }

    [TestMethod]
    public void AddToSelection_Appends()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var selection = new TreeSelection();
        selection.Select(a);

        selection.AddToSelection(b);

        Assert.IsTrue(a.IsSelected);
        Assert.IsTrue(b.IsSelected);
        Assert.AreEqual(2, selection.SelectedNodes.Count);
    }

    [TestMethod]
    public void ToggleSelection_FlipsState()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var selection = new TreeSelection();
        selection.Select(a);
        selection.AddToSelection(b);

        selection.ToggleSelection(b);

        Assert.IsTrue(a.IsSelected);
        Assert.IsFalse(b.IsSelected);
        Assert.AreEqual(1, selection.SelectedNodes.Count);

        selection.ToggleSelection(b);
        Assert.IsTrue(b.IsSelected);
        Assert.AreEqual(2, selection.SelectedNodes.Count);
    }

    [TestMethod]
    public void ClearSelection_ResetsNodesAndCache()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var selection = new TreeSelection();
        selection.Select(a);
        selection.AddToSelection(b);

        selection.ClearSelection();

        Assert.IsFalse(a.IsSelected);
        Assert.IsFalse(b.IsSelected);
        Assert.AreEqual(0, selection.SelectedNodes.Count);
    }

    [TestMethod]
    public void SetFocus_MaintainsSingleFocusedInvariant()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var selection = new TreeSelection();

        selection.SetFocus(a);

        Assert.IsTrue(a.IsFocused);
        Assert.AreSame(a, selection.FocusedNode);

        selection.SetFocus(b);

        Assert.IsFalse(a.IsFocused);
        Assert.IsTrue(b.IsFocused);
        Assert.AreSame(b, selection.FocusedNode);
    }

    [TestMethod]
    public void ClearFocus_ResetsFocusedNode()
    {
        var a = new TreeNode("a");
        var selection = new TreeSelection();
        selection.SetFocus(a);

        selection.ClearFocus();

        Assert.IsFalse(a.IsFocused);
        Assert.IsNull(selection.FocusedNode);
    }

    [TestMethod]
    public void SelectionCache_StaysConsistentWithNodeState()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        var selection = new TreeSelection();

        selection.Select(a);
        selection.AddToSelection(b);
        selection.ToggleSelection(c);
        selection.ToggleSelection(b);

        foreach (var node in selection.SelectedNodes)
        {
            Assert.IsTrue(node.IsSelected);
        }

        Assert.AreEqual(2, selection.SelectedNodes.Count);
        Assert.IsTrue(a.IsSelected);
        Assert.IsFalse(b.IsSelected);
        Assert.IsTrue(c.IsSelected);
    }
}
