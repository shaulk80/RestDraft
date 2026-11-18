using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Tests.Exchange;

[TestFixture]
public class ImportPlannerTests
{
    private static readonly string[] ListAndCreate = ["List", "Create"];
    private static readonly string[] CreateOnly = ["Create"];

    private ExchangeFixtures _data = null!;

    [SetUp]
    public void SetUp()
    {
        _data = new ExchangeFixtures();
    }

    [Test]
    public void NewCollectionTakesEverythingTicked()
    {
        var plan = Plan(ImportTarget.NewCollection("  Imported "), overwrite: false, _data.AllTicked, [_data.Target]);

        var created = plan.Collections.Single(collection => collection.Name == "Imported");
        Assert.That(plan.Collections, Has.Count.EqualTo(2));
        Assert.That(created.Root.Folders.Single().Requests.Select(request => request.Name), Is.EqualTo(ListAndCreate));
        Assert.That(created.Root.Requests.Single().Name, Is.EqualTo("Ping"));
        Assert.That(plan.Counts, Is.EqualTo(new ImportCounts(1, 3, 0, 0)));
    }

    [TestCase(false, 0, 1)]
    [TestCase(true, 1, 0)]
    public void ExistingRequestIsSkippedOrOverwrittenByName(bool overwrite, int overwritten, int skipped)
    {
        var plan = Plan(ImportTarget.Existing(_data.Target.Id), overwrite, _data.AllTicked, [_data.Target]);

        var api = plan.Collections.Single().Root.Folders.Single();
        var list = api.Requests.Single(request => request.Name == "list");
        Guid[] expectedOverwritten = overwrite ? [_data.TargetList.Id] : [];
        Assert.That(plan.Counts, Is.EqualTo(new ImportCounts(0, 2, overwritten, skipped)));
        Assert.That(list.Id, Is.EqualTo(_data.TargetList.Id));
        Assert.That(list.Url, Is.EqualTo(overwrite ? "http://file/list" : "http://mine/list"));
        Assert.That(api.Requests.Select(request => request.Name), Does.Contain("Create").And.Contain("Other"));
        Assert.That(plan.OverwrittenRequestIds, Is.EqualTo(expectedOverwritten));
    }

    [Test]
    public void ExistingFolderIsMergedAndNothingIsDeleted()
    {
        var plan = Plan(ImportTarget.Existing(_data.Target.Id), overwrite: true, _data.AllTicked, [_data.Target]);

        var root = plan.Collections.Single().Root;
        Assert.That(root.Folders.Single().Id, Is.EqualTo(_data.TargetApi.Id));
        Assert.That(root.Folders.Single().Requests.Select(request => request.Name), Does.Contain("Other"));
        Assert.That(root.Requests.Single().Name, Is.EqualTo("Ping"));
    }

    [Test]
    public void UntickedItemsAreLeftOutAndATickedRequestBringsItsFolders()
    {
        var ticked = new HashSet<Guid> { _data.SourceCreate.Id };

        var plan = Plan(ImportTarget.NewCollection("New"), overwrite: false, ticked, []);

        var root = plan.Collections.Single().Root;
        Assert.That(root.Requests, Is.Empty);
        Assert.That(root.Folders.Single().Requests.Select(request => request.Name), Is.EqualTo(CreateOnly));
        Assert.That(plan.Counts, Is.EqualTo(new ImportCounts(1, 1, 0, 0)));
    }

    [Test]
    public void NothingTickedChangesNothing()
    {
        var plan = Plan(ImportTarget.Existing(_data.Target.Id), overwrite: true, new HashSet<Guid>(), [_data.Target]);

        Assert.That(plan.Counts.HasChanges, Is.False);
        Assert.That(ExchangeFixtures.Describe(plan.Collections.Single().Root), Is.EqualTo(ExchangeFixtures.Describe(_data.Target.Root)));
    }

    [Test]
    public void NewItemMeetingTheOtherKindOfSameNameGetsANumber()
    {
        var clashing = ExchangeFixtures.Folder(string.Empty, [ExchangeFixtures.Request("api", "http://file/api")]);
        var source = new Collection(Guid.NewGuid(), "File", clashing);

        var plan = ImportPlanner.Plan([_data.Target], source, ImportTarget.Existing(_data.Target.Id), false, new HashSet<Guid> { source.Id }).Value!;

        Assert.That(plan.Collections.Single().Root.Requests.Single().Name, Is.EqualTo("api 2"));
    }

    [Test]
    public void TreeShowsWhichItemsExistInTheTarget()
    {
        var plan = Plan(ImportTarget.Existing(_data.Target.Id), overwrite: false, _data.AllTicked, [_data.Target]);

        var api = plan.Tree.Children.Single(node => node.Kind == PlanNodeKind.Folder);
        Assert.That(api.Status, Is.EqualTo(PlanStatus.Exists));
        Assert.That(api.Children.Single(node => node.Name == "List").Status, Is.EqualTo(PlanStatus.Exists));
        Assert.That(api.Children.Single(node => node.Name == "Create").Status, Is.EqualTo(PlanStatus.New));
        Assert.That(plan.Tree.Children.Single(node => node.Name == "Ping").Status, Is.EqualTo(PlanStatus.New));
    }

    [Test]
    public void TreeOfANewCollectionIsAllNew()
    {
        var plan = Plan(ImportTarget.NewCollection("New"), overwrite: false, _data.AllTicked, [_data.Target]);

        var statuses = plan.Tree.Children.SelectMany(node => new[] { node }.Concat(node.Children)).Select(node => node.Status);
        Assert.That(statuses, Is.All.EqualTo(PlanStatus.New));
    }

    [Test]
    public void MissingTargetCollectionFails()
    {
        var result = ImportPlanner.Plan([_data.Target], _data.Source, ImportTarget.Existing(Guid.NewGuid()), false, _data.AllTicked);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.NotFound));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void EmptyNameForANewCollectionFails(string name)
    {
        var result = ImportPlanner.Plan([], _data.Source, ImportTarget.NewCollection(name), false, _data.AllTicked);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.Validation));
    }

    private ImportPlan Plan(ImportTarget target, bool overwrite, HashSet<Guid> ticked, IReadOnlyList<Collection> existing)
    {
        return ImportPlanner.Plan(existing, _data.Source, target, overwrite, ticked).Value!;
    }
}
