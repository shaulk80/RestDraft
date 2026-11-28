using System.Windows.Input;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Results;
using RestDraft.UI.Commands;
using RestDraft.UI.Services;

namespace RestDraft.UI.ViewModels;

// The first step of an import from an address: the address, optional headers, and a choice when a page offers several
// specifications. It ends when a specification has been read; the preview is the second step.
public sealed class ImportUrlViewModel : ViewModelBase
{
    private const string DownloadingStatus = "Downloading...";

    private readonly IRemoteImportService _import;
    private readonly AsyncRelayCommand _nextCommand;
    private CancellationTokenSource? _download;
    private string _address;
    private string _status = string.Empty;
    private string _error = string.Empty;
    private IReadOnlyList<SpecificationLink> _choices = [];
    private SpecificationLink? _selectedChoice;
    private bool _isBusy;

    public ImportUrlViewModel(IRemoteImportService import, string address)
    {
        _import = import;
        _address = address;
        _nextCommand = new AsyncRelayCommand(NextAsync, () => !IsBusy);
    }

    // Raised when a specification has been read. The window closes with it.
    public event Action<ImportUrlResult>? Found;

    // Typed for this download only. They are never stored.
    public KeyValueEditorViewModel Headers { get; } = new([], "Header");

    // The choices belong to the page of the typed address, so another address drops them.
    public string Address
    {
        get => _address;
        set
        {
            if (SetProperty(ref _address, value ?? string.Empty))
            {
                ClearChoices();
            }
        }
    }

    public IReadOnlyList<SpecificationLink> Choices
    {
        get => _choices;
        private set
        {
            if (SetProperty(ref _choices, value))
            {
                RaisePropertyChanged(nameof(HasChoices));
            }
        }
    }

    public bool HasChoices => Choices.Count > 0;

    public SpecificationLink? SelectedChoice
    {
        get => _selectedChoice;
        set => SetProperty(ref _selectedChoice, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _nextCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // What the dialog is doing or waiting for.
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    // Why the last download led nowhere.
    public string Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    public ICommand NextCommand => _nextCommand;

    public async Task NextAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var typed = WebAddress.Parse(Address);
        if (!typed.IsSuccess)
        {
            Error = ErrorMessageFormatter.Format(typed.Error!);
            return;
        }

        var target = SelectedChoice?.Address ?? typed.Value!;
        var headers = new DownloadHeaders(Headers.ToEntries(), typed.Value!);
        var read = await DownloadAsync(target, headers);
        if (read is not null)
        {
            Show(read, typed.Value!);
        }
    }

    // Stops a running download. False when none was running, so the dialog may close.
    public bool CancelDownload()
    {
        if (_download is null)
        {
            return false;
        }

        _download.Cancel();
        return true;
    }

    // Null when the download was cancelled.
    private async Task<Result<RemoteImport>?> DownloadAsync(Uri address, DownloadHeaders headers)
    {
        using var download = new CancellationTokenSource();
        _download = download;
        IsBusy = true;
        Error = string.Empty;
        Status = DownloadingStatus;

        // Reading a large specification takes a while, so it does not run on the UI thread.
        var read = await Task.Run(() => _import.ReadAsync(address, headers, download.Token));
        _download = null;
        IsBusy = false;
        Status = string.Empty;
        if (!download.IsCancellationRequested)
        {
            return read;
        }

        Error = DownloadErrors.Cancelled.Message;
        return null;
    }

    private void Show(Result<RemoteImport> read, Uri typed)
    {
        if (!read.IsSuccess)
        {
            Error = ErrorMessageFormatter.Format(read.Error!);
            return;
        }

        switch (read.Value)
        {
            case SpecificationChoices offered:
                Choices = offered.Links;
                SelectedChoice = offered.Links[0];
                Status = $"The page offers {offered.Links.Count} specifications. Choose one and press Next.";
                break;
            case FoundSpecification found:
                Found?.Invoke(new ImportUrlResult(found.Document, found.Address, typed));
                break;
        }
    }

    private void ClearChoices()
    {
        SelectedChoice = null;
        Choices = [];
        Status = string.Empty;
    }
}
