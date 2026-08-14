using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Astrea.Components.ResourceTable;

public sealed partial class ResourceTable
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"ast-resource-table {(ContainerCss ?? "ast-rt-container")}");
        builder.AddAttribute(3, "tabindex", "0");
        builder.AddAttribute(4, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        // 工具栏
        if (ShowButton || ToolBar is not null)
        {
            builder.OpenElement(10, "div");
            builder.AddAttribute(11, "class", "ast-rt-toolbar");

            if (ShowButton)
            {
                builder.OpenElement(20, "div");
                builder.AddAttribute(21, "class", "ast-rt-btn-group");
                builder.AddContent(22, BuildButton(IconAdd ?? DefaultIcon_Add, OnAddClicked, "Add"));
                builder.AddContent(23, BuildButton(IconRemove ?? DefaultIcon_Remove, OnRemoveClicked, "Remove"));
                builder.AddContent(24, BuildButton(IconMoveUp ?? DefaultIcon_MoveUp, OnMoveUpClicked, "Move Up"));
                builder.AddContent(25, BuildButton(IconMoveDown ?? DefaultIcon_MoveDown, OnMoveDownClicked, "Move Down"));

                if (ShowRedoButton)
                {
                    builder.OpenElement(26, "div");
                    builder.AddAttribute(27, "class", "ast-rt-divider");
                    builder.CloseElement();

                    builder.AddContent(28, BuildButton(IconUndo ?? DefaultIcon_Undo, OnUndoClicked, "Undo", !CanUndo));
                    builder.AddContent(29, BuildButton(IconRedo ?? DefaultIcon_Redo, OnRedoClicked, "Redo", !CanRedo));
                }

                builder.CloseElement();
            }

            builder.AddContent(30, ToolBar);
            builder.CloseElement();
        }

        // 表体滚动区
        builder.OpenElement(50, "div");
        builder.AddAttribute(51, "class", "ast-rt-scroll");

        builder.OpenElement(60, "table");
        builder.AddAttribute(61, "id", _tableId);
        builder.AddAttribute(62, "class", "ast-rt-table ast-rt-no-select");

        // 表头
        builder.OpenElement(70, "thead");
        builder.OpenElement(71, "tr");
        if (HasColumns)
        {
            for (var i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];
                var index = i;
                builder.OpenElement(80 + i * 10, "th");
                builder.AddAttribute(81 + i * 10, "class", "ast-rt-th");
                if (column.Width is not null)
                {
                    builder.AddAttribute(82 + i * 10, "style", $"width:{column.Width};");
                }

                builder.OpenElement(83 + i * 10, "div");
                builder.AddAttribute(84 + i * 10, "class", "ast-rt-th-content");
                builder.AddContent(85 + i * 10, column.Title);
                builder.CloseElement();

                if (index != _columns.Count - 1 && column.Resizable)
                {
                    builder.OpenElement(86 + i * 10, "div");
                    builder.AddAttribute(87 + i * 10, "class", "ast-rt-resize-handle");
                    builder.AddAttribute(88 + i * 10, "data-column-index", index.ToString());
                    builder.CloseElement();
                }

                builder.CloseElement();
            }
        }
        else
        {
            builder.OpenElement(80, "th");
            builder.AddAttribute(81, "class", "ast-rt-th");
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();

        // 表体
        builder.OpenElement(90, "tbody");
        builder.AddContent(91, TableBody);
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement();
    }

    private static RenderFragment BuildButton(RenderFragment icon, EventCallback onClick, string title, bool disabled = false)
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "type", "button");
            builder.AddAttribute(2, "class", "ast-rt-btn");
            builder.AddAttribute(3, "title", title);
            builder.AddAttribute(4, "onclick", onClick);
            if (disabled)
            {
                builder.AddAttribute(5, "disabled", "disabled");
            }

            builder.AddContent(6, icon);
            builder.CloseElement();
        };
    }

    /// <summary>默认内联 SVG 图标（lucide 风格，stroke=currentColor，零框架依赖）。</summary>
    private static RenderFragment DefaultIcon_Add => BuildSvgIcon(
        "<line x1=\"12\" y1=\"5\" x2=\"12\" y2=\"19\"></line><line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"></line>");

    private static RenderFragment DefaultIcon_Remove => BuildSvgIcon(
        "<line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"></line>");

    private static RenderFragment DefaultIcon_MoveUp => BuildSvgIcon(
        "<line x1=\"12\" y1=\"19\" x2=\"12\" y2=\"5\"></line><polyline points=\"5 12 12 5 19 12\"></polyline>");

    private static RenderFragment DefaultIcon_MoveDown => BuildSvgIcon(
        "<line x1=\"12\" y1=\"5\" x2=\"12\" y2=\"19\"></line><polyline points=\"19 12 12 19 5 12\"></polyline>");

    private static RenderFragment DefaultIcon_Undo => BuildSvgIcon(
        "<polyline points=\"1 4 1 10 7 10\"></polyline><path d=\"M3.51 15a9 9 0 1 0 2.13-9.36L1 10\"></path>");

    private static RenderFragment DefaultIcon_Redo => BuildSvgIcon(
        "<polyline points=\"23 4 23 10 17 10\"></polyline><path d=\"M20.49 15a9 9 0 1 1-2.12-9.36L23 10\"></path>");

    /// <summary>
    /// 构建内联 SVG 图标 RenderFragment。内部内容为固定字面量（非用户输入），
    /// 以 MarkupString 注入避免额外转义。
    /// </summary>
    private static RenderFragment BuildSvgIcon(string innerMarkup)
    {
        return builder =>
        {
            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "class", "ast-rt-icon");
            builder.AddAttribute(2, "viewBox", "0 0 24 24");
            builder.AddAttribute(3, "fill", "none");
            builder.AddAttribute(4, "stroke", "currentColor");
            builder.AddAttribute(5, "stroke-width", "2");
            builder.AddAttribute(6, "stroke-linecap", "round");
            builder.AddAttribute(7, "stroke-linejoin", "round");
            builder.AddContent(8, (MarkupString)innerMarkup);
            builder.CloseElement();
        };
    }
}
