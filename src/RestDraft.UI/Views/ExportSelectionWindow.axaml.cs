using Avalonia.Controls;
using Avalonia.Interactivity;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Views;

public partial class ExportSelectionWindow : Window
{
    public ExportSelectionWindow()
    {
        InitializeComponent();
    }

    private ExportSelectionViewModel? ViewModel => DataContext as ExportSelectionViewModel;

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnExportClick(object? sender, RoutedEventArgs e)
    {
        Close(ViewModel?.Selection());
    }
}
