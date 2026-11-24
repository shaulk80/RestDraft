using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Behaviors;

// Lets the user drag the tabs of a request tab strip to a new place. A press on a tab, not on its close button, that
// moves a few pixels starts the drag. A line at the left or right edge of the tab under the pointer shows where the tab
// will go. A tab dropped on the empty part of the strip goes to the end.
public sealed class TabReorder
{
    public static readonly DataFormat<RequestTabViewModel> TabFormat = DataFormat.CreateInProcessFormat<RequestTabViewModel>("RestDraft.Tab");

    private readonly TabControl _strip;
    private readonly Func<TabStripViewModel?> _viewModel;
    private readonly DragStarter<RequestTabViewModel> _drag = new(TabFormat);
    private readonly DropIndicator _indicator;

    public TabReorder(TabControl strip, Func<TabStripViewModel?> viewModel, DropIndicator indicator)
    {
        _strip = strip;
        _viewModel = viewModel;
        _indicator = indicator;
        DragDrop.SetAllowDrop(strip, true);
        strip.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        strip.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        strip.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        strip.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        strip.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        strip.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private static TabItem? TabItemOf(object? source)
    {
        return (source as Visual)?.FindAncestorOfType<TabItem>(includeSelf: true);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var isOnCloseButton = (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is not null;
        var isLeftButton = e.GetCurrentPoint(_strip).Properties.IsLeftButtonPressed;
        if (isOnCloseButton || !isLeftButton || TabItemOf(e.Source)?.DataContext is not RequestTabViewModel tab)
        {
            _drag.Reset();
            return;
        }

        _drag.Press(tab, e, e.GetPosition(_strip));
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(_strip).Properties.IsLeftButtonPressed)
        {
            return;
        }

        await _drag.MoveAsync(e.GetPosition(_strip));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _drag.Reset();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;
        _indicator.Hide();
        var source = e.DataTransfer.TryGetValue(TabFormat);
        if (source is null || _viewModel() is not { } viewModel || FindDrop(e, viewModel, source) is not { } drop)
        {
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        _indicator.ShowVerticalLine(drop.Anchor, drop.AfterAnchor);
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _indicator.Hide();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        _indicator.Hide();
        var source = e.DataTransfer.TryGetValue(TabFormat);
        if (source is null || _viewModel() is not { } viewModel || FindDrop(e, viewModel, source) is not { } drop)
        {
            return;
        }

        e.Handled = true;
        viewModel.MoveTab(source, drop.NewIndex);
    }

    // The tab the pointer is over and the half of it, or the end of the strip when the pointer is not over a tab.
    // Null when the drop would leave the tab where it is.
    private DropTarget? FindDrop(DragEventArgs e, TabStripViewModel viewModel, RequestTabViewModel source)
    {
        var sourceIndex = viewModel.Tabs.IndexOf(source);
        var item = TabItemOf(e.Source);
        var afterAnchor = true;
        if (item?.DataContext is RequestTabViewModel)
        {
            afterAnchor = e.GetPosition(item).X > item.Bounds.Width / 2;
        }
        else
        {
            item = _strip.GetVisualDescendants().OfType<TabItem>().LastOrDefault();
        }

        if (sourceIndex < 0 || item?.DataContext is not RequestTabViewModel target)
        {
            return null;
        }

        var newIndex = TabDropIndex.Of(sourceIndex, viewModel.Tabs.IndexOf(target), afterAnchor);
        return newIndex == sourceIndex ? null : new DropTarget(item, afterAnchor, newIndex);
    }

    private sealed record DropTarget(TabItem Anchor, bool AfterAnchor, int NewIndex);
}
