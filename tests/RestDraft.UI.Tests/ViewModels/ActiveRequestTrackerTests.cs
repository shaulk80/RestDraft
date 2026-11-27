using FakeItEasy;
using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class ActiveRequestTrackerTests
{
    private TreeFixtures _fixtures = null!;
    private CollectionsTreeViewModel _sidebar = null!;
    private TabStripViewModel _tabs = null!;
    private WorkspaceLayoutViewModel _layout = null!;
    private RequestDefinition _profile = null!;

    // Login is in the folder Users. Profile is in the root of the collection.
    [SetUp]
    public async Task SetUp()
    {
        _fixtures = new TreeFixtures();
        _profile = new RequestDefinition { Id = Guid.NewGuid(), Name = "Profile", Url = "https://example.com/profile" };
        _fixtures.SetCollections([_fixtures.Shop with { Root = _fixtures.Root with { Requests = [_profile] } }]);
        SetStored(_fixtures.Login);
        SetStored(_profile);
        _sidebar = _fixtures.CreateSidebar();
        await _sidebar.ReloadAsync();
        var services = RequestTabServicesFactory.Create(
            A.Fake<RestDraft.Application.Abstractions.IRequestSender>(), _fixtures.Requests, A.Fake<IDestinationPicker>());
        _tabs = TabStripFactory.Create(services);
        _layout = new WorkspaceLayoutViewModel(SettingsServiceFactory.Create());
        new ActiveRequestTracker(_sidebar, _tabs, _layout).Start();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ActivatingATabSelectsItsRequestOnlyWhileTracking(bool isTracking)
    {
        _layout.IsTrackingActiveRequest = isTracking;

        await _tabs.OpenRequestAsync(_fixtures.Login.Id);

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(isTracking ? _fixtures.Login.Id : (Guid?)null));
        Assert.That(FolderOfLogin().IsExpanded, Is.EqualTo(isTracking));
    }

    [Test]
    public async Task SwitchingBackToATabSelectsItsRequestAgain()
    {
        _layout.IsTrackingActiveRequest = true;
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);
        await _tabs.OpenRequestAsync(_profile.Id);

        _tabs.SelectedTab = _tabs.Tabs[0];

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
    }

    [Test]
    public async Task ClosingTheActiveTabSelectsTheRequestOfTheNextOne()
    {
        _layout.IsTrackingActiveRequest = true;
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);
        await _tabs.OpenRequestAsync(_profile.Id);

        await _tabs.CloseTabAsync(_tabs.Tabs[1]);

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
    }

    [Test]
    public async Task SwitchingTrackingOnShowsTheActiveRequestAtOnce()
    {
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);

        _layout.IsTrackingActiveRequest = true;

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
        Assert.That(FolderOfLogin().IsExpanded, Is.True);
        Assert.That(_sidebar.Roots[0].IsExpanded, Is.True);
    }

    [Test]
    public async Task ATabThatIsNotInACollectionLeavesTheSelection()
    {
        _layout.IsTrackingActiveRequest = true;
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);

        _tabs.NewTab();

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
    }

    [Test]
    public async Task ARequestHiddenByTheFilterLeavesTheTreeAndTheFilterAsTheyAre()
    {
        _layout.IsTrackingActiveRequest = true;
        _sidebar.Search.Text = "profile";
        await _sidebar.Search.ApplyAsync();
        _sidebar.SelectedNode = _sidebar.Roots[0];

        await _tabs.OpenRequestAsync(_fixtures.Login.Id);

        Assert.That(_sidebar.SelectedNode, Is.SameAs(_sidebar.Roots[0]));
        Assert.That(_sidebar.FindRequest(_fixtures.Login.Id), Is.Null);
        Assert.That(_sidebar.Search.IsActive, Is.True);
    }

    [Test]
    public async Task TheActiveRequestIsRevealedAgainWhenTheFilterShowsIt()
    {
        _layout.IsTrackingActiveRequest = true;
        _sidebar.Search.Text = "profile";
        await _sidebar.Search.ApplyAsync();
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);

        _sidebar.Search.ClearCommand.Execute(null);

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
        Assert.That(FolderOfLogin().IsExpanded, Is.True);
    }

    [Test]
    public async Task AReloadForAnotherReasonKeepsTheSelection()
    {
        _layout.IsTrackingActiveRequest = true;
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);
        var folderId = _fixtures.Users.Id;

        await _sidebar.ReloadAsync(folderId);

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(folderId));
    }

    // An import that overwrites a request replaces its open tab. That is a reload, not a change of the active request.
    [Test]
    public async Task ReplacingTheActiveTabAfterAnImportKeepsTheSelection()
    {
        _layout.IsTrackingActiveRequest = true;
        await _tabs.OpenRequestAsync(_fixtures.Login.Id);
        _sidebar.SelectedNode = _sidebar.Roots[0];

        await _tabs.ReloadLinkedAsync([_fixtures.Login.Id]);

        Assert.That(_sidebar.SelectedNode, Is.SameAs(_sidebar.Roots[0]));
    }

    private TreeNodeViewModel FolderOfLogin()
    {
        return _sidebar.Roots[0].Children.Single(node => node.Id == _fixtures.Users.Id);
    }

    private void SetStored(RequestDefinition request)
    {
        A.CallTo(() => _fixtures.Requests.GetAsync(request.Id, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<RequestDefinition>.Success(request)));
    }
}
