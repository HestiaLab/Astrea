using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Astrea.Components.Tree;

public sealed partial class Tree
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (DataSource is null)
        {
            return;
        }

        var flat = _controller?.FlattenVisible() ?? (IReadOnlyList<TreeNode>)Array.Empty<TreeNode>();
        var cssClass = "tree-container";
        if (S.UseInternalKeyboard)
        {
            cssClass += " disabled-hotKey";
        }

        if (S.ShowLink)
        {
            cssClass += " show-link";
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", $"tree-container-{_id}");
        builder.AddAttribute(2, "class", cssClass);
        builder.AddAttribute(3, "role", "tree");
        builder.AddAttribute(4, "tabindex", "0");

        if (S.UseInternalKeyboard && _editingNode is null)
        {
            builder.AddAttribute(5, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
            builder.AddEventStopPropagationAttribute(6, "onkeydown", true);
            builder.AddEventPreventDefaultAttribute(7, "onkeydown", true);
        }

        if (isPreview)
        {
            builder.AddContent(8, RenderPreview(previewCount, PreviewX, PreviewY));
        }

        if (S.EnableVirtualization)
        {
            builder.OpenComponent<Virtualize<TreeNode>>(10);
            builder.AddAttribute(11, "Items", flat);
            builder.AddAttribute(12, "ItemSize", S.ItemSize);
            builder.AddAttribute(13, "ItemContent", (RenderFragment<TreeNode>)(node => b2 => b2.AddContent(0, RenderLeafNode(node))));
            builder.AddAttribute(15, "Placeholder", (RenderFragment<PlaceholderContext>)(_ => b2 => b2.AddContent(0, RenderEmptyPlaceHolder())));
            builder.CloseComponent();
        }
        else
        {
            for (int i = 0; i < flat.Count; i++)
            {
                builder.AddContent(10 + i, RenderLeafNode(flat[i]));
            }
        }

        builder.CloseElement();
    }

    private RenderFragment RenderLeafNode(TreeNode node)
    {
        return builder =>
        {
            int nodeLevel = S.HideRootNode ? node.Level - 1 : node.Level;

            var nodeCssClass = new List<string> { "tree-node" };
            if (!string.IsNullOrWhiteSpace(node.CssClass))
            {
                nodeCssClass.Add(node.CssClass);
            }

            if (node.IsSelected)
            {
                nodeCssClass.Add("selected");
            }

            int indent = nodeLevel <= 1 ? 0 : (nodeLevel - 1) * S.Indent;

            builder.OpenElement(0, "div");
            builder.SetKey(node.Id);
            builder.AddAttribute(1, "class", string.Join(" ", nodeCssClass));
            builder.AddAttribute(2, "style", $"--indent: {indent}px");
            builder.AddAttribute(3, "id", node.Id);
            builder.AddAttribute(4, "role", "treeitem");
            builder.AddAttribute(5, "aria-level", nodeLevel + 1);
            builder.AddAttribute(6, "aria-selected", node.IsSelected ? "true" : "false");
            builder.AddAttribute(7, "tabindex", node.IsFocused ? "0" : "-1");
            if (node.HasChildren)
            {
                builder.AddAttribute(8, "aria-expanded", node.IsExpanded ? "true" : "false");
            }

            if (node.IsDisabled)
            {
                builder.AddAttribute(9, "aria-disabled", "true");
            }

            if (S.AllowDrag)
            {
                builder.AddAttribute(10, "draggable", "true");
                builder.AddAttribute(11, "ondragstart", EventCallback.Factory.Create<DragEventArgs>(this, async _ => await HandleDragStart()));
                builder.AddAttribute(12, "ondragend", EventCallback.Factory.Create<DragEventArgs>(this, async _ => await HandleDragEnd()));
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
                    builder.OpenElement(35, "use");
                    builder.AddAttribute(36, "href", node.ExpandIcon);
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

        string mainCss = S.OnlyHighlightText ? "" : string.Join(" ", highLightCss);
        string textCss = S.OnlyHighlightText ? string.Join(" ", highLightCss) : "";

        builder.OpenElement(70, "div");
        builder.AddAttribute(71, "class", $"tree-node-content__main {mainCss}");

        builder.OpenElement(72, "span");
        builder.OpenElement(73, "svg");
        builder.AddAttribute(74, "class", "tree-node-icon");
        builder.OpenElement(75, "use");
        builder.AddAttribute(76, "href", node.FoldIcon);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(77, "span");
        builder.AddAttribute(78, "class", $"tree-node-text {textCss}");
        if (S.NodeTemplate is not null)
        {
            builder.AddContent(79, S.NodeTemplate(node));
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

    private void RenderEditContent(RenderTreeBuilder builder, TreeNode node)
    {
        if (S.EditTemplate is not null)
        {
            builder.AddContent(60, S.EditTemplate(node));
        }
        else
        {
            builder.OpenElement(61, "input");
            builder.AddAttribute(62, "class", "tree-node-edit");
            builder.AddAttribute(63, "value", _editingValue ?? node.Text);
            builder.AddAttribute(64, "autofocus", true);
            builder.AddAttribute(65, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => _editingValue = e.Value?.ToString()));
            builder.AddAttribute(66, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, _ => CommitEdit(node, _editingValue ?? node.Text)));
            builder.AddAttribute(67, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => HandleEditKeyDown(node, e)));
            builder.AddElementReferenceCapture(68, r => _editInputRef = r);
            builder.CloseElement();
        }
    }

    private async Task HandleEditKeyDown(TreeNode node, KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            // 取消编辑：退出编辑态，不发送结果通知（REQ-F-088 AC4）
            CancelEdit(node);
            return;
        }

        if (e.Key == "Enter")
        {
            await CommitEdit(node, _editingValue ?? node.Text);
        }
    }

    private void CancelEdit(TreeNode node)
    {
        if (_editingNode is null || !ReferenceEquals(_editingNode, node))
        {
            return;
        }

        _editingNode = null;
        node.IsEditing = false;
        StateHasChanged();
    }

    private async Task CommitEdit(TreeNode node, string newValue)
    {
        // 重入守卫：已提交/已取消后忽略后续 blur 事件
        if (_editingNode is null || !ReferenceEquals(_editingNode, node))
        {
            return;
        }

        if (_controller is null)
        {
            return;
        }

        // BeforeRename 否决：不接受结果，保持编辑态（REQ-F-088 AC2）
        if (!_controller.TryRename(node, newValue))
        {
            return;
        }

        _editingNode = null;
        node.IsEditing = false;
        StateHasChanged();
        await OnInputEnter.InvokeAsync((node, newValue));
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

    private static RenderFragment RenderEmptyPlaceHolder()
    {
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Loading items...");
            builder.CloseElement();
        };
    }
}
