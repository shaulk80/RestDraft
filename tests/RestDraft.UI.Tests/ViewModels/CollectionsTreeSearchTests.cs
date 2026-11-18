using FakeItEasy;
using NUnit.Framework;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class CollectionsTreeSearchTests
{
    private static readonly string[] ShopOnly = ["Shop"];
    private static readonly string[] ShopAndOperations = ["Shop", "Operations"];
    private static readonly string[] OperationsOnly = ["Operations"];

    private TreeFixtures _fixtures = null!;
    private CollectionsTreeViewModel _sidebar = null!;
    private RequestDefinition _health = null!;
    private Collection _ops = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixtures = new TreeFixtures();
        _health = new RequestDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Health",
            Url = "https://ops.example.com/health",
            Body = new RawBody("text/plain", "ping"),
        };
        _ops = new Collection(Guid.NewGuid(), "Operations", new Folder(Guid.NewGuid(), string.Empty, [], [_health]));
        _fixtures.SetCollections([_fixtures.Shop, _ops]);
        _sidebar = _fixtures.CreateSidebar();
        await _sidebar.ReloadAsync();
    }

    [Test]
    public async Task SearchShowsOnlyWhatMatchesAndOpensIt()
    {
        _sidebar.Search.Text = "login";

        await _sidebar.Search.ApplyAsync();

        Assert.That(_sidebar.Roots.Select(root => root.Name), Is.EqualTo(ShopOnly));
        Assert.That(_sidebar.Roots[0].IsExpanded, Is.True);
        Assert.That(_sidebar.Roots[0].Children[0].IsExpanded, Is.True);
        Assert.That(_sidebar.FindRequest(_fixtures.Login.Id), Is.Not.Null);
    }

    [Test]
    public async Task ClearingTheSearchShowsEverythingAgainWithTheOpenNodesOfBefore()
    {
        _sidebar.Roots[1].IsExpanded = true;
        _sidebar.Search.Text = "login";
        await _sidebar.Search.ApplyAsync();

        _sidebar.Search.Text = string.Empty;
        await _sidebar.Search.ApplyAsync();

        Assert.That(_sidebar.Roots.Select(root => root.Name), Is.EqualTo(ShopAndOperations));
        Assert.That(_sidebar.Roots[0].IsExpanded, Is.False, "it was closed before the search");
        Assert.That(_sidebar.Roots[1].IsExpanded, Is.True, "it was open before the search");
    }

    [Test]
    public async Task SearchInBodiesFindsTheBodyOnlyWhenAsked()
    {
        _sidebar.Search.Text = "ping";
        await _sidebar.Search.ApplyAsync();
        var withoutBodies = _sidebar.Roots.Count;

        _sidebar.Search.SearchInBodies = true;
        await _sidebar.Search.ApplyAsync();

        Assert.That(withoutBodies, Is.Zero);
        Assert.That(_sidebar.Roots.Select(root => root.Name), Is.EqualTo(OperationsOnly));
    }

    [Test]
    public async Task SelectedNodeStaysSelectedWhenItStillMatches()
    {
        await _sidebar.ReloadAsync(_fixtures.Login.Id);
        _sidebar.Search.Text = "log";

        await _sidebar.Search.ApplyAsync();

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
    }

    [Test]
    public async Task SelectionIsDroppedWhenTheSearchHidesTheSelectedNode()
    {
        await _sidebar.ReloadAsync(_fixtures.Login.Id);
        _sidebar.Search.Text = "health";

        await _sidebar.Search.ApplyAsync();

        Assert.That(_sidebar.SelectedNode, Is.Null);
        Assert.That(_sidebar.FindRequest(_fixtures.Login.Id), Is.Null);
    }

    [Test]
    public async Task NodeThatWasJustCreatedStaysVisibleEvenIfItDoesNotMatch()
    {
        _sidebar.Search.Text = "health";
        await _sidebar.Search.ApplyAsync();

        await _sidebar.ReloadAsync(_fixtures.Login.Id);

        Assert.That(_sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Login.Id));
    }

    [TestCase("login", false)]
    [TestCase("nothing like it", true)]
    [TestCase("", false)]
    public async Task EmptyTreeIsExplainedOnlyWhileAFilterIsApplied(string text, bool expected)
    {
        _sidebar.Search.Text = text;

        await _sidebar.Search.ApplyAsync();

        Assert.That(_sidebar.IsFilteredEmpty, Is.EqualTo(expected));
    }

    // The tree is shorter while a filter is applied. A tab of a hidden request must not be taken for a deleted one.
    [Test]
    public async Task RequestsHiddenByTheSearchAreStillKnownByName()
    {
        _sidebar.Search.Text = "health";

        await _sidebar.Search.ApplyAsync();

        Assert.That(_sidebar.FindRequestName(_fixtures.Login.Id), Is.EqualTo("Login"));
        Assert.That(_sidebar.FindRequestName(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task SearchDoesNotWriteAnything()
    {
        _sidebar.Search.Text = "login";

        await _sidebar.Search.ApplyAsync();

        A.CallTo(_fixtures.Folders).MustNotHaveHappened();
        A.CallTo(_fixtures.Requests).MustNotHaveHappened();
        A.CallTo(_fixtures.Tree).MustNotHaveHappened();
    }
}
