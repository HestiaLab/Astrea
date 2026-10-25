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
    public async Task ControlledExpansion_UnchangedParameter_DoesNotInvalidateVisibleCache()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("Root", id: "root");
        root.Add(new TreeNode("Child", id: "child"));
        var source = new[] { root };

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = source,
            [nameof(Astrea.Components.Tree.Tree.ExpandedIds)] = new[] { "root" },
        }));
        var visible = component.View!.FlattenVisible();
        Assert.AreEqual(2, visible.Count);

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = source,
            [nameof(Astrea.Components.Tree.Tree.ExpandedIds)] = new[] { "root" },
        }));

        Assert.AreSame(visible, component.View!.FlattenVisible());
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
    public async Task AutoDetectStructureChangesFalse_RequiresExplicitRefresh()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        var parameters = new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
            [nameof(Astrea.Components.Tree.Tree.AutoDetectStructureChanges)] = false,
        };
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        var revision = component.View!.Revision;
        Assert.AreEqual(1, component.View.FlattenVisible().Count);

        root.Add(new TreeNode("child", id: "child"));
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        Assert.AreEqual(revision, component.View!.Revision);
        Assert.AreEqual(1, component.View.FlattenVisible().Count);

        await renderer.Dispatcher.InvokeAsync(component.RefreshAsync);
        Assert.AreEqual(revision + 1, component.View!.Revision);
        Assert.AreEqual(2, component.View.FlattenVisible().Count);
    }

    [TestMethod]
    public async Task RemovedNode_PrunesDomTokenCache()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        var child = new TreeNode("child", id: "child");
        root.Add(child);
        var parameters = new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
        };
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        Assert.IsTrue(component.HasNodeToken("root"));
        Assert.IsTrue(component.HasNodeToken("child"));

        root.Remove(child);
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));

        Assert.IsFalse(component.HasNodeToken("child"));
        Assert.IsTrue(component.HasNodeToken("root"));
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

    [TestMethod]
    public async Task ShowLinkClass_FollowsParameter()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var parameters = new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { new TreeNode("root", id: "root") },
            [nameof(Astrea.Components.Tree.Tree.ShowLink)] = true,
        };
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        var root = GetElements(renderer.Frames(componentId))[0];
        StringAssert.Contains((string)root["class"]!, "show-link");

        parameters[nameof(Astrea.Components.Tree.Tree.ShowLink)] = false;
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        root = GetElements(renderer.Frames(componentId))[0];
        Assert.IsFalse(((string)root["class"]!).Contains("show-link", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Render_NodeIndentReflectsHierarchy()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("root", id: "root") { IsExpanded = true };
        var child = new TreeNode("child", id: "child") { IsExpanded = true };
        child.Add(new TreeNode("grandchild", id: "grandchild"));
        root.Add(child);

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
            [nameof(Astrea.Components.Tree.Tree.Indent)] = 20
        }));

        var elements = GetElements(renderer.Frames(componentId));
        var nodeStyles = elements
            .Where(element => element.ContainsKey("data-node-id"))
            .ToDictionary(element => (string)element["data-node-id"]!, element => (string?)element["style"]);
        StringAssert.Contains(nodeStyles["root"], "--indent: 0px");
        StringAssert.Contains(nodeStyles["child"], "--indent: 20px");
        StringAssert.Contains(nodeStyles["grandchild"], "--indent: 40px");

        var indentStyles = elements
            .Where(element => Equals(element.GetValueOrDefault("class"), "tree-node-indent"))
            .Select(element => (string?)element["style"])
            .ToArray();
        CollectionAssert.AreEqual(new[] { "width: 0px", "width: 20px", "width: 40px" }, indentStyles);
    }

    [TestMethod]
    public async Task Render_DisabledNode_IsNotDraggable()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var enabled = new TreeNode("enabled", id: "enabled");
        var disabled = new TreeNode("disabled", id: "disabled") { IsDisabled = true };

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { enabled, disabled },
            [nameof(Astrea.Components.Tree.Tree.AllowDrag)] = true
        }));

        var elements = GetElements(renderer.Frames(componentId));
        var enabledAttributes = elements.Single(element => Equals(element.GetValueOrDefault("data-node-id"), "enabled"));
        var disabledAttributes = elements.Single(element => Equals(element.GetValueOrDefault("data-node-id"), "disabled"));
        Assert.AreEqual("true", enabledAttributes["draggable"]);
        Assert.IsFalse(disabledAttributes.ContainsKey("draggable"));
    }

    [TestMethod]
    public async Task EditLabelFormat_IsConfigurable()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var node = new TreeNode("Node", id: "node");
        var parameters = new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { node },
            [nameof(Astrea.Components.Tree.Tree.EditLabelFormat)] = "Rename {0}",
        };
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(node.Id)));
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));

        var input = GetElements(renderer.Frames(componentId)).Single(element => element.ContainsKey("aria-label"));
        Assert.AreEqual("Rename Node", input["aria-label"]);
    }

    [TestMethod]
    public async Task BeforeExpandFalse_BlocksSetExpanded()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("root", id: "root");
        root.Add(new TreeNode("child", id: "child"));
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
            [nameof(Astrea.Components.Tree.Tree.BeforeExpand)] = (Func<TreeNode, bool>)(_ => false),
        }));

        var result = await renderer.Dispatcher.InvokeAsync(() => component.SetExpandedAsync(root.Id, isExpanded: true));

        Assert.IsTrue(result.Rejected);
        Assert.IsFalse(root.IsExpanded);
    }

    [TestMethod]
    public async Task BeforeEditNameFalse_BlocksBeginEdit()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var node = new TreeNode("node", id: "node");
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { node },
            [nameof(Astrea.Components.Tree.Tree.BeforeEditName)] = (Func<TreeNode, bool>)(_ => false),
        }));

        Assert.IsFalse(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(node.Id)));
        Assert.IsFalse(node.IsEditing);
    }

    [TestMethod]
    public async Task RenameRejectedText_IsConfigurableAndKeepsEditing()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var node = new TreeNode("Node", id: "node");
        TreeEditContext? editContext = null;
        TreeEditRejectedEventArgs? rejected = null;
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { node },
            [nameof(Astrea.Components.Tree.Tree.BeforeRename)] = (Func<TreeNode, string, bool>)((_, _) => false),
            [nameof(Astrea.Components.Tree.Tree.RenameRejectedText)] = "不满足",
            [nameof(Astrea.Components.Tree.Tree.EditTemplate)] = (RenderFragment<TreeEditContext>)(context =>
            {
                editContext = context;
                return builder => builder.AddContent(0, context.Value);
            }),
            [nameof(Astrea.Components.Tree.Tree.OnEditRejected)] = EventCallback.Factory.Create<TreeEditRejectedEventArgs>(
                this, args => rejected = args),
        }));
        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(node.Id)));
        editContext!.SetValue("new value");

        await renderer.Dispatcher.InvokeAsync(editContext.CommitAsync);

        Assert.IsTrue(node.IsEditing);
        Assert.AreEqual("不满足", editContext.ValidationMessage);
        Assert.IsNotNull(rejected);
        Assert.AreEqual("不满足", rejected!.Reason);
    }

    [TestMethod]
    public async Task NodeText_IsRenderedAsTextNotMarkup()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        const string unsafeText = "<script>alert('x')</script>";

        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { new TreeNode(unsafeText, id: "unsafe") }
        }));

        var frames = renderer.Frames(componentId);
        var renderedAsText = false;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames.Array[index];
            if (frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "script")
            {
                Assert.Fail("Node text was interpreted as markup.");
            }

            if (frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == unsafeText)
            {
                renderedAsText = true;
            }
        }

        Assert.IsTrue(renderedAsText);
    }

    [TestMethod]
    public async Task ClearingDataSource_EndsEditingSession()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var node = new TreeNode("node", id: "node");
        var parameters = new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { node },
            [nameof(Astrea.Components.Tree.Tree.EditTemplate)] = EditTemplate
        };
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(node.Id)));
        Assert.IsTrue(node.IsEditing);

        parameters[nameof(Astrea.Components.Tree.Tree.DataSource)] = null;
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));

        Assert.IsFalse(node.IsEditing);
        Assert.IsFalse(component.HasActiveEditSession);
    }

    [TestMethod]
    public async Task RemovingEditedNode_EndsEditingSession()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var root = new TreeNode("root", id: "root");
        var child = new TreeNode("child", id: "child");
        root.Add(child);
        var parameters = new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { root },
            [nameof(Astrea.Components.Tree.Tree.EditTemplate)] = EditTemplate
        };
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));
        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(child.Id)));
        Assert.IsTrue(child.IsEditing);

        root.Remove(child);
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(parameters));

        Assert.IsFalse(child.IsEditing);
        Assert.IsFalse(component.HasActiveEditSession);
    }

    [TestMethod]
    public async Task DisposeAsync_EndsEditingSession()
    {
        using var renderer = new TreeTestRenderer();
        var component = new Astrea.Components.Tree.Tree();
        var componentId = renderer.Attach(component);
        var node = new TreeNode("node", id: "node");
        await renderer.RenderAsync(componentId, ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Astrea.Components.Tree.Tree.DataSource)] = new[] { node },
            [nameof(Astrea.Components.Tree.Tree.EditTemplate)] = EditTemplate
        }));
        Assert.IsTrue(await renderer.Dispatcher.InvokeAsync(() => component.BeginEditAsync(node.Id)));

        await component.DisposeAsync();

        Assert.IsFalse(node.IsEditing);
        Assert.IsFalse(component.HasActiveEditSession);
    }

    private static RenderFragment<TreeEditContext> EditTemplate => context => builder => builder.AddContent(0, context.Value);

    private static List<Dictionary<string, object?>> GetElements(ArrayRange<RenderTreeFrame> frames)
    {
        var elements = new List<Dictionary<string, object?>>();
        Dictionary<string, object?>? current = null;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames.Array[index];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    current = new Dictionary<string, object?>(StringComparer.Ordinal);
                    elements.Add(current);
                    break;
                case RenderTreeFrameType.Attribute when current is not null:
                    current[frame.AttributeName] = frame.AttributeValue;
                    break;
            }
        }

        return elements;
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
