using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeControllerCommandTests
{
    [TestMethod]
    public void SingleMode_AllPublicSelectionCommandsPreserveAtMostOneSelection()
    {
        var root = new TreeNode("root", id: "root");
        var first = new TreeNode("first", id: "first");
        var second = new TreeNode("second", id: "second");
        root.AddChildren(new[] { first, second });
        root.IsExpanded = true;
        var controller = new TreeController(new[] { root }, TreeSelectionMode.Single);

        Assert.IsTrue(controller.AddToSelection(first).Changed);
        Assert.IsTrue(controller.AddToSelection(second).Changed);
        CollectionAssert.AreEqual(new[] { second.Id }, controller.SelectedIds.ToArray());
        Assert.IsTrue(controller.ReplaceSelection(new[] { first, second }).Rejected);
        Assert.IsTrue(controller.SelectRange(first, second).Rejected);
        Assert.IsTrue(controller.SelectAll(root, excludeSelf: true).Rejected);
        Assert.AreEqual(1, controller.SelectedIds.Count);
    }

    [TestMethod]
    public void Flatten_ReturnsReadOnlyViewThatCannotMutateTheCache()
    {
        var root = new TreeNode("root", id: "root");
        root.Add(new TreeNode("child", id: "child"));
        var controller = new TreeController(new[] { root });

        var flatten = controller.Flatten();

        Assert.IsInstanceOfType(flatten, typeof(ReadOnlyCollection<TreeNode>));
        Assert.ThrowsExactly<NotSupportedException>(
            () => ((ICollection<TreeNode>)flatten).Add(new TreeNode("injected", id: "injected")));
        Assert.AreEqual(2, controller.Flatten().Count);
        Assert.IsNull(controller.FindById("injected"));
    }

    [TestMethod]
    public void FlattenVisible_InvalidationReturnsAnotherReadOnlyView()
    {
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        root.Add(new TreeNode("child", id: "child"));
        var controller = new TreeController(new[] { root });

        var first = controller.FlattenVisible();
        controller.InvalidateVisible();
        var second = controller.FlattenVisible();

        Assert.IsInstanceOfType(first, typeof(ReadOnlyCollection<TreeNode>));
        Assert.IsInstanceOfType(second, typeof(ReadOnlyCollection<TreeNode>));
        Assert.ThrowsExactly<NotSupportedException>(
            () => ((ICollection<TreeNode>)second).Add(new TreeNode("injected", id: "injected")));
        Assert.AreEqual(2, controller.FlattenVisible().Count);
    }

    private static (TreeController Controller, TreeNode Owned, TreeNode External) Build()
    {
        var owned = new TreeNode("owned", id: "owned");
        var external = new TreeNode("external", id: "external");
        var controller = new TreeController(new[] { owned });
        return (controller, owned, external);
    }

    [TestMethod]
    public void SelectedIds_MutableCastCannotBypassControllerValidation()
    {
        var (controller, owned, _) = Build();
        controller.Select(owned);

        if (controller.SelectedIds is ISet<string> mutable)
        {
            mutable.Add("injected");
        }

        Assert.IsFalse(controller.SelectedIds.Contains("injected"));
        CollectionAssert.AreEqual(new[] { "owned" }, controller.SelectedIds.ToArray());
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
