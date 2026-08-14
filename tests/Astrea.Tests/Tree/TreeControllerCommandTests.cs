using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeControllerCommandTests
{
    private static (TreeController Controller, TreeNode Owned, TreeNode External) Build()
    {
        var owned = new TreeNode("owned", id: "owned");
        var external = new TreeNode("external", id: "external");
        var controller = new TreeController(new[] { owned });
        return (controller, owned, external);
    }

    [TestMethod]
    public void Select_ExternalNode_ReturnsRejected()
    {
        var (controller, _, external) = Build();

        var result = controller.Select(external);

        Assert.AreEqual(TreeOperationStatus.Rejected, result.Status);
        Assert.IsFalse(controller.SelectedIds.Contains("external"));
    }

    [TestMethod]
    public void Select_DisabledNode_ReturnsRejected()
    {
        var node = new TreeNode("n", id: "n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        var result = controller.Select(node);

        Assert.AreEqual(TreeOperationStatus.Rejected, result.Status);
        Assert.IsFalse(node.IsSelected);
    }

    [TestMethod]
    public void Select_NotSelected_ReturnsChanged()
    {
        var (controller, owned, _) = Build();

        var result = controller.Select(owned);

        Assert.AreEqual(TreeOperationStatus.Changed, result.Status);
    }

    [TestMethod]
    public void Select_AlreadySelected_ReturnsNoOp()
    {
        var (controller, owned, _) = Build();
        controller.Select(owned);

        var result = controller.Select(owned);

        Assert.AreEqual(TreeOperationStatus.NoOp, result.Status);
    }

    [TestMethod]
    public void SetFocus_ExternalNode_ReturnsRejected()
    {
        var (controller, _, external) = Build();

        var result = controller.SetFocus(external);

        Assert.AreEqual(TreeOperationStatus.Rejected, result.Status);
        Assert.IsNull(controller.FocusedNode);
    }

    [TestMethod]
    public void SetFocus_DisabledNode_ReturnsChanged()
    {
        var node = new TreeNode("n", id: "n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        var result = controller.SetFocus(node);

        Assert.AreEqual(TreeOperationStatus.Changed, result.Status);
        Assert.IsTrue(node.IsFocused);
    }

    [TestMethod]
    public void ReplaceSelection_WithExternalNode_RejectsAtomically()
    {
        var (controller, owned, external) = Build();
        controller.AddToSelection(owned);

        var result = controller.ReplaceSelection(new[] { owned, external });

        Assert.AreEqual(TreeOperationStatus.Rejected, result.Status);
        // 原子：拒绝时选择集合保持原状（仍含 owned）
        Assert.IsTrue(controller.SelectedIds.Contains("owned"));
        Assert.IsFalse(controller.SelectedIds.Contains("external"));
    }

    [TestMethod]
    public void Refresh_IncrementsRevision()
    {
        var root = new TreeNode("root", id: "root");
        var controller = new TreeController(new[] { root });
        long before = controller.Revision;

        controller.Refresh();

        Assert.AreEqual(before + 1, controller.Revision);
    }

    [TestMethod]
    public void BeforeExpandGuard_Replacement_TakesEffect()
    {
        var root = new TreeNode("root");
        root.Add(new TreeNode("child"));
        var controller = new TreeController(new[] { root }) { BeforeExpand = _ => false };

        Assert.IsFalse(controller.Expand(root));

        controller.BeforeExpand = null;

        Assert.IsTrue(controller.Expand(root));
    }

    [TestMethod]
    public void SelectModeConvergence_WhenMultipleToSingle_KeepsFocused()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);
        var controller = new TreeController(new[] { root });
        controller.AddToSelection(a);
        controller.AddToSelection(b);
        controller.SetFocus(a);

        var result = controller.ConvergeToSingleSelection();

        Assert.AreEqual(TreeOperationStatus.Changed, result.Status);
        Assert.IsTrue(a.IsSelected);
        Assert.IsFalse(b.IsSelected);
    }
}
