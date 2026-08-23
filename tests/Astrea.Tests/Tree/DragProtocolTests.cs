using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeDragMessageRelationTests
{
    [TestMethod]
    public void TryParseRelation_Valid_ReturnsCorrectValue()
    {
        Assert.IsTrue(TreeDragMessage.TryParseRelation("before", out var b) && b == TreeDropRelation.Before);
        Assert.IsTrue(TreeDragMessage.TryParseRelation("inside", out var i) && i == TreeDropRelation.Inside);
        Assert.IsTrue(TreeDragMessage.TryParseRelation("after", out var a) && a == TreeDropRelation.After);
    }

    [TestMethod]
    public void TryParseRelation_Unknown_ReturnsFalse_NoDefault()
    {
        Assert.IsFalse(TreeDragMessage.TryParseRelation("top", out _));
        Assert.IsFalse(TreeDragMessage.TryParseRelation("bottom", out _));
        Assert.IsFalse(TreeDragMessage.TryParseRelation("center", out _));
        Assert.IsFalse(TreeDragMessage.TryParseRelation("", out _));
        Assert.IsFalse(TreeDragMessage.TryParseRelation(null, out _));
        Assert.IsFalse(TreeDragMessage.TryParseRelation("BEFORE", out _));
    }
}

[TestClass]
public class DragProtocolNormalizeTests
{
    [TestMethod]
    public void NormalizeSources_Empty_ReturnsEmpty()
    {
        Assert.AreEqual(0, DragProtocol.NormalizeSources(new List<TreeNode>()).Count);
    }

    [TestMethod]
    public void NormalizeSources_Siblings_KeepsAll()
    {
        var a = new TreeNode("a");
        var b = new TreeNode("b");

        var result = DragProtocol.NormalizeSources(new[] { a, b });

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void NormalizeSources_AncestorAndDescendant_KeepsAncestorOnly()
    {
        var root = new TreeNode("root");
        var a = new TreeNode("a");
        root.Add(a);
        var a1 = new TreeNode("a1");
        a.Add(a1);

        var result = DragProtocol.NormalizeSources(new[] { a, a1 });

        Assert.AreEqual(1, result.Count);
        Assert.AreSame(a, result[0]);
    }
}

[TestClass]
public class DragProtocolValidateTests
{
    private const string TreeId = "tree1";

    private static TreeController BuildController()
    {
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        var a = new TreeNode("a", id: "a");
        var b = new TreeNode("b", id: "b");
        root.Add(a);
        root.Add(b);
        a.Add(new TreeNode("a1", id: "a1"));
        return new TreeController(new[] { root });
    }

    private static TreeDragMessage ValidMessage(TreeController c)
    {
        return new TreeDragMessage
        {
            ProtocolVersion = 1,
            SessionId = "s1",
            SourceTreeId = TreeId,
            SourceNodeIds = new List<string> { "a" },
            SourceRevision = c.Revision,
            TargetTreeId = TreeId,
            TargetNodeId = "b",
            Relation = "after",
        };
    }

    [TestMethod]
    public void Validate_ValidMessage_Accepts()
    {
        var c = BuildController();
        var validation = DragProtocol.Validate(ValidMessage(c), TreeId, c.Revision, c, null);

        Assert.IsTrue(validation.Accepted);
        Assert.IsNotNull(validation.SourceNodes);
        Assert.IsNotNull(validation.TargetNode);
        Assert.AreEqual(TreeDropRelation.After, validation.Relation);
    }

    [TestMethod]
    public void Validate_UnknownRelation_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.Relation = "top";

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_StaleRevision_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceRevision = c.Revision + 1;

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_CrossTree_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceTreeId = "other-tree";

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_UnknownSource_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceNodeIds = new List<string> { "missing" };

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_DuplicateSource_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceNodeIds = new List<string> { "a", "a" };

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_EmptySource_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceNodeIds = new List<string>();

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_UnknownTarget_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.TargetNodeId = "missing";

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_SelfDrop_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.TargetNodeId = "a";

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_Cycle_Rejects()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceNodeIds = new List<string> { "a" };
        msg.TargetNodeId = "a1"; // a1 是 a 的后代 → 循环

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsFalse(validation.Accepted);
    }

    [TestMethod]
    public void Validate_AncestorAndDescendantSource_NormalizesToAncestor()
    {
        var c = BuildController();
        var msg = ValidMessage(c);
        msg.SourceNodeIds = new List<string> { "a", "a1" }; // a1 是 a 的后代

        var validation = DragProtocol.Validate(msg, TreeId, c.Revision, c, null);

        Assert.IsTrue(validation.Accepted);
        Assert.AreEqual(1, validation.SourceNodes!.Count);
        Assert.AreEqual("a", validation.SourceNodes[0].Id);
    }

    [TestMethod]
    public void Validate_GuardVeto_Rejects()
    {
        var c = BuildController();
        var validation = DragProtocol.Validate(ValidMessage(c), TreeId, c.Revision, c, _ => false);

        Assert.IsFalse(validation.Accepted);
    }
}
