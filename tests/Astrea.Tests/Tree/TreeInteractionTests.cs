using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeInteractionDoubleClickTests
{
    [TestMethod]
    public void IsDoubleClick_SameNodeWithinDelay_ReturnsTrue()
    {
        var node = new TreeNode("n");

        Assert.IsTrue(TreeInteraction.IsDoubleClick(node, node, 1000, 1100, 300));
    }

    [TestMethod]
    public void IsDoubleClick_DifferentNode_ReturnsFalse()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");

        Assert.IsFalse(TreeInteraction.IsDoubleClick(a, b, 1000, 1100, 300));
    }

    [TestMethod]
    public void IsDoubleClick_BeyondDelay_ReturnsFalse()
    {
        var node = new TreeNode("n");

        Assert.IsFalse(TreeInteraction.IsDoubleClick(node, node, 1000, 1500, 300));
    }
}

[TestClass]
public class TreeInteractionSelectionActionTests
{
    [TestMethod]
    public void ResolveSelectionAction_ShiftWithAnchor_ReturnsRange()
    {
        Assert.AreEqual(TreeInteraction.SelectionAction.Range,
            TreeInteraction.ResolveSelectionAction(true, shiftKey: true, ctrlKey: false, hasAnchor: true));
    }

    [TestMethod]
    public void ResolveSelectionAction_Ctrl_ReturnsToggle()
    {
        Assert.AreEqual(TreeInteraction.SelectionAction.Toggle,
            TreeInteraction.ResolveSelectionAction(true, shiftKey: false, ctrlKey: true, hasAnchor: false));
    }

    [TestMethod]
    public void ResolveSelectionAction_NoModifier_ReturnsSingle()
    {
        Assert.AreEqual(TreeInteraction.SelectionAction.Single,
            TreeInteraction.ResolveSelectionAction(true, shiftKey: false, ctrlKey: false, hasAnchor: false));
    }

    [TestMethod]
    public void ResolveSelectionAction_ShiftWithoutMultiSelect_ReturnsSingle()
    {
        Assert.AreEqual(TreeInteraction.SelectionAction.Single,
            TreeInteraction.ResolveSelectionAction(false, shiftKey: true, ctrlKey: false, hasAnchor: true));
    }

    [TestMethod]
    public void ResolveSelectionAction_ShiftWithoutAnchor_ReturnsSingle()
    {
        Assert.AreEqual(TreeInteraction.SelectionAction.Single,
            TreeInteraction.ResolveSelectionAction(true, shiftKey: true, ctrlKey: false, hasAnchor: false));
    }
}

[TestClass]
public class DragPositionParseTests
{
    [TestMethod]
    public void Parse_Top_ReturnsTop()
    {
        Assert.AreEqual(DragPosition.Top, DragPosition.Parse("top"));
    }

    [TestMethod]
    public void Parse_Center_ReturnsCenter()
    {
        Assert.AreEqual(DragPosition.Center, DragPosition.Parse("center"));
    }

    [TestMethod]
    public void Parse_Bottom_ReturnsBottom()
    {
        Assert.AreEqual(DragPosition.Bottom, DragPosition.Parse("bottom"));
    }

    [TestMethod]
    public void Parse_Unknown_DefaultsToBottom()
    {
        Assert.AreEqual(DragPosition.Bottom, DragPosition.Parse("garbage"));
    }
}
