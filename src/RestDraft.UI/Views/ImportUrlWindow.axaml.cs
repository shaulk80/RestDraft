using Avalonia.Controls;
using Avalonia.Interactivity;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Views;

public partial class ImportUrlWindow : Window
{
    public ImportUrlWindow()
    {
        InitializeComponent();
    }

    private ImportUrlViewModel? ViewModel => DataContext as ImportUrlViewModel;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (ViewModel is { } viewModel)
        {
            viewModel.Found += OnFound;
        }
    }

    // A download does not outlive its dialog.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        ViewModel?.CancelDownload();
    }

    private void OnFound(ImportUrlResult result)
    {
        Close(result);
    }

    // While a download runs, Cancel stops it and the dialog stays, so the address can be corrected.
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.CancelDownload() != true)
        {
            Close(null);
        }
    }
}
