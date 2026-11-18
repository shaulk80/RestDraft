using NUnit.Framework;
using RestDraft.Application.Search;
using RestDraft.Domain.Collections;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class SearchViewModelTests
{
    private static readonly string[] ListThenApplied = ["list", "applied"];

    private int _applied;
    private SearchViewModel _search = null!;

    [SetUp]
    public void SetUp()
    {
        _applied = 0;
        _search = new SearchViewModel(() =>
        {
            _applied++;
            return Task.CompletedTask;
        });
    }

    [Test]
    public void NothingIsAppliedUntilSearch()
    {
        _search.Text = "login";
        _search.SelectedMethod = nameof(RequestMethod.Post);

        Assert.That(_search.IsActive, Is.False);
        Assert.That(_search.Filter, Is.EqualTo(RequestFilter.None));
        Assert.That(_applied, Is.Zero);
    }

    [Test]
    public async Task SearchAppliesTrimmedTextMethodAndBodiesOption()
    {
        _search.Text = "  login ";
        _search.SelectedMethod = nameof(RequestMethod.Post);
        _search.SearchInBodies = true;

        await _search.ApplyAsync();

        Assert.That(_search.Filter, Is.EqualTo(new RequestFilter("login", RequestMethod.Post, true)));
        Assert.That(_search.IsActive, Is.True);
        Assert.That(_applied, Is.EqualTo(1));
    }

    [TestCase("", SearchViewModel.AnyMethod, true, false)]
    [TestCase("   ", SearchViewModel.AnyMethod, false, false)]
    [TestCase("a", SearchViewModel.AnyMethod, false, true)]
    [TestCase("", "Get", false, true)]
    public async Task FilterIsActiveOnlyWithTextOrMethod(string text, string method, bool inBodies, bool expected)
    {
        _search.Text = text;
        _search.SelectedMethod = method;
        _search.SearchInBodies = inBodies;

        await _search.ApplyAsync();

        Assert.That(_search.IsActive, Is.EqualTo(expected));
    }

    [Test]
    public async Task SearchCanBeCleared()
    {
        _search.Text = "login";
        await _search.ApplyAsync();
        _search.Text = string.Empty;

        await _search.ApplyAsync();

        Assert.That(_search.IsActive, Is.False);
        Assert.That(_search.Summary, Is.EqualTo("Filter"));
    }

    [Test]
    public async Task ClearNeedsAnAppliedFilter()
    {
        _search.Text = "typed but not applied";
        Assert.That(_search.ClearCommand.CanExecute(null), Is.False);

        await _search.ApplyAsync();

        Assert.That(_search.ClearCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task ClearEmptiesTheBoxesRemovesTheFilterAndTellsTheList()
    {
        _search.Text = "login";
        _search.SelectedMethod = nameof(RequestMethod.Post);
        _search.SearchInBodies = true;
        await _search.ApplyAsync();
        var appliedBefore = _applied;
        var raised = 0;
        _search.Applied += (_, _) => raised++;

        _search.ClearCommand.Execute(null);
        await Task.Yield();

        Assert.That(_search.Text, Is.Empty);
        Assert.That(_search.SelectedMethod, Is.EqualTo(SearchViewModel.AnyMethod));
        Assert.That(_search.SearchInBodies, Is.False);
        Assert.That(_search.Filter, Is.EqualTo(RequestFilter.None));
        Assert.That(_applied, Is.EqualTo(appliedBefore + 1));
        Assert.That(raised, Is.EqualTo(1));
        Assert.That(_search.ClearCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task ClearBecomesAvailableAndUnavailableAsTheFilterChanges()
    {
        var changes = 0;
        _search.ClearCommand.CanExecuteChanged += (_, _) => changes++;
        _search.Text = "login";

        await _search.ApplyAsync();
        _search.Text = string.Empty;
        await _search.ApplyAsync();

        Assert.That(changes, Is.EqualTo(2));
    }

    [Test]
    public async Task SummaryNamesWhatIsApplied()
    {
        _search.Text = "login";
        _search.SelectedMethod = nameof(RequestMethod.Post);
        _search.SearchInBodies = true;

        await _search.ApplyAsync();

        Assert.That(_search.Summary, Is.EqualTo("Filter on: \"login\", POST, also in bodies"));
    }

    [Test]
    public async Task AppliedIsRaisedAfterTheListHasTakenTheFilter()
    {
        var order = new List<string>();
        var search = new SearchViewModel(() =>
        {
            order.Add("list");
            return Task.CompletedTask;
        });
        search.Applied += (_, _) => order.Add("applied");

        await search.ApplyAsync();

        Assert.That(order, Is.EqualTo(ListThenApplied));
    }

    [Test]
    public async Task ChangingTheAppliedFilterRaisesIsActiveAndSummary()
    {
        var changed = new List<string?>();
        _search.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        _search.Text = "login";

        await _search.ApplyAsync();

        Assert.That(changed, Does.Contain(nameof(SearchViewModel.IsActive)));
        Assert.That(changed, Does.Contain(nameof(SearchViewModel.Summary)));
    }
}
