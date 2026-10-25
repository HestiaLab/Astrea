using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeControllerDisabledNodeTests
{
    [TestMethod]
    public void Select_DisabledNode_Ignored()
    {
        var node = new TreeNode("n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        controller.Select(node);

        Assert.IsFalse(node.IsSelected);
    }

    [TestMethod]
    public void AddToSelection_DisabledNode_Ignored()
    {
        var node = new TreeNode("n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        controller.AddToSelection(node);

        Assert.IsFalse(node.IsSelected);
    }

    [TestMethod]
    public void ToggleSelection_DisabledNode_Ignored()
    {
        var node = new TreeNode("n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        controller.ToggleSelection(node);

        Assert.IsFalse(node.IsSelected);
    }

    [TestMethod]
    public void Expand_DisabledNode_ReturnsFalse()
    {
        var node = new TreeNode("n") { IsDisabled = true };
        node.Add(new TreeNode("child"));
        var controller = new TreeController(new[] { node });

        bool result = controller.Expand(node);

        Assert.IsFalse(result);
        Assert.IsFalse(node.IsExpanded);
    }

    [TestMethod]
    public void Collapse_DisabledNode_ReturnsFalse()
    {
        var node = new TreeNode("n") { IsDisabled = true, IsExpanded = true };
        node.Add(new TreeNode("child"));
        var controller = new TreeController(new[] { node });

        bool result = controller.Collapse(node);

        Assert.IsFalse(result);
        Assert.IsTrue(node.IsExpanded);
    }

    [TestMethod]
    public void Expand_ExpandDisabledNode_ReturnsFalse()
    {
        var node = new TreeNode("n") { IsExpandDisable = true };
        node.Add(new TreeNode("child"));
        var controller = new TreeController(new[] { node });

        Assert.IsFalse(controller.Expand(node));
        Assert.IsFalse(node.IsExpanded);
    }

    [TestMethod]
    public void Collapse_ExpandDisabledNode_ReturnsFalse()
    {
        var node = new TreeNode("n") { IsExpandDisable = true, IsExpanded = true };
        node.Add(new TreeNode("child"));
        var controller = new TreeController(new[] { node });

        Assert.IsFalse(controller.Collapse(node));
        Assert.IsTrue(node.IsExpanded);
    }

    [TestMethod]
    public void BeginEdit_DisabledNode_ReturnsFalse()
    {
        var node = new TreeNode("n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        Assert.IsFalse(controller.BeginEdit(node));
        Assert.IsFalse(node.IsEditing);
    }

    [TestMethod]
    public void TryRename_DisabledNode_ReturnsFalse()
    {
        var node = new TreeNode("n") { IsDisabled = true };
        var controller = new TreeController(new[] { node });

        Assert.IsFalse(controller.TryRename(node, "new"));
    }

    [TestMethod]
    public void CanDrop_DisabledSource_ReturnsFalse()
    {
        var source = new TreeNode("source", id: "source") { IsDisabled = true };
        var target = new TreeNode("target", id: "target");
        var controller = new TreeController(new[] { source, target });

        Assert.IsFalse(controller.CanDrop(new[] { source }, target));
    }

    [TestMethod]
    public void CanDrop_DisabledTarget_ReturnsFalse()
    {
        var source = new TreeNode("source", id: "source");
        var target = new TreeNode("target", id: "target") { IsDisabled = true };
        var controller = new TreeController(new[] { source, target });

        Assert.IsFalse(controller.CanDrop(new[] { source }, target));
    }
}

[TestClass]
public class TreeControllerEditVetoTests
{
    [TestMethod]
    public void TryRename_NoVeto_ReturnsTrue()
    {
        var node = new TreeNode("n");
        var controller = new TreeController(new[] { node });

        Assert.IsTrue(controller.TryRename(node, "new"));
    }

    [TestMethod]
    public void BeginEdit_NoVeto_SetsEditing()
    {
        var node = new TreeNode("n");
        var controller = new TreeController(new[] { node });

        Assert.IsTrue(controller.BeginEdit(node));
        Assert.IsTrue(node.IsEditing);
    }

    [TestMethod]
    public void EndEdit_ClearsEditing()
    {
        var node = new TreeNode("n");
        var controller = new TreeController(new[] { node });
        controller.BeginEdit(node);

        controller.EndEdit(node);

        Assert.IsFalse(node.IsEditing);
    }
}

[TestClass]
public class TreeControllerConvergeSelectionTests
{
    [TestMethod]
    public void ConvergeToSingleSelection_KeepsFocusedWhenSelected()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);
        var controller = new TreeController(new[] { root });

        controller.Select(a);
        controller.AddToSelection(b);
        controller.SetFocus(a);

        controller.ConvergeToSingleSelection();

        Assert.AreEqual(1, controller.GetSelectedNodes().Count);
        Assert.IsTrue(a.IsSelected);
        Assert.IsFalse(b.IsSelected);
    }

    [TestMethod]
    public void ConvergeToSingleSelection_ClearsWhenFocusedNotSelected()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        root.Add(a);
        root.Add(b);
        root.Add(c);
        var controller = new TreeController(new[] { root });

        controller.Select(a);
        controller.AddToSelection(b);
        controller.SetFocus(c);

        controller.ConvergeToSingleSelection();

        Assert.AreEqual(0, controller.GetSelectedNodes().Count);
    }

    [TestMethod]
    public void ConvergeToSingleSelection_NoOpWhenSingleSelected()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        root.Add(a);
        var controller = new TreeController(new[] { root });

        controller.Select(a);
        controller.ConvergeToSingleSelection();

        Assert.AreEqual(1, controller.GetSelectedNodes().Count);
        Assert.IsTrue(a.IsSelected);
    }
}

[TestClass]
public class TreeControllerFocusRestoreTests
{
    [TestMethod]
    public void Collapse_FocusInDescendant_MovesFocusToCollapsedNode()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a") { IsExpanded = true };
        root.Add(a);
        var b = new TreeNode("b");
        a.Add(b);
        var controller = new TreeController(new[] { root });

        controller.SetFocus(b);
        controller.Collapse(a);

        Assert.IsTrue(a.IsFocused);
        Assert.IsFalse(b.IsFocused);
    }

    [TestMethod]
    public void Refresh_RemovedFocusNode_RestoresToNextSibling()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a", id: "a");
        var b = new TreeNode("b", id: "b");
        root.Add(a);
        root.Add(b);
        var controller = new TreeController(new[] { root });

        controller.SetFocus(a);
        root.Remove(a);
        controller.Refresh();

        Assert.IsTrue(b.IsFocused);
    }

    [TestMethod]
    public void Refresh_RemovedFocusNode_LastChild_RestoresToPreviousSibling()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a", id: "a");
        var b = new TreeNode("b", id: "b");
        root.Add(a);
        root.Add(b);
        var controller = new TreeController(new[] { root });

        controller.SetFocus(b);
        root.Remove(b);
        controller.Refresh();

        Assert.IsTrue(a.IsFocused);
    }

    [TestMethod]
    public void Refresh_RemovedFocusNode_TreeEmpty_ClearsFocus()
    {
        var roots = new List<TreeNode> { new TreeNode("root", id: "r") };
        var controller = new TreeController(roots);

        controller.SetFocus(roots[0]);
        roots.RemoveAt(0);
        controller.Refresh();

        Assert.IsNull(controller.FocusedNode);
    }

    [TestMethod]
    public void Refresh_RemovedSelectedNode_ClearsSelection()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a", id: "a");
        root.Add(a);
        var controller = new TreeController(new[] { root });

        controller.Select(a);
        root.Remove(a);
        controller.Refresh();

        Assert.AreEqual(0, controller.GetSelectedNodes().Count);
        Assert.IsFalse(a.IsSelected);
    }

    [TestMethod]
    public void Refresh_FocusHiddenByExternalCollapse_RestoresToVisibleTarget()
    {
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        var a = new TreeNode("a", id: "a") { IsExpanded = true };
        root.Add(a);
        var a1 = new TreeNode("a1", id: "a1");
        a.Add(a1);
        var controller = new TreeController(new[] { root });
        controller.SetFocus(a1);

        // 外部折叠含焦点的分支（不经过 Controller.Collapse），再 Refresh
        a.IsExpanded = false;
        controller.Refresh();

        // 焦点不得停留在隐藏节点 a1 上，应恢复到可见目标
        Assert.AreNotSame(a1, controller.FocusedNode);
        Assert.AreSame(root, controller.FocusedNode);
    }
}

[TestClass]
public class TreeControllerStateRestoreTests
{
    [TestMethod]
    public void RestoreState_RestoresExpandedSelectionAndFocusById()
    {
        var oldRoot = new TreeNode("root", id: "r") { IsExpanded = true };
        var oldA = new TreeNode("a", id: "a") { IsExpanded = true };
        oldRoot.Add(oldA);
        oldA.Add(new TreeNode("a1", id: "a1"));
        var oldController = new TreeController(new[] { oldRoot });
        oldController.Select(oldA);
        oldController.SetFocus(oldA);

        var selectedIds = oldController.GetSelectedNodes().Select(n => n.Id).ToHashSet();
        var expandedIds = oldController.Flatten().Where(n => n.IsExpanded).Select(n => n.Id).ToHashSet();
        var focusId = oldController.FocusedNode!.Id;

        var newRoot = new TreeNode("root", id: "r");
        var newA = new TreeNode("a", id: "a");
        newRoot.Add(newA);
        newA.Add(new TreeNode("a1", id: "a1"));
        var newController = new TreeController(new[] { newRoot });

        newController.RestoreState(selectedIds, expandedIds, focusId);

        Assert.IsTrue(newRoot.IsExpanded);
        Assert.IsTrue(newA.IsExpanded);
        Assert.IsTrue(newA.IsSelected);
        Assert.IsTrue(newA.IsFocused);
    }

    [TestMethod]
    public void RestoreState_MissingIds_AreIgnored()
    {
        var root = new TreeNode("root", id: "r");
        root.Add(new TreeNode("a", id: "a"));
        var controller = new TreeController(new[] { root });

        controller.RestoreState(new[] { "missing" }, new[] { "missing" }, "missing");

        Assert.AreEqual(0, controller.GetSelectedNodes().Count);
        Assert.IsNull(controller.FocusedNode);
    }
}
