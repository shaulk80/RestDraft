using System.Windows.Input;
using RestDraft.Application.Settings;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Commands;
using RestDraft.UI.Services;

namespace RestDraft.UI.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const int HistoryTabIndex = 1;

    private readonly ISettingsService _settings;
    private readonly ISettingsDialogs _settingsDialogs;
    private readonly IExchangeDialogs _exchangeDialogs;
    private readonly AsyncRelayCommand _settingsCommand;
    private readonly AsyncRelayCommand _importCommand;
    private readonly AsyncRelayCommand _importFromUrlCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private ThemePreference _theme = AppSettings.Default.Theme;
    private int _sidebarTabIndex;
    private string _notice = string.Empty;

    public MainWindowViewModel(
        CollectionsTreeViewModel sidebar,
        HistoryListViewModel history,
        TabStripViewModel tabStrip,
        EnvironmentSelectorViewModel environments,
        SecretStatusViewModel secrets,
        WorkspaceLayoutViewModel layout,
        ISettingsService settings,
        ISettingsDialogs settingsDialogs,
        IExchangeDialogs exchangeDialogs)
    {
        Sidebar = sidebar;
        History = history;
        TabStrip = tabStrip;
        Environments = environments;
        Secrets = secrets;
        Layout = layout;
        _settings = settings;
        _settingsDialogs = settingsDialogs;
        _exchangeDialogs = exchangeDialogs;
        _settingsCommand = new AsyncRelayCommand(ShowSettingsAsync, () => true);
        _importCommand = new AsyncRelayCommand(ImportAsync, () => true);
        _importFromUrlCommand = new AsyncRelayCommand(ImportFromUrlAsync, () => true);
        _exportCommand = new AsyncRelayCommand(ExportAsync, () => Sidebar.HasCollections);
        Sidebar.RequestOpenRequested += OnRequestOpenRequested;
        Sidebar.RequestCreated += OnRequestCreated;
        Sidebar.TreeChanged += OnTreeChanged;
        History.RestoreRequested += TabStrip.OpenUnsaved;
        TabStrip.RequestSent += OnTabRequestSent;
        TabStrip.Linked += OnTabLinked;
        Sidebar.Search.Applied += OnSearchApplied;
        History.Search.Applied += OnSearchApplied;
        new ActiveRequestTracker(Sidebar, TabStrip, Layout).Start();
    }

    // The search popup closes when a search has been applied.
    public event EventHandler? SearchApplied;

    public CollectionsTreeViewModel Sidebar { get; }

    public HistoryListViewModel History { get; }

    public TabStripViewModel TabStrip { get; }

    public EnvironmentSelectorViewModel Environments { get; }

    public SecretStatusViewModel Secrets { get; }

    public WorkspaceLayoutViewModel Layout { get; }

    public ICommand SettingsCommand => _settingsCommand;

    public ICommand ImportCommand => _importCommand;

    public ICommand ImportFromUrlCommand => _importFromUrlCommand;

    // There is nothing to export before the first collection exists.
    public ICommand ExportCommand => _exportCommand;

    // What the last import or export did. Failures go to the status of the tree instead, which is shown as an error.
    public string Notice
    {
        get => _notice;
        private set => SetProperty(ref _notice, value);
    }

    // The tab of the sidebar that is shown: 0 is Collections, 1 is History. Not remembered.
    public int SidebarTabIndex
    {
        get => _sidebarTabIndex;
        set
        {
            if (SetProperty(ref _sidebarTabIndex, value))
            {
                RaisePropertyChanged(nameof(ActiveSearch));
            }
        }
    }

    // One search popup serves both lists. It edits the search of the tab that is shown.
    public SearchViewModel ActiveSearch => SidebarTabIndex == HistoryTabIndex ? History.Search : Sidebar.Search;

    // The window applies it to the application, because view models know no Avalonia types.
    public ThemePreference Theme
    {
        get => _theme;
        private set => SetProperty(ref _theme, value);
    }

    // Takes the settings that the window itself uses: the panel widths, the click that opens a request and the theme.
    public async Task ApplySettingsAsync()
    {
        var loaded = await _settings.GetAsync(CancellationToken.None);
        if (!loaded.IsSuccess)
        {
            Sidebar.StatusMessage = ErrorMessageFormatter.Format(loaded.Error!);
            return;
        }

        Layout.Apply(loaded.Value!);
        Sidebar.OpensOnSingleClick = loaded.Value!.OpenOnSingleClick;
        Theme = loaded.Value!.Theme;
    }

    public async Task RememberSidebarWidthAsync(double width)
    {
        ReportFailure(await Layout.RememberSidebarWidthAsync(width));
    }

    public async Task RememberResponseWidthAsync(double width)
    {
        ReportFailure(await Layout.RememberResponseWidthAsync(width));
    }

    public async Task RememberVisibilityAsync()
    {
        ReportFailure(await Layout.RememberVisibilityAsync());
    }

    public async Task RememberWindowAsync(double normalWidth, double normalHeight, bool isMaximized)
    {
        ReportFailure(await Layout.RememberWindowAsync(normalWidth, normalHeight, isMaximized));
    }

    public async Task OpenRequestAsync(Guid requestId, bool focusEditor = true)
    {
        var opened = await TabStrip.OpenRequestAsync(requestId, focusEditor);
        if (!opened.IsSuccess)
        {
            Sidebar.StatusMessage = ErrorMessageFormatter.Format(opened.Error!);
        }
    }

    public void OpenUnsaved(RequestDefinition definition)
    {
        TabStrip.OpenUnsaved(definition);
    }

    private async Task ShowSettingsAsync()
    {
        await _settingsDialogs.ShowAsync(CancellationToken.None);
        await ApplySettingsAsync();
    }

    private async Task ImportAsync()
    {
        Notice = string.Empty;
        var outcome = await _exchangeDialogs.ImportAsync(Sidebar.SelectedCollectionId, CancellationToken.None);
        await ShowOutcomeAsync(outcome);
    }

    private async Task ImportFromUrlAsync()
    {
        Notice = string.Empty;
        var outcome = await _exchangeDialogs.ImportFromUrlAsync(Sidebar.SelectedCollectionId, CancellationToken.None);
        await ShowOutcomeAsync(outcome);
    }

    private async Task ExportAsync()
    {
        Notice = string.Empty;
        var outcome = await _exchangeDialogs.ExportAsync(Sidebar.SelectedCollectionId, CancellationToken.None);
        await ShowOutcomeAsync(outcome);
    }

    // The tree is reloaded first, because that clears its message line.
    private async Task ShowOutcomeAsync(ExchangeOutcome outcome)
    {
        if (outcome.Changed)
        {
            await Sidebar.ReloadAsync(outcome.CollectionId, outcome.CollectionId);
            await TabStrip.ReloadLinkedAsync(outcome.OverwrittenRequestIds);
        }

        if (outcome.IsError)
        {
            Sidebar.StatusMessage = outcome.Message;
            return;
        }

        Notice = outcome.Message;
    }

    private void ReportFailure(Result result)
    {
        if (!result.IsSuccess)
        {
            Sidebar.StatusMessage = ErrorMessageFormatter.Format(result.Error!);
        }
    }

    private async void OnRequestOpenRequested(Guid requestId)
    {
        await OpenRequestAsync(requestId);
    }

    // The rename starts after the tab is open, so nothing that opening does can take the focus from the name box.
    private async void OnRequestCreated(Guid requestId)
    {
        await OpenRequestAsync(requestId, focusEditor: false);
        Sidebar.BeginRenameOfNew(requestId);
    }

    private async void OnTabRequestSent(object? sender, EventArgs e)
    {
        await History.ReloadAsync();
    }

    private async void OnTabLinked(object? sender, RequestLinkedEventArgs e)
    {
        await Sidebar.ReloadAsync(e.RequestId, e.ExpandId);
    }

    private void OnSearchApplied(object? sender, EventArgs e)
    {
        SearchApplied?.Invoke(this, EventArgs.Empty);
    }

    private void OnTreeChanged(object? sender, EventArgs e)
    {
        _exportCommand.RaiseCanExecuteChanged();
        TabStrip.SyncWithTree(requestId => Sidebar.FindRequestName(requestId));
    }
}
