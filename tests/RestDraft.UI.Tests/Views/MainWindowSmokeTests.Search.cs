using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.Domain.History;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

// The search popup of the top bar, and the History tab it filters.
public partial class MainWindowSmokeTests
{
    private const int CollectionsTab = 0;
    private const int HistoryTab = 1;
    private static readonly string[] LoginUrlOnly = ["https://example.com/login"];

    [AvaloniaTest]
    public void RestoreButtonOfTheHistoryTabIsEnabledByClickingAnEntry()
    {
        SetHistory("https://example.com/login");
        OpenWindow();
        ShowSidebarTab(HistoryTab);
        var restore = FindButton("Restore");
        Assert.That(restore.IsEffectivelyEnabled, Is.False, "disabled before a selection");
        var item = _window.GetVisualDescendants().OfType<ListBoxItem>().First(candidate => candidate.DataContext is HistoryItemViewModel);

        Click(item);

        Assert.That(_viewModel.History.SelectedItem, Is.Not.Null, "entry selected");
        Assert.That(restore.IsEffectivelyEnabled, Is.True, "Restore enabled after the click");
    }

    [AvaloniaTest]
    public void FilterButtonSitsLeftOfTheEnvironmentControls()
    {
        OpenWindow();

        var filter = FindFilterButton();
        var environment = _window.GetVisualDescendants().OfType<ComboBox>()
            .First(candidate => AutomationProperties.GetName(candidate) == "Active environment");

        Assert.That(filter.TranslatePoint(new Point(0, 0), _window)!.Value.X, Is.LessThan(environment.TranslatePoint(new Point(0, 0), _window)!.Value.X));
        Assert.That(filter.Classes.Contains("active"), Is.False);
    }

    [TestCase(CollectionsTab)]
    [TestCase(HistoryTab)]
    [AvaloniaTest]
    public void FilterButtonOpensThePopupOnTheSearchOfTheShownTab(int tab)
    {
        OpenWindow();
        ShowSidebarTab(tab);

        Click(FindFilterButton());

        var popup = FindPopup();
        Assert.That(popup, Is.Not.Null, "the popup is open");
        var expected = tab == HistoryTab ? _viewModel.History.Search : _viewModel.Sidebar.Search;
        Assert.That(popup!.DataContext, Is.SameAs(expected));
    }

    [AvaloniaTest]
    public void SearchInThePopupFiltersTheHistoryClosesThePopupAndMarksTheButton()
    {
        SetHistory("https://example.com/login", "https://example.com/other");
        OpenWindow();
        ShowSidebarTab(HistoryTab);
        Assert.That(_viewModel.History.Items, Has.Count.EqualTo(2));
        Click(FindFilterButton());
        var popup = FindPopup()!;
        popup.GetVisualDescendants().OfType<TextBox>().First().Text = "login";

        Click(popup.GetVisualDescendants().OfType<Button>().First(button => button.Content as string == "Search"));

        Assert.That(_viewModel.History.Items.Select(item => item.Url), Is.EqualTo(LoginUrlOnly));
        Assert.That(_viewModel.Sidebar.Search.IsActive, Is.False, "the tree has its own search");
        Assert.That(FindFilterButton().Classes.Contains("active"), Is.True);
        Assert.That(FindPopup(), Is.Null, "the popup closed after the search");
    }

    [AvaloniaTest]
    public void FilterButtonShowsTheAppliedSearchOfTheShownTabOnly()
    {
        SetHistory("https://example.com/login");
        OpenWindow();
        _viewModel.History.Search.Text = "login";
        _viewModel.History.Search.SearchCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        ShowSidebarTab(CollectionsTab);
        var onCollections = FindFilterButton().Classes.Contains("active");
        ShowSidebarTab(HistoryTab);
        var onHistory = FindFilterButton().Classes.Contains("active");

        Assert.That(onCollections, Is.False);
        Assert.That(onHistory, Is.True);
    }

    [AvaloniaTest]
    public void SearchWithoutAMatchSaysSoInTheTree()
    {
        OpenWindow();
        _viewModel.Sidebar.Search.Text = "nothing like it";

        _viewModel.Sidebar.Search.SearchCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var message = _window.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == "Nothing matches the filter.");
        Assert.That(message.IsEffectivelyVisible, Is.True);
    }

    [TestCase(CollectionsTab)]
    [TestCase(HistoryTab)]
    [AvaloniaTest]
    public void CtrlFOpensThePopupOnTheSearchOfTheShownTab(int tab)
    {
        OpenWindow();
        ShowSidebarTab(tab);

        PressCtrlF();

        var expected = tab == HistoryTab ? _viewModel.History.Search : _viewModel.Sidebar.Search;
        Assert.That(FindPopup()?.DataContext, Is.SameAs(expected));
    }

    [AvaloniaTest]
    public void CtrlFWorksWhileTheTopBarIsHiddenAndHangsUnderTheMenu()
    {
        OpenWindow();
        _viewModel.Layout.IsTopBarVisible = false;
        Dispatcher.UIThread.RunJobs();
        Assert.That(FindFilterButton().IsEffectivelyVisible, Is.False);

        PressCtrlF();

        var popup = FindPopup();
        Assert.That(popup, Is.Not.Null, "the popup is open");
        var menu = _window.GetVisualDescendants().OfType<Menu>().First();
        var menuBottom = menu.TranslatePoint(new Point(0, menu.Bounds.Height), _window)!.Value.Y;
        var popupTop = popup!.TranslatePoint(new Point(0, 0), _window)!.Value.Y;
        Assert.That(popupTop, Is.GreaterThanOrEqualTo(menuBottom - 1), "below the menu, not over it");
    }

    [AvaloniaTest]
    public void CtrlFWorksWhileATextBoxHasTheFocus()
    {
        OpenWindow();
        FocusUrlBox();

        PressCtrlF();

        Assert.That(FindPopup(), Is.Not.Null);
    }

    [AvaloniaTest]
    public void CtrlFAgainWhileThePopupIsOpenKeepsOnePopup()
    {
        OpenWindow();
        PressCtrlF();

        PressCtrlF();

        Assert.That(_window.GetVisualDescendants().OfType<SearchPopupView>().Count(view => view.IsEffectivelyVisible), Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void ClearIsDisabledUntilAFilterIsAppliedThenRemovesItAndClosesThePopup()
    {
        SetHistory("https://example.com/login", "https://example.com/other");
        OpenWindow();
        ShowSidebarTab(HistoryTab);
        Click(FindFilterButton());
        var clear = FindPopup()!.GetVisualDescendants().OfType<Button>().First(button => button.Content as string == "Clear");
        Assert.That(clear.IsEffectivelyEnabled, Is.False, "no filter yet");
        _viewModel.History.Search.Text = "login";
        _viewModel.History.Search.SearchCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(_viewModel.History.Items, Has.Count.EqualTo(1));
        Click(FindFilterButton());
        clear = FindPopup()!.GetVisualDescendants().OfType<Button>().First(button => button.Content as string == "Clear");
        Assert.That(clear.IsEffectivelyEnabled, Is.True, "a filter is applied");

        Click(clear);

        Assert.That(_viewModel.History.Items, Has.Count.EqualTo(2));
        Assert.That(FindFilterButton().Classes.Contains("active"), Is.False);
        Assert.That(FindPopup(), Is.Null, "the popup closed");
        Assert.That(_viewModel.History.Search.Text, Is.Empty);
    }

    private void PressCtrlF()
    {
        _window.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
    }

    private void SetHistory(params string[] urls)
    {
        _historyEntries = urls
            .Select(url => new RequestDefinition { Id = Guid.NewGuid(), Name = "Entry", Url = url })
            .Select(request => new HistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, RequestMethod.Get, request.Url, request, 200, 3, null, null))
            .ToList();
    }

    private void ShowSidebarTab(int index)
    {
        _viewModel.SidebarTabIndex = index;
        Dispatcher.UIThread.RunJobs();
    }

    private Button FindButton(string content)
    {
        return _window.GetVisualDescendants().OfType<Button>().First(button => button.Content as string == content);
    }

    private Button FindFilterButton()
    {
        return _window.GetVisualDescendants().OfType<Button>().First(button => AutomationProperties.GetName(button) == "Filter");
    }

    private SearchPopupView? FindPopup()
    {
        return _window.GetVisualDescendants().OfType<SearchPopupView>().FirstOrDefault(view => view.IsEffectivelyVisible);
    }

    private void Click(Control control)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), _window)!.Value;
        _window.MouseDown(center, MouseButton.Left);
        _window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
