using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Settings;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Tests.ViewModels;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.Views;

// View > Track active request: the tree follows the active tab.
public partial class MainWindowSmokeTests
{
    private const int FillerCount = 60;

    // Far enough down a long tree, inside a folder that starts closed, that it is off screen until it is scrolled to.
    [AvaloniaTest]
    public async Task TrackingScrollsTheTreeToARequestWhoseFolderWasClosed()
    {
        var deep = new RequestDefinition { Id = Guid.NewGuid(), Name = "Deep", Url = "https://example.com/deep" };
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        OpenWindow(deep);
        var closed = await ShowLongTreeAsync(deep);
        Assert.That(IsOnScreenInTheTree(closed.Id), Is.False, "the folder of the request starts off screen");

        _viewModel.TabStrip.SelectedTab = TabOf(deep);
        await SettleAsync();

        Assert.That(_viewModel.Sidebar.SelectedNode?.Id, Is.EqualTo(deep.Id));
        Assert.That(IsOnScreenInTheTree(deep.Id), Is.True);
    }

    // The request is still selected, so selecting it again changes nothing. It has to be scrolled to all the same.
    [TestCase(true)]
    [TestCase(false)]
    [AvaloniaTest]
    public async Task TrackingScrollsBackToARequestThatIsStillSelected(bool bySwitchingTrackingOn)
    {
        var deep = new RequestDefinition { Id = Guid.NewGuid(), Name = "Deep", Url = "https://example.com/deep" };
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        OpenWindow(deep);
        var closed = await ShowLongTreeAsync(deep);
        _viewModel.TabStrip.SelectedTab = TabOf(deep);
        await SettleAsync();
        _viewModel.Layout.IsTrackingActiveRequest = !bySwitchingTrackingOn;
        await ScrollTreeToTheTopAsync();
        Assert.That(IsOnScreenInTheTree(closed.Id), Is.False, "scrolled away");

        if (bySwitchingTrackingOn)
        {
            _viewModel.Layout.IsTrackingActiveRequest = true;
        }
        else
        {
            _viewModel.TabStrip.NewTab();
            _viewModel.TabStrip.SelectedTab = TabOf(deep);
        }

        await SettleAsync();

        Assert.That(IsOnScreenInTheTree(deep.Id), Is.True);
    }

    // The tab changes while the filtered tree is shown. The unfiltered tree has the folder of the request closed.
    [AvaloniaTest]
    public async Task ClearingTheFilterScrollsToTheActiveRequest()
    {
        var deep = new RequestDefinition { Id = Guid.NewGuid(), Name = "Deep", Url = "https://example.com/deep" };
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        OpenWindow(deep);
        await ShowLongTreeAsync(deep);
        _viewModel.Sidebar.Search.Text = "deep";
        await _viewModel.Sidebar.Search.ApplyAsync();
        _viewModel.TabStrip.SelectedTab = TabOf(deep);
        await SettleAsync();

        _viewModel.Sidebar.Search.ClearCommand.Execute(null);
        await SettleAsync();

        Assert.That(IsOnScreenInTheTree(deep.Id), Is.True);
    }

    // The History tab and a hidden Collections panel stay as they are. The request is there when the tree is shown.
    [TestCase(true)]
    [TestCase(false)]
    [AvaloniaTest]
    public async Task ARequestRevealedWhileTheTreeIsNotShownIsThereWhenItIs(bool behindTheHistoryTab)
    {
        var deep = new RequestDefinition { Id = Guid.NewGuid(), Name = "Deep", Url = "https://example.com/deep" };
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        OpenWindow(deep);
        await ShowLongTreeAsync(deep);
        await SetTreeShownAsync(false, behindTheHistoryTab);

        _viewModel.TabStrip.SelectedTab = TabOf(deep);
        await SettleAsync();

        Assert.That(_viewModel.SidebarTabIndex, Is.EqualTo(behindTheHistoryTab ? HistoryTab : CollectionsTab));
        Assert.That(_viewModel.Layout.IsSidebarVisible, Is.EqualTo(behindTheHistoryTab));
        await SetTreeShownAsync(true, behindTheHistoryTab);
        Assert.That(_viewModel.Sidebar.SelectedNode?.Id, Is.EqualTo(deep.Id));
        Assert.That(IsOnScreenInTheTree(deep.Id), Is.True);
    }

    [AvaloniaTest]
    public async Task TrackingDoesNotMoveTheKeyboardFocus()
    {
        var other = new RequestDefinition { Id = Guid.NewGuid(), Name = "Other", Url = "https://example.com/other" };
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        OpenWindow(other);
        await ShowLongTreeAsync(other);
        var urlBox = FocusUrlBox();

        _window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
        await SettleAsync();

        Assert.That(_viewModel.Sidebar.SelectedNode?.Id, Is.EqualTo(other.Id));
        Assert.That(_window.FocusManager?.GetFocusedElement(), Is.SameAs(urlBox));
    }

    // The real tab strip may select another tab for a moment while a tab is replaced, which must not be followed.
    [AvaloniaTest]
    public async Task ReplacingTheActiveTabAfterAnImportKeepsTheTreeSelection()
    {
        var other = new RequestDefinition { Id = Guid.NewGuid(), Name = "Other", Url = "https://example.com/other" };
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        OpenWindow(other);
        await ShowLongTreeAsync(other);
        _viewModel.TabStrip.SelectedTab = TabOf(other);
        var collection = _viewModel.Sidebar.Roots[0];
        _viewModel.Sidebar.SelectedNode = collection;
        A.CallTo(() => _fixtures.Requests.GetAsync(other.Id, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<RequestDefinition>.Success(other with { Url = "https://example.com/imported" })));

        await _viewModel.TabStrip.ReloadLinkedAsync([other.Id]);
        await SettleAsync();

        Assert.That(_viewModel.TabStrip.SelectedTab?.Url, Is.EqualTo("https://example.com/imported"));
        Assert.That(_viewModel.Sidebar.SelectedNode, Is.SameAs(collection));
    }

    // The tab strip selects a tab by itself while the stored tabs are added, before the stored selection is applied.
    [AvaloniaTest]
    public async Task TrackingAtTheStartOpensOnlyTheFoldersOfTheStoredSelection()
    {
        var first = new RequestDefinition { Id = Guid.NewGuid(), Name = "First", Url = "https://example.com/first" };
        var selected = new RequestDefinition { Id = Guid.NewGuid(), Name = "Selected", Url = "https://example.com/selected" };
        var firstFolder = new Folder(Guid.NewGuid(), "Of first", [], [first]);
        var selectedFolder = new Folder(Guid.NewGuid(), "Of selected", [], [selected]);
        _initialSettings = AppSettings.Default with { TrackActiveRequest = true };
        CreateFixtures();
        var root = new Folder(Guid.NewGuid(), string.Empty, [firstFolder, selectedFolder], []);
        _fixtures.SetCollections([_fixtures.Shop with { Root = root }]);

        ShowWindow([first, selected], selected);
        await SettleAsync();

        var folders = _viewModel.Sidebar.Roots[0].Children;
        Assert.That(_viewModel.Sidebar.SelectedNode?.Id, Is.EqualTo(selected.Id));
        Assert.That(folders.Single(node => node.Id == selectedFolder.Id).IsExpanded, Is.True);
        Assert.That(folders.Single(node => node.Id == firstFolder.Id).IsExpanded, Is.False);
    }

    [AvaloniaTest]
    public async Task ViewMenuSwitchesTrackingAndShowsTheActiveRequestAtOnce()
    {
        OpenWindow();
        await ShowLongTreeAsync();
        var item = FindMenuItem("_Track active request");
        Assert.That(item.IsChecked, Is.False);

        item.IsChecked = true;
        await SettleAsync();

        Assert.That(_viewModel.Layout.IsTrackingActiveRequest, Is.True);
        Assert.That(_viewModel.Sidebar.SelectedNode?.Id, Is.EqualTo(_stored.Id));
    }

    // Many folders ahead of a closed one that holds the given requests, so they start far below what the tree shows.
    // The tree lists folders before requests, and the stored request is in the root, below all of them.
    private async Task<Folder> ShowLongTreeAsync(params RequestDefinition[] deepRequests)
    {
        var fillers = Enumerable.Range(1, FillerCount).Select(number => TreeFixtures.CreateFolder($"Filler {number}"));
        var closed = new Folder(Guid.NewGuid(), "Closed", [], [.. deepRequests]);
        var root = new Folder(Guid.NewGuid(), string.Empty, [.. fillers, closed], [_stored]);
        _fixtures.SetCollections([_fixtures.Shop with { Root = root }]);
        await _viewModel.Sidebar.ReloadAsync();
        _viewModel.Sidebar.Roots[0].IsExpanded = true;
        await SettleAsync();
        return closed;
    }

    private MenuItem FindMenuItem(string header)
    {
        return _window.GetLogicalDescendants().OfType<MenuItem>().Single(item => item.Header as string == header);
    }

    private RequestTabViewModel TabOf(RequestDefinition definition)
    {
        return _viewModel.TabStrip.Tabs.Single(tab => tab.RequestId == definition.Id);
    }

    private async Task SetTreeShownAsync(bool isShown, bool byTheSidebarTab)
    {
        if (byTheSidebarTab)
        {
            ShowSidebarTab(isShown ? CollectionsTab : HistoryTab);
        }
        else
        {
            _viewModel.Layout.IsSidebarVisible = isShown;
        }

        await SettleAsync();
    }

    private TreeView CollectionsTree()
    {
        return _window.GetVisualDescendants().OfType<TreeView>()
            .Single(candidate => AutomationProperties.GetName(candidate) == "Collections");
    }

    private async Task ScrollTreeToTheTopAsync()
    {
        CollectionsTree().GetVisualDescendants().OfType<ScrollViewer>().First().Offset = default;
        await SettleAsync();
    }

    private bool IsOnScreenInTheTree(Guid nodeId)
    {
        var tree = CollectionsTree();
        var row = tree.GetVisualDescendants().OfType<TreeViewItem>()
            .FirstOrDefault(item => item.DataContext is TreeNodeViewModel node && node.Id == nodeId);
        if (row?.TranslatePoint(new Point(0, 0), tree) is not { } top)
        {
            return false;
        }

        return top.Y >= 0 && top.Y < tree.Bounds.Height;
    }
}
