using RestDraft.Application.Settings;
using RestDraft.Domain.Results;

namespace RestDraft.UI.ViewModels;

// The sizes of the Collections and Response panels, and whether they and the top bar are shown. The widths are
// stored in the settings when a splitter is released, and what is shown and the size and state of the window when
// the window closes. All come back at the next start. Hiding a panel keeps its width.
public sealed class WorkspaceLayoutViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private double _sidebarWidth = AppSettings.Default.SidebarWidth;
    private double _responseWidth = AppSettings.Default.ResponseWidth;
    private bool _isSidebarVisible = true;
    private bool _isResponseVisible = true;
    private bool _isTopBarVisible = true;
    private bool _visibilityRestored;
    private double _windowWidth = AppSettings.Default.WindowWidth;
    private double _windowHeight = AppSettings.Default.WindowHeight;
    private bool _isWindowMaximized;

    public WorkspaceLayoutViewModel(ISettingsService settings)
    {
        _settings = settings;
    }

    public double SidebarWidth
    {
        get => _sidebarWidth;
        private set => SetProperty(ref _sidebarWidth, value);
    }

    public double ResponseWidth
    {
        get => _responseWidth;
        private set => SetProperty(ref _responseWidth, value);
    }

    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set => SetProperty(ref _isSidebarVisible, value);
    }

    public bool IsResponseVisible
    {
        get => _isResponseVisible;
        set => SetProperty(ref _isResponseVisible, value);
    }

    // The bar with the secrets button and the environment.
    public bool IsTopBarVisible
    {
        get => _isTopBarVisible;
        set => SetProperty(ref _isTopBarVisible, value);
    }

    // The size of the window when it is not maximized. The window sets them from its own size while it changes,
    // and they are stored when it closes.
    public double WindowWidth
    {
        get => _windowWidth;
        private set => SetProperty(ref _windowWidth, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        private set => SetProperty(ref _windowHeight, value);
    }

    public bool IsWindowMaximized
    {
        get => _isWindowMaximized;
        private set => SetProperty(ref _isWindowMaximized, value);
    }

    // The widths are taken every time. What is shown and the state of the window are taken only the first time:
    // after that the View menu and the window hold the current state, and it is stored when the window closes,
    // so a later call (after the Settings dialog) must not bring back the old state.
    public void Apply(AppSettings settings)
    {
        SidebarWidth = settings.SidebarWidth;
        ResponseWidth = settings.ResponseWidth;
        if (_visibilityRestored)
        {
            return;
        }

        _visibilityRestored = true;
        WindowWidth = settings.WindowWidth;
        WindowHeight = settings.WindowHeight;
        IsWindowMaximized = settings.WindowMaximized;
        IsSidebarVisible = settings.SidebarVisible;
        IsResponseVisible = settings.ResponseVisible;
        IsTopBarVisible = settings.TopBarVisible;
    }

    public Task<Result> RememberVisibilityAsync()
    {
        return StoreAsync();
    }

    // The size is the one of the normal window. A maximized window keeps the size it had before.
    public Task<Result> RememberWindowAsync(double normalWidth, double normalHeight, bool isMaximized)
    {
        WindowWidth = Math.Clamp(Math.Round(normalWidth), AppSettingsValidator.MinWindowWidth, AppSettingsValidator.MaxWindowSize);
        WindowHeight = Math.Clamp(Math.Round(normalHeight), AppSettingsValidator.MinWindowHeight, AppSettingsValidator.MaxWindowSize);
        IsWindowMaximized = isMaximized;
        return StoreAsync();
    }

    public Task<Result> RememberSidebarWidthAsync(double width)
    {
        SidebarWidth = Clamp(width);
        return StoreAsync();
    }

    public Task<Result> RememberResponseWidthAsync(double width)
    {
        ResponseWidth = Clamp(width);
        return StoreAsync();
    }

    private static double Clamp(double width)
    {
        return Math.Clamp(Math.Round(width), AppSettingsValidator.MinPanelWidth, AppSettingsValidator.MaxPanelWidth);
    }

    private async Task<Result> StoreAsync()
    {
        var current = await _settings.GetAsync(CancellationToken.None);
        if (!current.IsSuccess)
        {
            return Result.Failure(current.Error!);
        }

        var updated = current.Value! with
        {
            SidebarWidth = (int)SidebarWidth,
            ResponseWidth = (int)ResponseWidth,
            SidebarVisible = IsSidebarVisible,
            ResponseVisible = IsResponseVisible,
            TopBarVisible = IsTopBarVisible,
            WindowWidth = (int)WindowWidth,
            WindowHeight = (int)WindowHeight,
            WindowMaximized = IsWindowMaximized,
        };
        return await _settings.SaveAsync(updated, CancellationToken.None);
    }
}
