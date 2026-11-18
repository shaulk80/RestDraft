using Avalonia.Controls;
using Avalonia.Interactivity;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Views;

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow()
    {
        InitializeComponent();
    }

    private ImportPreviewViewModel? ViewModel => DataContext as ImportPreviewViewModel;

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        Close(ViewModel?.Request());
    }
}
