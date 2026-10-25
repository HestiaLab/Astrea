using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Astrea.Components.ResourceTable;

public sealed partial class ResourceTable
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var componentClass = $"ast-resource-table {(ContainerCss ?? "ast-rt-container")}";
        var mergedClass = componentClass;
        var extraAttributes = new List<KeyValuePair<string, object>>();
        if (AdditionalAttributes is not null)
        {
            foreach (var attribute in AdditionalAttributes)
            {
                if (attribute.Key == "class")
                {
                    if (attribute.Value?.ToString() is { Length: > 0 } appClass)
                    {
                        mergedClass = $"{componentClass} {appClass}";
                    }
                }
                else if (attribute.Key is not "tabindex" and not "onkeydown")
                {
                    extraAttributes.Add(attribute);
                }
            }
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", mergedClass);
        if (extraAttributes.Count > 0)
        {
            builder.AddMultipleAttributes(2, extraAttributes);
        }

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
                builder.AddContent(22, BuildButton(IconAdd ?? DefaultIcon_Add, OnAddClicked, AddButtonLabel));
                builder.AddContent(23, BuildButton(IconRemove ?? DefaultIcon_Remove, OnRemoveClicked, RemoveButtonLabel));
                builder.AddContent(24, BuildButton(IconMoveUp ?? DefaultIcon_MoveUp, OnMoveUpClicked, MoveUpButtonLabel));
                builder.AddContent(25, BuildButton(IconMoveDown ?? DefaultIcon_MoveDown, OnMoveDownClicked, MoveDownButtonLabel));

                if (ShowRedoButton)
                {
                    builder.OpenElement(26, "div");
                    builder.AddAttribute(27, "class", "ast-rt-divider");
                    builder.CloseElement();

                    builder.AddContent(28, BuildButton(IconUndo ?? DefaultIcon_Undo, OnUndoClicked, UndoButtonLabel, !CanUndo));
                    builder.AddContent(29, BuildButton(IconRedo ?? DefaultIcon_Redo, OnRedoClicked, RedoButtonLabel, !CanRedo));
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

        // 表头：空列集合不渲染空 th/thead（REQ-F-096）
        if (HasColumns)
        {
            builder.OpenElement(70, "thead");
            builder.OpenElement(71, "tr");
            for (var i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];
                var index = i;
                builder.OpenElement(80 + i * 10, "th");
                builder.AddAttribute(81 + i * 10, "class", "ast-rt-th");
                builder.AddAttribute(82 + i * 10, "scope", "col");
                if (column.Width is not null)
                {
                    builder.AddAttribute(83 + i * 10, "style", $"width:{column.Width};");
                }

                builder.OpenElement(84 + i * 10, "div");
                builder.AddAttribute(85 + i * 10, "class", "ast-rt-th-content");
                builder.AddContent(86 + i * 10, column.Title);
                builder.CloseElement();

                if (ResizableColumns && index != _columns.Count - 1 && column.Resizable)
                {
                    var initialValue = ParseColumnWidth(column);
                    var minValue = column.MinWidth ?? 50;
                    builder.OpenElement(87 + i * 10, "div");
                    builder.AddAttribute(88 + i * 10, "class", "ast-rt-resize-handle");
                    builder.AddAttribute(89 + i * 10, "role", "separator");
                    builder.AddAttribute(90 + i * 10, "aria-orientation", "vertical");
                    builder.AddAttribute(91 + i * 10, "tabindex", "0");
                    builder.AddAttribute(92 + i * 10, "aria-label", string.Format(CultureInfo.CurrentCulture, ResizeHandleLabelFormat, column.Title));
                    builder.AddAttribute(93 + i * 10, "data-column-index", index.ToString());
                    builder.AddAttribute(94 + i * 10, "aria-valuemin", FormatNumber(minValue));
                    builder.AddAttribute(95 + i * 10, "aria-valuemax", FormatNumber(Math.Max(10000, initialValue)));
                    builder.AddAttribute(96 + i * 10, "aria-valuenow", FormatNumber(initialValue));
                    builder.CloseElement();
                }

                builder.CloseElement();
            }

            builder.CloseElement();
            builder.CloseElement();
        }

        // 表体
        builder.OpenElement(90, "tbody");
        builder.AddContent(91, TableBody);
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement();
    }

    private static string FormatNumber(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static double ParseColumnWidth(TableColumn column)
    {
        if (column.Width is { Length: > 2 } width &&
            width.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(width[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            return parsed;
        }

        return column.MinWidth ?? 50;
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
            builder.AddAttribute(8, "aria-hidden", "true");
            builder.AddContent(9, (MarkupString)innerMarkup);
            builder.CloseElement();
        };
    }
}
