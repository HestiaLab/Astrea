using Microsoft.JSInterop;

namespace Astrea.Components.Tree;

public sealed partial class Tree
{
    private const double previewOffset = 20;

    private List<TreeNode>? _dragNodes;
    private bool isPreview;
    private int previewCount;
    private double PreviewX;
    private double PreviewY;
    private IJSObjectReference? _dropModule;

    private async Task ToggleNode(TreeNode node)
    {
        if (_controller is null)
        {
            return;
        }

        if (node.IsExpanded)
        {
            if (_controller.Collapse(node))
            {
                await OnCollapse.InvokeAsync(node);
            }
        }
        else
        {
            if (_controller.Expand(node))
            {
                await OnExpand.InvokeAsync(node);
            }
        }

        StateHasChanged();
    }

    private Task HandleDragStart()
    {
        _dragNodes = _controller?.GetSelectedNodes().ToList() ?? new List<TreeNode>();
        return Task.CompletedTask;
    }

    private Task HandleDragEnd()
    {
        _dragNodes = null;
        return Task.CompletedTask;
    }

    /// <summary>JS 拖放回调：校验（内置规则 + BeforeDrop）后发出放置通知，不执行移动。</summary>
    [JSInvokable]
    public async Task HandleNodesDrop(List<string> _, string targetId, string position)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var handled = false;
        try
        {
            if (_controller is null || _dragNodes is null || _dragNodes.Count == 0)
            {
                return;
            }

            handled = true;

            var target = _controller.FindById(targetId);
            if (target is null)
            {
                return;
            }

            var dragPosition = DragPosition.Parse(position);

            if (!_controller.CanDrop(_dragNodes, target, dragPosition))
            {
                return;
            }

            await OnDrop.InvokeAsync(new DragNodesArgs(_dragNodes, target, dragPosition));
        }
        finally
        {
            sw.Stop();
            if (handled)
            {
                await OnTiming.InvokeAsync(("drop", sw.Elapsed.TotalMilliseconds));
            }
        }
    }

    [JSInvokable]
    public void ShowPreview(List<string> selectedNodes, double clientX, double clientY)
    {
        isPreview = true;
        previewCount = selectedNodes.Count;
        PreviewX = clientX + previewOffset;
        PreviewY = clientY;
        StateHasChanged();
    }

    [JSInvokable]
    public void UpdatePreviewPos(double clientX, double clientY)
    {
        PreviewX = clientX + previewOffset;
        PreviewY = clientY;
        StateHasChanged();
    }

    [JSInvokable]
    public void HidePreview()
    {
        isPreview = false;
        StateHasChanged();
    }
}
