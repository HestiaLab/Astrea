using Astrea.Components.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

#pragma warning disable BL0006

namespace Astrea.Tests.Tree;

[TestClass]
public sealed class TreeComponentLifecycleTests
{
    [TestMethod]
    public async Task NullDataSource_RendersStableAccessibleTreeContainer()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = null,
            [nameof(Astrea.Components.Tree.Tree.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["aria-label"] = "Empty tree"
            }
        }));

        var attributes = GetAttributes(renderer.Frames(componentId));
        Assert.AreEqual("tree", attributes["role"]);
        Assert.AreEqual("0", attributes["tabindex"]?.ToString());
        Assert.AreEqual("Empty tree", attributes["aria-label"]);
        Assert.IsFalse(attributes.ContainsKey("aria-activedescendant"));
    }

    [TestMethod]
    public async Task EqualLogicalIds_InDifferentTrees_ProduceDifferentDomIds()
    {
        using var renderer = new TreeTestRenderer();
        var first = new Astrea.Components.Tree.Tree();
        var second = new Astrea.Components.Tree.Tree();
        var firstId = renderer.Attach(first);
        var secondId = renderer.Attach(second);
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { new TreeNode("Node", id: "same") }
        });

        await renderer.RenderAsync(firstId, parameters);
        await renderer.RenderAsync(secondId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { new TreeNode("Node", id: "same") }
        }));

        var firstNodeId = GetAttributeValues(renderer.Frames(firstId), "data-node-id", "same", "id").Single();
        var secondNodeId = GetAttributeValues(renderer.Frames(secondId), "data-node-id", "same", "id").Single();
        Assert.AreNotEqual(firstNodeId, secondNodeId);
    }

    [TestMethod]
    public async Task StaleEditContext_DoesNotCommitCurrentNode()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var first = new TreeNode("first") { Text = "First" };
        var second = new TreeNode("second") { Text = "Second" };
        var contexts = new List<TreeEditContext>();
        var committed = 0;

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { first, second },
            [nameof(Astrea.Components.Tree.Tree.EditTemplate)] = (RenderFragment<TreeEditContext>)(context => builder =>
            {
                contexts.Add(context);
                builder.AddContent(0, context.Value);
            }),
            [nameof(Astrea.Components.Tree.Tree.OnEditCommitted)] = EventCallback.Factory.Create<TreeEditEventArgs>(
                this, _ => committed++)
        }));

        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(first.Id)));
        var staleContext = contexts.Last();
        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(second.Id)));

        staleContext.SetValue("Stale value");
        await renderer.Dispatcher.InvokeAsync(staleContext.CommitAsync);

        Assert.AreEqual("First", first.Text);
        Assert.AreEqual("Second", second.Text);
        Assert.AreEqual(0, committed);
        Assert.IsTrue(second.IsEditing);
    }

    [TestMethod]
    public async Task ControlledSelection_UserInteractionOnlyProposesNextState()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var first = new TreeNode("First", id: "first");
        var second = new TreeNode("Second", id: "second");
        IReadOnlyCollection<string>? proposed = null;

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { first, second },
            [nameof(Astrea.Components.Tree.Tree.SelectedIds)] = new[] { first.Id },
            [nameof(Astrea.Components.Tree.Tree.SelectedIdsChanged)] = EventCallback.Factory.Create<IReadOnlyCollection<string>>(
                this, ids => proposed = ids)
        }));

        await renderer.Dispatcher.InvokeAsync(() => InvokePrivateTask(
            component,
            "HandleSelectionInteraction",
            new MouseEventArgs(),
            second));

        CollectionAssert.AreEqual(new[] { second.Id }, proposed?.ToArray());
        Assert.IsTrue(first.IsSelected);
        Assert.IsFalse(second.IsSelected);
    }

    [TestMethod]
    public async Task ControlledExpansion_UserInteractionOnlyProposesNextState()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("Root", id: "root");
        root.Add(new TreeNode("Child", id: "child"));
        IReadOnlyCollection<string>? proposed = null;

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
            [nameof(Astrea.Components.Tree.Tree.ExpandedIds)] = Array.Empty<string>(),
            [nameof(Astrea.Components.Tree.Tree.ExpandedIdsChanged)] = EventCallback.Factory.Create<IReadOnlyCollection<string>>(
                this, ids => proposed = ids)
        }));

        await renderer.Dispatcher.InvokeAsync(() => InvokePrivateTask(component, "ToggleNode", root));

        CollectionAssert.AreEqual(new[] { root.Id }, proposed?.ToArray());
        Assert.IsFalse(root.IsExpanded);
    }

    [TestMethod]
    public async Task SameDataSource_UnrelatedParameterUpdateDoesNotIncrementRevision()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("Root", id: "root");
        IReadOnlyList<TreeNode> source = new[] { root };

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = source,
            [nameof(Astrea.Components.Tree.Tree.ShowLink)] = true
        }));
        var initialRevision = component.View!.Revision;

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = source,
            [nameof(Astrea.Components.Tree.Tree.ShowLink)] = false
        }));
        Assert.AreEqual(initialRevision, component.View!.Revision);

        root.Add(new TreeNode("Child", id: "child"));
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = source,
            [nameof(Astrea.Components.Tree.Tree.ShowLink)] = false
        }));
        Assert.AreEqual(initialRevision + 1, component.View!.Revision);
    }

    [TestMethod]
    public void TreeView_DoesNotExposeMutationCommands()
    {
        var publicMethods = typeof(TreeView).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsFalse(publicMethods.Contains("Select"));
        Assert.IsFalse(publicMethods.Contains("ReplaceSelection"));
        Assert.IsFalse(publicMethods.Contains("Expand"));
        Assert.IsFalse(publicMethods.Contains("Refresh"));
    }

    [TestMethod]
    public async Task AsyncExpandGuard_StructureChangesWhileAwaiting_RejectsStaleResult()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("Root", id: "root");
        root.Add(new TreeNode("Child", id: "child"));
        var decision = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
            [nameof(Astrea.Components.Tree.Tree.BeforeExpandAsync)] =
                (Func<TreeNode, ValueTask<bool>>)(_ => new ValueTask<bool>(decision.Task))
        }));

        var pending = component.SetExpandedAsync(root.Id, isExpanded: true);
        root.Add(new TreeNode("Later child", id: "later"));
        await renderer.Dispatcher.InvokeAsync(component.RefreshAsync);
        decision.SetResult(true);

        var result = await pending;
        Assert.IsTrue(result.Rejected);
        Assert.IsFalse(root.IsExpanded);
    }

    [TestMethod]
    public async Task Enter_InvokesFocusedNodeWithoutChangingSelection()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var node = new TreeNode("Node", id: "node");
        TreeNode? invoked = null;

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { node },
            [nameof(Astrea.Components.Tree.Tree.OnNodeInvoked)] = EventCallback.Factory.Create<TreeNodeEventArgs>(
                this, args => invoked = args.Node)
        }));
        await renderer.Dispatcher.InvokeAsync(() => component.FocusAsync(node.Id));

        await renderer.Dispatcher.InvokeAsync(() => InvokePrivateTask(
            component,
            "HandleKeyDown",
            new KeyboardEventArgs { Key = "Enter" }));

        Assert.AreSame(node, invoked);
        Assert.IsFalse(node.IsSelected);
    }

    private static Task InvokePrivateTask(object instance, string methodName, params object[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");
        return (Task)(method.Invoke(instance, arguments)
            ?? throw new InvalidOperationException($"Method {methodName} returned null."));
    }

    private static Dictionary<string, object?> GetAttributes(ArrayRange<RenderTreeFrame> frames)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames.Array[index];
            if (frame.FrameType == RenderTreeFrameType.Attribute)
            {
                result[frame.AttributeName] = frame.AttributeValue;
            }
        }

        return result;
    }

    private static IEnumerable<string> GetAttributeValues(
        ArrayRange<RenderTreeFrame> frames,
        string markerName,
        string markerValue,
        string requestedName)
    {
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames.Array[index];
            if (frame.FrameType != RenderTreeFrameType.Attribute ||
                frame.AttributeName != markerName ||
                !Equals(frame.AttributeValue, markerValue))
            {
                continue;
            }

            for (var attributeIndex = index - 1; attributeIndex >= 0; attributeIndex--)
            {
                var attribute = frames.Array[attributeIndex];
                if (attribute.FrameType == RenderTreeFrameType.Element)
                {
                    break;
                }

                if (attribute.FrameType == RenderTreeFrameType.Attribute &&
                    attribute.AttributeName == requestedName &&
                    attribute.AttributeValue is not null)
                {
                    yield return attribute.AttributeValue.ToString()!;
                    break;
                }
            }
        }
    }
}

#pragma warning restore BL0006
