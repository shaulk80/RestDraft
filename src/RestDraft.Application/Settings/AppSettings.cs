namespace RestDraft.Application.Settings;

// Application settings. They apply to the next send or the next click, so no restart is needed.
public sealed record AppSettings
{
    public static readonly AppSettings Default = new();

    public int TimeoutSeconds { get; init; } = 30;

    public bool FollowRedirects { get; init; } = true;

    public int MaxRedirects { get; init; } = 10;

    public bool VerifySsl { get; init; } = true;

    // Empty means the system proxy.
    public string ProxyUrl { get; init; } = string.Empty;

    // The Collections tree opens a request on a single click. Off means a double-click.
    public bool OpenOnSingleClick { get; init; }

    // The color theme. System follows the operating system.
    public ThemePreference Theme { get; init; } = ThemePreference.System;

    // Panel widths in pixels. Remembered when a splitter is dragged.
    public int SidebarWidth { get; init; } = 260;

    public int ResponseWidth { get; init; } = 420;

    // The View menu: which parts of the window are shown. Remembered when the window closes.
    public bool SidebarVisible { get; init; } = true;

    public bool ResponseVisible { get; init; } = true;

    public bool TopBarVisible { get; init; } = true;

    // The View menu: the Collections tree selects the request of the active tab. Remembered when the window closes.
    public bool TrackActiveRequest { get; init; }

    // The size of the main window when it is not maximized, and whether it is maximized. Remembered when the window closes.
    public int WindowWidth { get; init; } = 1200;

    public int WindowHeight { get; init; } = 720;

    public bool WindowMaximized { get; init; }

    // The address of the last import from an address, without a user name or password, and the base URL used with it.
    // Remembered after a successful import.
    public string LastImportUrl { get; init; } = string.Empty;

    public string LastImportBaseUrl { get; init; } = string.Empty;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
