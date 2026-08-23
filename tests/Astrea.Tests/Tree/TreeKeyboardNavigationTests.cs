using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeKeyboardNavigationTests
{
    private static TreeController Build(out TreeNode root, out TreeNode a, out TreeNode b)
    {
        root = new TreeNode("root") { IsExpanded = true };
        a = new TreeNode("a") { IsExpanded = true };
        b = new TreeNode("b");
        root.Add(a);
        a.Add(new TreeNode("a1"));
        root.Add(b);
        return new TreeController(new[] { root });
    }

    [TestMethod]
    public void ArrowDown_NoFocus_FocusesFirstVisible()
    {
        var controller = Build(out var root, out _, out _);

        var result = TreeKeyboardNavigation.Navigate(controller, "ArrowDown");

        Assert.IsTrue(result.Changed);
        Assert.AreSame(root, controller.FocusedNode);
    }

    [TestMethod]
    public void ArrowDown_MovesToNextVisible()
    {
        var controller = Build(out var root, out var a, out _);
        controller.SetFocus(root);

        var result = TreeKeyboardNavigation.Navigate(controller, "ArrowDown");

        Assert.IsTrue(result.Changed);
        Assert.AreSame(a, controller.FocusedNode);
    }

    [TestMethod]
    public void ArrowUp_MovesToPreviousVisible()
    {
        var controller = Build(out var root, out var a, out _);
        controller.SetFocus(a);

        TreeKeyboardNavigation.Navigate(controller, "ArrowUp");

        Assert.AreSame(root, controller.FocusedNode);
    }

    [TestMethod]
    public void Home_FocusesFirstVisible()
    {
        var controller = Build(out var root, out _, out var b);
        controller.SetFocus(b);

        TreeKeyboardNavigation.Navigate(controller, "Home");

        Assert.AreSame(root, controller.FocusedNode);
    }

    [TestMethod]
    public void End_FocusesLastVisible()
    {
        var controller = Build(out _, out _, out var b);

        TreeKeyboardNavigation.Navigate(controller, "End");

        Assert.AreSame(b, controller.FocusedNode);
    }

    [TestMethod]
    public void ArrowRight_ExpandsCollapsedNode()
    {
        var root = new TreeNode("root");
        root.Add(new TreeNode("child"));
        var controller = new TreeController(new[] { root });
        controller.SetFocus(root);

        var result = TreeKeyboardNavigation.Navigate(controller, "ArrowRight");

        Assert.IsTrue(root.IsExpanded);
        Assert.AreSame(root, result.Expanded);
    }

    [TestMethod]
    public void ArrowRight_ExpandedNode_MovesToFirstChild()
    {
        var controller = Build(out _, out var a, out _);
        controller.SetFocus(a);

        TreeKeyboardNavigation.Navigate(controller, "ArrowRight");

        Assert.AreSame(a.Children[0], controller.FocusedNode);
    }

    [TestMethod]
    public void ArrowLeft_ExpandedNode_Collapses()
    {
        var controller = Build(out _, out var a, out _);
        controller.SetFocus(a);

        var result = TreeKeyboardNavigation.Navigate(controller, "ArrowLeft");

        Assert.IsFalse(a.IsExpanded);
        Assert.AreSame(a, result.Collapsed);
    }

    [TestMethod]
    public void ArrowLeft_Child_MovesToParent()
    {
        var controller = Build(out _, out var a, out _);
        var a1 = a.Children[0];
        controller.SetFocus(a1);

        TreeKeyboardNavigation.Navigate(controller, "ArrowLeft");

        Assert.AreSame(a, controller.FocusedNode);
    }

    [TestMethod]
    public void Space_SelectsFocused()
    {
        var controller = Build(out _, out var a, out _);
        controller.SetFocus(a);

        var result = TreeKeyboardNavigation.Navigate(controller, " ");

        Assert.IsTrue(a.IsSelected);
        Assert.IsTrue(result.SelectionChanged);
    }

    [TestMethod]
    public void Space_DisabledNode_NoSelection()
    {
        var root = new TreeNode("root") { IsDisabled = true };
        var controller = new TreeController(new[] { root });
        controller.SetFocus(root);

        var result = TreeKeyboardNavigation.Navigate(controller, " ");

        Assert.IsFalse(root.IsSelected);
        Assert.IsFalse(result.SelectionChanged);
    }
}
