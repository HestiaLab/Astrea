using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Astrea.Components.Tree.Tests;

[TestClass]
public class TreeDragSessionTests
{
    [TestMethod]
    public async Task HandleDrop_SameSessionTwice_NotifiesOnlyOnce()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ => notificationCount++));

        var message = BeginAndCreateMessage(tree, controller, treeId, "session-1");

        await tree.HandleDrop(message);
        await tree.HandleDrop(message);

        Assert.AreEqual(1, notificationCount);
    }

    [TestMethod]
    public async Task HandleDrop_CallbackThrows_ConsumesSession()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ =>
        {
            notificationCount++;
            throw new InvalidOperationException("callback failed");
        }));

        var message = BeginAndCreateMessage(tree, controller, treeId, "session-2");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => tree.HandleDrop(message));
        await tree.HandleDrop(message);

        Assert.AreEqual(1, notificationCount);
    }

    [TestMethod]
    public async Task HandleDrop_RejectedMessage_ConsumesSession()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ => notificationCount++));

        _ = tree.BeginDrag("session-3", "[\"a\"]");
        var rejected = CreateMessage(controller, treeId, "session-3", relation: "unknown");
        var otherwiseValid = CreateMessage(controller, treeId, "session-3", relation: "after");

        await tree.HandleDrop(rejected);
        await tree.HandleDrop(otherwiseValid);

        Assert.AreEqual(0, notificationCount);
    }

    [TestMethod]
    public async Task HandleDrop_SourceIdsChangedAfterBegin_Rejects()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ => notificationCount++));

        _ = tree.BeginDrag("session-source-snapshot", "[\"a\"]");
        var tampered = CreateMessage(controller, treeId, "session-source-snapshot", relation: "after", sourceNodeId: "b", targetNodeId: "c");

        await tree.HandleDrop(tampered);

        Assert.AreEqual(0, notificationCount);
    }

    [TestMethod]
    public async Task HandleDrop_RevisionChangedAfterBegin_CannotUseCurrentRevision()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ => notificationCount++));

        _ = tree.BeginDrag("session-revision-snapshot", "[\"a\"]");
        controller.Refresh();
        var forgedAsCurrent = CreateMessage(controller, treeId, "session-revision-snapshot", relation: "after");

        await tree.HandleDrop(forgedAsCurrent);

        Assert.AreEqual(0, notificationCount);
    }

    [TestMethod]
    public void BeginDrag_WhenDragIsDisabled_Rejects()
    {
        var (tree, _, _) = CreateTree();
        SetParameter(tree, nameof(Tree.AllowDrag), false);

        var result = tree.BeginDrag("session-disabled", "[\"a\"]");

        Assert.AreEqual("{}", result);
    }

    [TestMethod]
    public async Task DestroyDragAsync_EndsActiveSession()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ => notificationCount++));
        var message = BeginAndCreateMessage(tree, controller, treeId, "session-disabled-mid-drag");

        SetParameter(tree, nameof(Tree.AllowDrag), false);
        await InvokePrivateTask(tree, "DestroyDragAsync");
        SetParameter(tree, nameof(Tree.AllowDrag), true);
        await tree.HandleDrop(message);

        Assert.AreEqual(0, notificationCount);
    }

    [TestMethod]
    public async Task CancelDrag_StaleSession_DoesNotCancelNewSession()
    {
        var (tree, controller, treeId) = CreateTree();
        var notificationCount = 0;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, _ => notificationCount++));

        _ = tree.BeginDrag("old-session", "[\"a\"]");
        var currentMessage = BeginAndCreateMessage(tree, controller, treeId, "current-session");
        tree.CancelDrag("old-session");
        await tree.HandleDrop(currentMessage);

        Assert.AreEqual(1, notificationCount);
    }

    [TestMethod]
    public void TreeDragMessage_JsCamelCaseJson_DeserializesAllFields()
    {
        const string json = "{\"protocolVersion\":1,\"sessionId\":\"s-1\",\"sourceTreeId\":\"tree-1\",\"sourceNodeIds\":[\"a\",\"b\"],\"sourceRevision\":42,\"targetTreeId\":\"tree-1\",\"targetNodeId\":\"c\",\"relation\":\"inside\"}";

        var message = JsonSerializer.Deserialize<TreeDragMessage>(json);

        Assert.IsNotNull(message);
        Assert.AreEqual(1, message!.ProtocolVersion);
        Assert.AreEqual("s-1", message.SessionId);
        Assert.AreEqual("tree-1", message.SourceTreeId);
        CollectionAssert.AreEqual(new[] { "a", "b" }, message.SourceNodeIds);
        Assert.AreEqual(42L, message.SourceRevision);
        Assert.AreEqual("tree-1", message.TargetTreeId);
        Assert.AreEqual("c", message.TargetNodeId);
        Assert.AreEqual("inside", message.Relation);
    }

    [TestMethod]
    public async Task HandleDrop_JsCamelCaseMessage_NotifiesOnceWithRelation()
    {
        var (tree, controller, treeId) = CreateTree();
        TreeDropEventArgs? received = null;
        SetParameter(tree, nameof(Tree.OnDrop), EventCallback.Factory.Create<TreeDropEventArgs>(this, args => received = args));
        Assert.AreNotEqual("{}", tree.BeginDrag("js-session", "[\"a\"]"));

        var json = $"{{\"protocolVersion\":1,\"sessionId\":\"js-session\",\"sourceTreeId\":\"{treeId}\",\"sourceNodeIds\":[\"a\"],\"sourceRevision\":{controller.Revision},\"targetTreeId\":\"{treeId}\",\"targetNodeId\":\"b\",\"relation\":\"after\"}}";

        await tree.HandleDrop(json);

        Assert.IsNotNull(received);
        Assert.AreEqual("b", received!.TargetNode.Id);
        Assert.AreEqual(TreeDropRelation.After, received.Relation);
        CollectionAssert.AreEqual(new[] { "a" }, received.SourceNodes.Select(node => node.Id).ToArray());
    }

    [TestMethod]
    public void BeginDrag_Response_UsesCamelCaseWireNames()
    {
        var (tree, _, _) = CreateTree();

        var result = tree.BeginDrag("wire-session", "[\"a\"]");

        using var document = JsonDocument.Parse(result);
        Assert.IsTrue(document.RootElement.TryGetProperty("sourceRevision", out _));
        Assert.IsTrue(document.RootElement.TryGetProperty("sourceNodeIds", out _));
    }

    [TestMethod]
    public void BeginDrag_DisabledSource_Rejects()
    {
        var (tree, controller, _) = CreateTree();
        controller.FindById("b")!.IsDisabled = true;

        Assert.AreEqual("{}", tree.BeginDrag("disabled-session", "[\"b\"]"));
        Assert.AreEqual("{}", tree.BeginDrag("disabled-session", "[\"a\",\"b\"]"));
    }

    [TestMethod]
    public async Task DisposeAsync_JsDisconnected_ClearsAllDragReferences()
    {
        var tree = new Tree();
        SetField(tree, "_dropInstance", new DisconnectingJsObjectReference());
        SetField(tree, "_dropModule", new DisconnectingJsObjectReference());
        SetField(tree, "_dotNetRef", DotNetObjectReference.Create(tree));

        await tree.DisposeAsync();
        await tree.DisposeAsync();

        Assert.IsNull(GetField(tree, "_dropInstance"));
        Assert.IsNull(GetField(tree, "_dropModule"));
        Assert.IsNull(GetField(tree, "_dotNetRef"));
    }

    private static (Tree Tree, TreeController Controller, string TreeId) CreateTree()
    {
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        root.Add(new TreeNode("a", id: "a"));
        root.Add(new TreeNode("b", id: "b"));
        root.Add(new TreeNode("c", id: "c"));
        var controller = new TreeController(new[] { root });
        var tree = new Tree();
        SetParameter(tree, nameof(Tree.AllowDrag), true);
        SetField(tree, "_controller", controller);
        var treeId = (string)GetField(tree, "_id")!;
        return (tree, controller, treeId);
    }

    private static string BeginAndCreateMessage(Tree tree, TreeController controller, string treeId, string sessionId)
    {
        var result = tree.BeginDrag(sessionId, "[\"a\"]");
        Assert.AreNotEqual("{}", result);
        return CreateMessage(controller, treeId, sessionId, "after");
    }

    private static string CreateMessage(
        TreeController controller,
        string treeId,
        string sessionId,
        string relation,
        string sourceNodeId = "a",
        string targetNodeId = "b")
    {
        return JsonSerializer.Serialize(new TreeDragMessage
        {
            ProtocolVersion = TreeDragMessage.CurrentProtocolVersion,
            SessionId = sessionId,
            SourceTreeId = treeId,
            SourceNodeIds = new List<string> { sourceNodeId },
            SourceRevision = controller.Revision,
            TargetTreeId = treeId,
            TargetNodeId = targetNodeId,
            Relation = relation,
        });
    }

    private static object? GetField(Tree tree, string name)
        => typeof(Tree).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tree);

    private static void SetField(Tree tree, string name, object value)
        => typeof(Tree).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tree, value);

    private static void SetParameter<T>(Tree tree, string name, T value)
        => typeof(Tree).GetProperty(name, BindingFlags.Instance | BindingFlags.Public)!.SetValue(tree, value);

    private static Task InvokePrivateTask(Tree tree, string name)
        => (Task)typeof(Tree).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(tree, null)!;

    private sealed class DisconnectingJsObjectReference : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromException<TValue>(new JSDisconnectedException("test disconnect"));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromException<TValue>(new JSDisconnectedException("test disconnect"));

        public ValueTask DisposeAsync()
            => ValueTask.FromException(new JSDisconnectedException("test disconnect"));
    }
}
