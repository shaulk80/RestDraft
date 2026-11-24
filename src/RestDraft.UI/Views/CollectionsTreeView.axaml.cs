using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RestDraft.UI.Behaviors;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Views;

public partial class CollectionsTreeView : UserControl
{
    public static readonly DataFormat<TreeNodeViewModel> NodeFormat = DataFormat.CreateInProcessFormat<TreeNodeViewModel>("RestDraft.TreeNode");

    private readonly DragStarter<TreeNodeViewModel> _drag = new(NodeFormat);
    private readonly DropIndicator _dropIndicator;
    private TopLevel? _topLevel;

    public CollectionsTreeView()
    {
        InitializeComponent();
        _dropIndicator = new DropIndicator(DropIndicatorBorder, ContentGrid);
        DragDrop.SetAllowDrop(this, true);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DoubleTappedEvent, OnDoubleTapped);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        MenuItems.AddHandler(MenuItem.ClickEvent, OnMenuItemClick);
        MenuItems.AddHandler(PointerPressedEvent, OnMenuPointerPressed, RoutingStrategies.Tunnel);
    }

    private CollectionsTreeViewModel? ViewModel => DataContext as CollectionsTreeViewModel;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    // Any press in the window closes the menu first. The tree's own handler then runs, and a
    // right-press on a row opens the menu for that row again.
    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsMenuPress(e.Source))
        {
            return;
        }

        CloseMenu();
    }

    private void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        CloseMenu();
        Tree.Focus();
    }

    // Escape closes the context menu and returns to the tree. Shift+F10 and the Menu key open it for the
    // selected node, as on any Windows list. Delete deletes the selected node, except in the rename box,
    // where it is an editing key (a KeyBinding on the tree would take it from the box).
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && e.Source is not TextBox && ViewModel?.DeleteSelectedCommand is { } delete)
        {
            e.Handled = true;
            delete.Execute(null);
            return;
        }

        if (e.Key == Key.Escape && MenuPopup.IsOpen)
        {
            e.Handled = true;
            CloseMenu();
            Tree.Focus();
            return;
        }

        var opensMenu = e.Key == Key.Apps || (e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.Shift);
        if (opensMenu && ViewModel?.SelectedNode is { IsEditing: false } node)
        {
            e.Handled = true;
            OpenMenu(node, Tree.ContainerFromItem(node) as Control, focusMenu: true);
        }
    }

    // The menu covers the rows below the pointer. A right-press on one of those rows must still select
    // it and move the menu there, so the press is resolved against the tree instead of the menu.
    private void OnMenuPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(MenuItems).Properties.IsRightButtonPressed)
        {
            return;
        }

        var treePoint = e.GetPosition(this);
        if (this.InputHitTest(treePoint) is not { } hit || NodeOf(hit) is not { } node)
        {
            return;
        }

        e.Handled = true;
        OpenMenu(node, hit as Control);
    }

    private void OpenMenu(TreeNodeViewModel node, Control? target, bool focusMenu = false)
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.SelectedNode = node;
        MenuPopup.IsOpen = false;
        MenuPopup.DataContext = node;
        MenuPopup.PlacementTarget = target ?? this;
        MenuPopup.IsOpen = true;
        if (focusMenu)
        {
            Dispatcher.UIThread.Post(FocusFirstMenuItem);
        }
    }

    private void FocusFirstMenuItem()
    {
        MenuItems.Items.OfType<MenuItem>().FirstOrDefault(item => item.IsVisible)?.Focus();
    }

    private void CloseMenu()
    {
        MenuPopup.IsOpen = false;
    }

    private static TreeNodeViewModel? NodeOf(object? source)
    {
        return (source as StyledElement)?.DataContext as TreeNodeViewModel;
    }

    // Presses that start inside the menu belong to the menu: a click must reach the menu item, and must
    // not close the menu or start a drag from the tree node the menu happens to be bound to.
    private static bool IsMenuPress(object? source)
    {
        return source is Visual visual && visual.FindAncestorOfType<MenuFlyoutPresenter>(includeSelf: true) is not null;
    }

    // A press on the expander must stay a click. A drag started from it would capture the pointer,
    // and the expand would be lost.
    private static bool IsExpanderPress(object? source)
    {
        return source is Visual visual && visual.FindAncestorOfType<ToggleButton>(includeSelf: true) is not null;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsMenuPress(e.Source))
        {
            _drag.Reset();
            return;
        }

        var node = NodeOf(e.Source);
        var properties = e.GetCurrentPoint(this).Properties;
        if (node is not null && !node.IsEditing && properties.IsRightButtonPressed)
        {
            _drag.Reset();
            OpenMenu(node, e.Source as Control);
            return;
        }

        if (node is null || node.IsEditing || IsExpanderPress(e.Source) || !properties.IsLeftButtonPressed)
        {
            _drag.Reset();
            return;
        }

        _drag.Press(node, e, e.GetPosition(this));
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        await _drag.MoveAsync(e.GetPosition(this));
    }

    // A press that was released without a drag is a click. With the single-click setting on, it opens a request.
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var clicked = _drag.Release();
        if (clicked is RequestNodeViewModel request && ViewModel is { OpensOnSingleClick: true } viewModel)
        {
            viewModel.OpenRequest(request.Id);
        }
    }

    // A drop is allowed on any row, except a row inside the dragged node itself. The part of the row under the
    // pointer says whether the node goes before it, inside it or after it, and the indicator shows which.
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;
        _dropIndicator.Hide();
        var source = e.DataTransfer.TryGetValue(NodeFormat);
        if (source is null || DropRow(e, source) is not { } row)
        {
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        ShowDropIndicator(row, PositionAt(e, row));
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _dropIndicator.Hide();
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        _dropIndicator.Hide();
        var source = e.DataTransfer.TryGetValue(NodeFormat);
        if (source is null || ViewModel is null || DropRow(e, source) is not { DataContext: TreeNodeViewModel target } row)
        {
            return;
        }

        e.Handled = true;
        await ViewModel.MoveAsync(source, target, PositionAt(e, row));
    }

    private static TreeViewItem? DropRow(DragEventArgs e, TreeNodeViewModel source)
    {
        var row = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(includeSelf: true);
        if (row?.DataContext is not TreeNodeViewModel target || !DropPlanner.CanDrop(source, target))
        {
            return null;
        }

        return row;
    }

    // The part of a row that shows its own content, without the rows of its children below it.
    private static Grid? HeaderOf(TreeViewItem row)
    {
        return row.GetVisualDescendants().OfType<Grid>().FirstOrDefault(grid => grid.Name == "PART_Header");
    }

    private static DropPosition PositionAt(DragEventArgs e, TreeViewItem row)
    {
        if (row.DataContext is not TreeNodeViewModel target || HeaderOf(row) is not { } header)
        {
            return DropPosition.Inside;
        }

        var y = Math.Clamp(e.GetPosition(header).Y, 0, header.Bounds.Height);
        return DropZones.Resolve(target, y, header.Bounds.Height);
    }

    private void ShowDropIndicator(TreeViewItem row, DropPosition position)
    {
        if (HeaderOf(row) is not { } header)
        {
            return;
        }

        switch (position)
        {
            case DropPosition.Before:
                _dropIndicator.ShowHorizontalLine(header, atBottom: false);
                break;
            case DropPosition.After:
                _dropIndicator.ShowHorizontalLine(header, atBottom: true);
                break;
            default:
                _dropIndicator.ShowFrame(header);
                break;
        }
    }

    // With the single-click setting on, the first click has opened the request already.
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel is { OpensOnSingleClick: false } viewModel && NodeOf(e.Source) is RequestNodeViewModel request)
        {
            viewModel.OpenRequest(request.Id);
        }
    }
}
