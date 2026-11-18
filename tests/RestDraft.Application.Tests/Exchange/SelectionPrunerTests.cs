using NUnit.Framework;
using RestDraft.Application.Exchange;

namespace RestDraft.Application.Tests.Exchange;

[TestFixture]
public class SelectionPrunerTests
{
    private ExchangeFixtures _data = null!;

    [SetUp]
    public void SetUp()
    {
        _data = new ExchangeFixtures();
    }

    [Test]
    public void TheCollectionTickedKeepsEverything()
    {
        var pruned = SelectionPruner.Prune(_data.Source, new HashSet<Guid> { _data.Source.Id });

        Assert.That(ExchangeFixtures.Describe(pruned.Root), Is.EqualTo(ExchangeFixtures.Describe(_data.Source.Root)));
    }

    [Test]
    public void TheRootFolderTickedKeepsEverything()
    {
        var pruned = SelectionPruner.Prune(_data.Source, new HashSet<Guid> { _data.Source.Root.Id });

        Assert.That(ExchangeFixtures.Describe(pruned.Root.Folders.Single()), Is.EqualTo(ExchangeFixtures.Describe(_data.SourceApi)));
        Assert.That(pruned.Root.Requests.Single(), Is.EqualTo(_data.SourcePing));
    }

    [Test]
    public void ARequestKeepsItsFoldersButNotItsSiblings()
    {
        var pruned = SelectionPruner.Prune(_data.Source, new HashSet<Guid> { _data.SourceList.Id });

        Assert.That(pruned.Root.Requests, Is.Empty);
        Assert.That(pruned.Root.Folders.Single().Requests.Single().Id, Is.EqualTo(_data.SourceList.Id));
    }

    [Test]
    public void AFolderKeepsItsWholeContent()
    {
        var pruned = SelectionPruner.Prune(_data.Source, new HashSet<Guid> { _data.SourceApi.Id });

        Assert.That(ExchangeFixtures.Describe(pruned.Root.Folders.Single()), Is.EqualTo(ExchangeFixtures.Describe(_data.SourceApi)));
        Assert.That(pruned.Root.Requests, Is.Empty);
    }

    [Test]
    public void AnEmptyFolderTickedIsKept()
    {
        var empty = ExchangeFixtures.Folder("Empty", []);
        var collection = _data.Source with { Root = _data.Source.Root with { Folders = [.. _data.Source.Root.Folders, empty] } };

        var pruned = SelectionPruner.Prune(collection, new HashSet<Guid> { empty.Id });

        Assert.That(ExchangeFixtures.Describe(pruned.Root.Folders.Single()), Is.EqualTo(ExchangeFixtures.Describe(empty)));
    }

    [Test]
    public void NothingTickedLeavesAnEmptyRoot()
    {
        var pruned = SelectionPruner.Prune(_data.Source, new HashSet<Guid>());

        Assert.That(TreeCounts.Of(pruned.Root).IsEmpty, Is.True);
        Assert.That(pruned.Root.Id, Is.EqualTo(_data.Source.Root.Id));
    }
}
