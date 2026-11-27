using System.ComponentModel;

namespace RestDraft.UI.ViewModels;

// While tracking is on, selects the request of the active tab in the Collections tree: when another tab becomes
// active, when tracking is switched on, and after the filter changes. A reload of the tree for another reason is not
// followed, so it keeps its own selection. A tab that is not in a collection leaves the tree as it is.
public sealed class ActiveRequestTracker
{
    private readonly CollectionsTreeViewModel _sidebar;
    private readonly TabStripViewModel _tabStrip;
    private readonly WorkspaceLayoutViewModel _layout;
    private RequestTabViewModel? _activeTab;

    public ActiveRequestTracker(CollectionsTreeViewModel sidebar, TabStripViewModel tabStrip, WorkspaceLayoutViewModel layout)
    {
        _sidebar = sidebar;
        _tabStrip = tabStrip;
        _layout = layout;
    }

    // Listens for as long as the three view models live.
    public void Start()
    {
        _tabStrip.ActiveTabChanged += OnActiveTabChanged;
        _layout.PropertyChanged += OnLayoutChanged;
        _sidebar.Search.Applied += OnSearchApplied;
    }

    private void RevealActiveRequest()
    {
        if (_layout.IsTrackingActiveRequest && _tabStrip.SelectedTab is { IsLinked: true } tab)
        {
            _sidebar.Reveal(tab.RequestId);
        }
    }

    // A tab that is replaced by another tab of the same request, after an import, is not a change of the active one.
    private void OnActiveTabChanged(object? sender, EventArgs e)
    {
        var previous = _activeTab;
        _activeTab = _tabStrip.SelectedTab;
        if (!ShowSameRequest(previous, _activeTab))
        {
            RevealActiveRequest();
        }
    }

    private static bool ShowSameRequest(RequestTabViewModel? first, RequestTabViewModel? second)
    {
        return first is { IsLinked: true } && second is { IsLinked: true } && first.RequestId == second.RequestId;
    }

    private void OnLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceLayoutViewModel.IsTrackingActiveRequest))
        {
            RevealActiveRequest();
        }
    }

    private void OnSearchApplied(object? sender, EventArgs e)
    {
        RevealActiveRequest();
    }
}
