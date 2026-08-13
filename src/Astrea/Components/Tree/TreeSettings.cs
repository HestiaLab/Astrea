using Microsoft.AspNetCore.Components;

namespace Astrea.Components.Tree;

/// <summary>
/// Tree 组件配置。
/// </summary>
public sealed class TreeSettings
{
    /// <summary>是否允许拖拽。</summary>
    public bool AllowDrag { get; set; }

    /// <summary>是否允许多选（Ctrl/Shift）。</summary>
    public bool AllowMultiSelect { get; set; }

    /// <summary>选中时是否仅高亮文字。</summary>
    public bool OnlyHighlightText { get; set; } = true;

    /// <summary>隐藏根节点（差异根场景）。</summary>
    public bool HideRootNode { get; set; }

    /// <summary>是否启用虚拟化。</summary>
    public bool EnableVirtualization { get; set; } = true;

    /// <summary>是否使用内部键盘事件处理（ARIA 方向键/Home/End/Space/Enter 导航）。</summary>
    public bool UseInternalKeyboard { get; set; } = true;

    /// <summary>是否显示父子连接线。</summary>
    public bool ShowLink { get; set; } = true;

    /// <summary>每级缩进像素。</summary>
    public int Indent { get; set; } = 20;

    /// <summary>虚拟化单项高度。</summary>
    public float ItemSize { get; set; } = 32f;

    /// <summary>节点文字自定义模板。</summary>
    public RenderFragment<TreeNode>? NodeTemplate { get; set; }

    /// <summary>节点编辑模板（替代默认输入框）。</summary>
    public RenderFragment<TreeNode>? EditTemplate { get; set; }
}
