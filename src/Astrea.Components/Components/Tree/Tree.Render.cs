using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Astrea.Components.Tree;

public sealed partial class Tree
{
    private readonly Dictionary<string, int> _nodeTokens = new();
    private int _nodeTokenCounter;

    private string NodeDomId(string nodeId)
    {
        if (!_nodeTokens.TryGetValue(nodeId, out var token))
        {
            token = _nodeTokenCounter++;
            _nodeTokens[nodeId] = token;
        }

        return $"ast-tree-{_id}-node-{token}";
    }

    internal bool HasNodeToken(string nodeId) => _nodeTokens.ContainsKey(nodeId);

    private void PruneNodeTokens()
    {
        if (_controller is null)
        {
            ClearNodeTokens();
            return;
        }

        var liveIds = new HashSet<string>(_controller.Flatten().Select(node => node.Id), StringComparer.Ordinal);
        foreach (var id in _nodeTokens.Keys.Where(id => !liveIds.Contains(id)).ToArray())
        {
            _nodeTokens.Remove(id);
        }
    }

    private void ClearNodeTokens()
    {
        _nodeTokens.Clear();
        _nodeTokenCounter = 0;
    }

    private static bool IsContainerGuaranteedAttribute(string key)
        => key is "id" or "role" or "tabindex" or "aria-activedescendant" or "aria-multiselectable";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var flat = _controller?.FlattenVisible() ?? (IReadOnlyList<TreeNode>)Array.Empty<TreeNode>();

        // 组件 class 段
        var componentClass = "tree-container";
        if (UseInternalKeyboard)
        {
            componentClass += " disabled-hotKey";
        }

        if (ShowLink)
        {
            componentClass += " show-link";
        }

        if (EnableVirtualization)
        {
            componentClass += " tree-container--virtualized";
        }

        // 附加属性合并（REQ-F-001 冲突规则）：class 两者保留；保证属性以组件为准；其余用应用值
        var mergedClass = componentClass;
        var extraAttributes = new List<KeyValuePair<string, object>>();
        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key == "class")
                {
                    if (attr.Value?.ToString() is { Length: > 0 } appClass)
                    {
                        mergedClass = $"{componentClass} {appClass}";
                    }
                }
                else if (!IsContainerGuaranteedAttribute(attr.Key))
                {
                    extraAttributes.Add(attr);
                }
            }
        }

        var focusedId = _controller?.FocusedId;
        // 虚拟化下 aria-activedescendant 只引用已渲染节点（由 SyncVirtualizationFocusAsync 维护）
        var ariaFocusId = EnableVirtualization ? _ariaFocusId : focusedId;
        var activeDescendant = ariaFocusId is null ? null : NodeDomId(ariaFocusId);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", $"tree-container-{_id}");
        builder.AddAttribute(2, "class", mergedClass);
        builder.AddAttribute(3, "role", "tree");
        builder.AddAttribute(4, "tabindex", "0");
        if (activeDescendant is not null)
        {
            builder.AddAttribute(5, "aria-activedescendant", activeDescendant);
        }

        if (SelectionMode == TreeSelectionMode.Multiple)
        {
            builder.AddAttribute(6, "aria-multiselectable", "true");
        }

        if (extraAttributes.Count > 0)
        {
            builder.AddMultipleAttributes(7, extraAttributes);
        }

        if (UseInternalKeyboard && _editingContext is null)
        {
            builder.AddAttribute(8, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
            builder.AddEventStopPropagationAttribute(9, "onkeydown", true);
            // 不无条件 preventDefault：Tab/Shift+Tab 必须能离开；已处理按键的 preventDefault 由 JS 层负责（Phase 4）
        }

        if (isPreview)
        {
            builder.AddContent(10, RenderPreview(previewCount, PreviewX, PreviewY));
        }

        // 容器聚焦时若无焦点节点，聚焦首个可见节点（WAI-ARIA Tree View Pattern：树接收焦点时建立 active descendant）
        builder.AddAttribute(11, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, _ => HandleContainerFocus()));
        builder.AddElementReferenceCapture(12, reference => _containerRef = reference);

        if (EnableVirtualization)
        {
            builder.OpenComponent<Virtualize<TreeNode>>(20);
            builder.AddAttribute(21, "Items", flat);
            builder.AddAttribute(22, "ItemSize", ItemSize);
            builder.AddAttribute(23, "ItemContent", (RenderFragment<TreeNode>)(node => b2 => b2.AddContent(0, RenderLeafNode(node))));
            builder.AddAttribute(24, "Placeholder", (RenderFragment<PlaceholderContext>)(_ => b2 => b2.AddContent(0, RenderEmptyPlaceHolder())));
            builder.CloseComponent();
        }
        else
        {
            // 动态集合：每轮固定源码 sequence，身份由 SetKey 表达
            foreach (var node in flat)
            {
                builder.AddContent(30, RenderLeafNode(node));
            }
        }

        builder.CloseElement();
    }

    private RenderFragment RenderLeafNode(TreeNode node)
    {
        return builder =>
        {
            int nodeLevel = node.Level;

            var nodeCssClass = new List<string> { "tree-node" };
            if (!string.IsNullOrWhiteSpace(node.CssClass))
            {
                nodeCssClass.Add(node.CssClass);
            }

            if (node.IsSelected)
            {
                nodeCssClass.Add("selected");
            }

            int indent = nodeLevel * Indent;

            builder.OpenElement(0, "div");
            builder.SetKey(node.Id);
            builder.AddAttribute(1, "class", string.Join(" ", nodeCssClass));
            builder.AddAttribute(2, "style", $"--indent: {indent}px; --indent-step: {Indent}px");
            builder.AddAttribute(3, "id", NodeDomId(node.Id));
            builder.AddAttribute(4, "data-node-id", node.Id);
            builder.AddAttribute(5, "role", "treeitem");
            builder.AddAttribute(6, "aria-level", nodeLevel + 1);
            builder.AddAttribute(7, "aria-selected", node.IsSelected ? "true" : "false");
            if (node.HasChildren)
            {
                builder.AddAttribute(9, "aria-expanded", node.IsExpanded ? "true" : "false");
            }

            if (node.IsDisabled)
            {
                builder.AddAttribute(10, "aria-disabled", "true");
            }

            if (AllowDrag && !node.IsDisabled)
            {
                builder.AddAttribute(11, "draggable", "true");
            }

            builder.OpenElement(20, "span");
            builder.AddAttribute(21, "class", "tree-node-indent");
            builder.AddAttribute(22, "style", $"width: {indent}px");
            builder.CloseElement();

            if (!node.IsExpandIconDisable)
            {
                builder.OpenElement(30, "span");
                builder.AddAttribute(31, "class", "tree-node-control");
                if (node.HasChildren)
                {
                    builder.AddAttribute(32, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => ToggleNode(node)));
                    builder.OpenElement(33, "svg");
                    builder.AddAttribute(34, "class", "tree-node-icon");
                    builder.AddAttribute(35, "aria-hidden", "true");
                    builder.OpenElement(36, "use");
                    builder.AddAttribute(37, "href", node.ExpandIcon);
                    builder.CloseElement();
                    builder.CloseElement();
                }

                builder.CloseElement();

                builder.OpenElement(37, "span");
                builder.AddAttribute(38, "class", "tree-node-control__slot");
                builder.CloseElement();
            }

            builder.OpenElement(40, "span");
            builder.AddAttribute(41, "class", "tree-node-content");

            if (node.IsEditing)
            {
                RenderEditContent(builder, node);
            }
            else
            {
                RenderNormalContent(builder, node);
            }

            builder.OpenElement(50, "span");
            builder.CloseElement();
            builder.CloseElement();

            builder.CloseElement();
        };
    }

    private void RenderNormalContent(RenderTreeBuilder builder, TreeNode node)
    {
        builder.AddAttribute(60, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, args => HandleClick(args, node)));
        builder.AddAttribute(61, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await HandleDblClick(node)));
        builder.AddAttribute(62, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, args => HandleRightClick(args, node)));
        builder.AddEventPreventDefaultAttribute(63, "oncontextmenu", true);

        var highLightCss = new List<string>();
        if (node.IsSelected)
        {
            highLightCss.Add("selected");
        }

        if (node.IsFocused)
        {
            highLightCss.Add("focused");
        }

        string mainCss = OnlyHighlightText ? "" : string.Join(" ", highLightCss);
        string textCss = OnlyHighlightText ? string.Join(" ", highLightCss) : "";

        builder.OpenElement(70, "div");
        builder.AddAttribute(71, "class", $"tree-node-content__main {mainCss}");

        RenderNodeIcon(builder, node);

        builder.OpenElement(77, "span");
        builder.AddAttribute(78, "class", $"tree-node-text {textCss}");
        if (NodeTemplate is not null)
        {
            builder.AddContent(79, NodeTemplate(node));
        }
        else
        {
            builder.AddContent(79, $"{node.PlaceHolder}{node.Text}");
        }

        builder.CloseElement();

        builder.OpenElement(80, "span");
        builder.AddAttribute(81, "class", "tree-node-content__slot");
        builder.CloseElement();

        builder.CloseElement();
    }

    /// <summary>
    /// 渲染节点图标（技术中立）：优先 IconTemplate，其次默认 SVG symbol 适配（字符串图标），
    /// 无图标时不渲染图标容器（REQ-F-064 AC2）；装饰图标 aria-hidden（REQ-F-064 AC3）。
    /// </summary>
    private void RenderNodeIcon(RenderTreeBuilder builder, TreeNode node)
    {
        if (IconTemplate is not null)
        {
            var iconContext = new TreeIconContext(node, node.IsExpanded, node.HasChildren, node.IsDisabled);
            builder.OpenElement(72, "span");
            builder.AddAttribute(73, "class", "tree-node-icon");
            builder.AddAttribute(74, "aria-hidden", "true");
            builder.AddContent(75, IconTemplate(iconContext));
            builder.CloseElement();
            return;
        }

        if (!string.IsNullOrEmpty(node.FoldIcon))
        {
            builder.OpenElement(72, "span");
            builder.AddAttribute(73, "class", "tree-node-icon");
            builder.AddAttribute(74, "aria-hidden", "true");
            builder.OpenElement(75, "svg");
            builder.OpenElement(76, "use");
            builder.AddAttribute(77, "href", node.FoldIcon);
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private void RenderEditContent(RenderTreeBuilder builder, TreeNode node)
    {
        var context = _editingContext;
        if (context is null)
        {
            return;
        }

        if (EditTemplate is not null)
        {
            builder.AddContent(60, EditTemplate(context));
        }
        else
        {
            builder.OpenElement(61, "input");
            builder.AddAttribute(62, "class", "tree-node-edit");
            builder.AddAttribute(69, "aria-label", string.Format(CultureInfo.CurrentCulture, EditLabelFormat, node.Text));
            builder.AddAttribute(63, "value", context.Value);
            builder.AddAttribute(64, "autofocus", true);
            builder.AddAttribute(65, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => context.SetValue(e.Value?.ToString() ?? string.Empty)));
            builder.AddAttribute(66, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, _ => HandleEditBlur(context)));
            builder.AddAttribute(67, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => HandleEditKeyDown(context, e)));
            builder.AddElementReferenceCapture(68, r =>
            {
                _editInputRef = r;
                _editInputRendered = true;
            });
            builder.CloseElement();
        }
    }

    private async Task HandleEditKeyDown(TreeEditContext context, KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            // 取消编辑：退出编辑态，不发送结果通知（REQ-F-088 AC4）
            await context.CancelAsync();
            return;
        }

        if (e.Key == "Enter")
        {
            // Enter 提交后，容器可能在同一按键派发中重新挂载 onkeydown 并再次处理该 Enter；
            // 标记抑制，避免提交后立即触发一次 OnNodeInvoked。
            _suppressNodeInvoke = true;
            _suppressBlurCommit = (context.SessionId, context.ValueVersion);
            await context.CommitAsync();
        }
    }

    private async Task HandleEditBlur(TreeEditContext context)
    {
        if (_suppressBlurCommit is { } suppressed &&
            suppressed.SessionId == context.SessionId &&
            suppressed.ValueVersion == context.ValueVersion)
        {
            _suppressBlurCommit = null;
            return;
        }

        await context.CommitAsync();
    }

    private static RenderFragment RenderPreview(int count, double x, double y)
    {
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "drag-preview-container");
            builder.AddAttribute(2, "style", $"left:{x}px; top:{y}px;");
            if (count > 1)
            {
                builder.OpenElement(3, "div");
                builder.AddAttribute(4, "class", "drag-preview-count");
                builder.AddContent(5, count.ToString());
                builder.CloseElement();
            }

            builder.CloseElement();
        };
    }

    private RenderFragment RenderEmptyPlaceHolder()
    {
        var text = LoadingText;
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, text);
            builder.CloseElement();
        };
    }
}
