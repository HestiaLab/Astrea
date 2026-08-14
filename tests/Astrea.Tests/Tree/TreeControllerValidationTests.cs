using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeControllerValidationTests
{
    [TestMethod]
    public void Constructor_DuplicateId_Throws()
    {
        var r1 = new TreeNode("r1", id: "dup");
        var r2 = new TreeNode("r2", id: "dup");

        Assert.ThrowsException<TreeStructureException>(() => new TreeController(new[] { r1, r2 }));
    }

    [TestMethod]
    public void Constructor_DuplicateInstance_Throws()
    {
        var node = new TreeNode("n");

        Assert.ThrowsException<TreeStructureException>(() => new TreeController(new[] { node, node }));
    }

    [TestMethod]
    public void Constructor_NodeWithExternalParentPassedAsRoot_Throws()
    {
        var externalParent = new TreeNode("external");
        var proposedRoot = new TreeNode("proposed-root");
        externalParent.Add(proposedRoot);

        Assert.ThrowsException<TreeStructureException>(() => new TreeController(new[] { proposedRoot }));
    }

    [TestMethod]
    public void Refresh_ValidationFailure_LeavesPreviousStateIntact()
    {
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        var a = new TreeNode("a", id: "a");
        root.Add(a);
        var controller = new TreeController(new[] { root });
        controller.Select(a);

        // 引入重复 Id（原索引仍指向 a）
        var b = new TreeNode("b", id: "a");
        root.Add(b);

        Assert.ThrowsException<TreeStructureException>(() => controller.Refresh());

        // 校验失败后，控制器旧状态与索引不被部分破坏
        Assert.AreSame(a, controller.FindById("a"));
        Assert.AreEqual(1, controller.GetSelectedNodes().Count);
        Assert.AreSame(a, controller.GetSelectedNodes().First());
    }
}
