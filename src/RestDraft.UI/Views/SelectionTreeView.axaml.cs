using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Views;

public partial class SelectionTreeView : UserControl
{
    public SelectionTreeView()
    {
        InitializeComponent();
        Tree.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || Tree.SelectedItem is not SelectableNodeViewModel node)
        {
            return;
        }

        node.IsChecked = node.IsChecked != true;
        e.Handled = true;
    }

    // Two quick clicks on the box toggle it twice. The tree row must not also take them as a double-click that
    // expands or collapses the node.
    private void OnCheckBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        e.Handled = true;
    }
}
