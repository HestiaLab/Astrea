using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeControllerIndexTests
{
    [TestMethod]
    public void FindById_ReturnsNode()
    {
        var root = new TreeNode("root", id: "r1");
        var child = new TreeNode("child", id: "c1");
        root.Add(child);
        var controller = new TreeController(new[] { root });

        Assert.AreSame(root, controller.FindById("r1"));
        Assert.AreSame(child, controller.FindById("c1"));
        Assert.IsNull(controller.FindById("missing"));
    }

    [TestMethod]
    public void FindByBindData_ReturnsNodeByReference()
    {
        var data = new object();
        var root = new TreeNode("root", bindData: data);
        var controller = new TreeController(new[] { root });

        Assert.AreSame(root, controller.FindByBindData(data));
        Assert.IsNull(controller.FindByBindData(new object()));
    }
}

[TestClass]
public class TreeControllerFlattenTests
{
    [TestMethod]
    public void Flatten_ReturnsAllNodesInOrder()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);
        a.Add(new TreeNode("a1"));
        var controller = new TreeController(new[] { root });

        var flat = controller.Flatten();

        Assert.AreEqual(4, flat.Count);
        Assert.AreSame(root, flat[0]);
        Assert.AreSame(a, flat[1]);
        Assert.AreSame(a.Children[0], flat[2]);
        Assert.AreSame(b, flat[3]);
    }

    [TestMethod]
    public void FlattenVisible_OnlyExpandedPaths()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);
        a.Add(new TreeNode("a1"));
        root.IsExpanded = true;
        a.IsExpanded = false;
        var controller = new TreeController(new[] { root });

        var visible = controller.FlattenVisible();

        Assert.AreEqual(3, visible.Count);
        Assert.AreSame(root, visible[0]);
        Assert.AreSame(a, visible[1]);
        Assert.AreSame(b, visible[2]);
    }
}

[TestClass]
public class TreeControllerExpandTests
{
    [TestMethod]
    public void Expand_LeafReturnsFalse()
    {
        var leaf = new TreeNode("leaf");
        var controller = new TreeController(new[] { leaf });

        Assert.IsFalse(controller.Expand(leaf));
    }

    [TestMethod]
    public void ExpandAllParents_ExpandsAncestors()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        var grand = new TreeNode("grand");
        root.Add(child);
        child.Add(grand);
        var controller = new TreeController(new[] { root });

        controller.ExpandAllParents(grand);

        Assert.IsTrue(child.IsExpanded);
        Assert.IsTrue(root.IsExpanded);
    }
}

[TestClass]
public class TreeControllerSelectionTests
{
    [TestMethod]
    public void GetSelectedNodes_IncludesCollapsedSubtreeSelection()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        root.Add(a);
        var a1 = new TreeNode("a1");
        a.Add(a1);
        a.IsExpanded = true;
        var controller = new TreeController(new[] { root });

        controller.Select(a1);
        a.IsExpanded = false; // 折叠后仍应返回选中（修复旧版漏选）

        var selected = controller.GetSelectedNodes();

        Assert.AreEqual(1, selected.Count);
        Assert.AreSame(a1, selected.First());
    }

    [TestMethod]
    public void SelectRange_SelectsNodesBetween()
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
        controller.SelectRange(a, c);

        Assert.AreEqual(3, controller.GetSelectedNodes().Count);
        Assert.IsTrue(a.IsSelected);
        Assert.IsTrue(b.IsSelected);
        Assert.IsTrue(c.IsSelected);
    }

    [TestMethod]
    public void ClearSelectionSubtree_OnlyClearsSubtree()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);
        var a1 = new TreeNode("a1");
        a.Add(a1);
        a.IsExpanded = true;
        var controller = new TreeController(new[] { root });

        controller.Select(a1);
        controller.AddToSelection(b);

        controller.ClearSelectionSubtree(a);

        Assert.IsFalse(a1.IsSelected);
        Assert.IsTrue(b.IsSelected);
    }

    [TestMethod]
    public void NodeFacade_SetFocus_IsNotPublicMutationPath()
    {
        Assert.IsNull(typeof(TreeNode).GetMethod("SetFocus", Type.EmptyTypes));
    }

    [TestMethod]
    public void NodeFacade_FindNode_DelegatesToController()
    {
        var root = new TreeNode("root", id: "r1");
        var child = new TreeNode("child", id: "c1");
        root.Add(child);
        var controller = new TreeController(new[] { root });

        Assert.AreSame(child, root.FindNode("c1"));
    }
}

[TestClass]
public class TreeControllerDropTests
{
    private static TreeController Build(params TreeNode[] roots) => new(roots);

    [TestMethod]
    public void CanDrop_RejectsSelfReference()
    {
        var root = new TreeNode("root");
        var child = new TreeNode("child");
        root.Add(child);
        var controller = Build(root);

        Assert.IsFalse(controller.CanDrop(new[] { child }, child));
    }

    [TestMethod]
    public void CanDrop_AllowsCrossRootWithinSameTree()
    {
        var r1 = new TreeNode("r1");
        var r2 = new TreeNode("r2");
        var c1 = new TreeNode("c1");
        r1.Add(c1);
        var c2 = new TreeNode("c2");
        r2.Add(c2);
        var controller = Build(r1, r2);

        // 同一 Tree 实例的多个根属于同一交互边界，允许跨根拖拽（REQ-F-087）
        Assert.IsTrue(controller.CanDrop(new[] { c1 }, c2));
    }

    [TestMethod]
    public void CanDrop_RejectsCycle()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        root.Add(a);
        var b = new TreeNode("b");
        a.Add(b);
        var controller = Build(root);

        Assert.IsFalse(controller.CanDrop(new[] { root }, b));
    }

    [TestMethod]
    public void CanDrop_ValidMoveAllowed()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        root.Add(a);
        var b = new TreeNode("b");
        root.Add(b);
        var controller = Build(root);

        Assert.IsTrue(controller.CanDrop(new[] { a }, b));
    }
}

[TestClass]
public class TreeControllerRangeTests
{
    [TestMethod]
    public void SelectRange_AcrossRoots_UsesControllerVisibleOrder()
    {
        var firstRoot = new TreeNode("first-root", id: "r1") { IsExpanded = true };
        var firstChild = new TreeNode("first-child", id: "a");
        firstRoot.Add(firstChild);
        var secondRoot = new TreeNode("second-root", id: "r2") { IsExpanded = true };
        var secondChild = new TreeNode("second-child", id: "b");
        secondRoot.Add(secondChild);
        var controller = new TreeController(new[] { firstRoot, secondRoot });

        var result = controller.SelectRange(firstChild, secondChild);

        Assert.AreEqual(TreeOperationStatus.Changed, result.Status);
        CollectionAssert.AreEqual(
            new[] { firstChild, secondRoot, secondChild },
            controller.GetSelectedNodes().ToArray());
    }

    [TestMethod]
    public void SelectRange_DisabledEndpoint_RejectsAtomically()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b") { IsDisabled = true };
        root.AddChildren(new[] { a, b });
        var controller = new TreeController(new[] { root });
        controller.Select(a);

        var result = controller.SelectRange(a, b);

        Assert.AreEqual(TreeOperationStatus.Rejected, result.Status);
        CollectionAssert.AreEqual(new[] { a }, controller.GetSelectedNodes().ToArray());
    }

    [TestMethod]
    public void SelectRange_DisabledIntermediateNode_SkipsDisabledNode()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var disabled = new TreeNode("disabled") { IsDisabled = true };
        var c = new TreeNode("c");
        root.AddChildren(new[] { a, disabled, c });
        var controller = new TreeController(new[] { root });

        var result = controller.SelectRange(a, c);

        Assert.AreEqual(TreeOperationStatus.Changed, result.Status);
        CollectionAssert.AreEqual(new[] { a, c }, controller.GetSelectedNodes().ToArray());
        Assert.IsFalse(disabled.IsSelected);
    }

    [TestMethod]
    public void GetNodesBetween_ReturnsRangeInOrder()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        var c = new TreeNode("c");
        root.Add(a);
        root.Add(b);
        root.Add(c);

        bool ok = TreeController.GetNodesBetween(a, c, out var range);

        Assert.IsTrue(ok);
        Assert.AreEqual(3, range.Count);
        Assert.AreSame(a, range[0]);
        Assert.AreSame(b, range[1]);
        Assert.AreSame(c, range[2]);
    }

    [TestMethod]
    public void GetNodesBetween_SkipsCollapsedNodes()
    {
        var root = new TreeNode("root") { IsExpanded = true };
        var a = new TreeNode("a");
        var b = new TreeNode("b");
        root.Add(a);
        root.Add(b);
        var a1 = new TreeNode("a1");
        a.Add(a1);
        a.IsExpanded = false; // a1 折叠

        bool ok = TreeController.GetNodesBetween(a, b, out var range);

        Assert.IsTrue(ok);
        // a, b（a1 因父节点折叠被跳过）
        Assert.AreEqual(2, range.Count);
        Assert.AreSame(a, range[0]);
        Assert.AreSame(b, range[1]);
    }
}
