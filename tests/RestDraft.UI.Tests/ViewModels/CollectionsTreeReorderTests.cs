using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class CollectionsTreeReorderTests
{
    private ReorderTree _tree = null!;
    private ICollectionTreeService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _tree = await ReorderTree.CreateAsync();
        _service = _tree.Fixtures.Tree;
        A.CallTo(() => _service.MoveAsync(A<Guid>._, A<Guid>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Success()));
    }

    [TestCase("F1", "F2", DropPosition.After, "F3")]
    [TestCase("F3", "F1", DropPosition.Before, "F1")]
    [TestCase("R1", "R3", DropPosition.After, null)]
    [TestCase("R3", "F2", DropPosition.Before, "R1")]
    public async Task DroppingBesideASiblingAsksForTheMatchingPlace(string source, string target, DropPosition position, string? before)
    {
        var moved = _tree.Node(source);
        Guid? beforeId = before is null ? null : _tree.Node(before).Id;

        await _tree.Sidebar.MoveAsync(moved, _tree.Node(target), position);

        A.CallTo(() => _service.MoveAsync(moved.Id, _tree.RootFolderId, beforeId, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [TestCase("F1", "F2", DropPosition.Before)]
    [TestCase("R2", "R1", DropPosition.After)]
    [TestCase("F3", "R3", DropPosition.After)]
    public async Task DroppingWhereTheNodeAlreadyIsDoesNothing(string source, string target, DropPosition position)
    {
        await _tree.Sidebar.MoveAsync(_tree.Node(source), _tree.Node(target), position);

        A.CallTo(() => _service.MoveAsync(A<Guid>._, A<Guid>._, A<Guid?>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task AMovedFolderIsSelectedAndItsNewParentIsOpened()
    {
        var folder = _tree.Node("F3");

        await _tree.Sidebar.MoveAsync(folder, _tree.Node("N1"), DropPosition.Before);

        Assert.That(_tree.Sidebar.SelectedNode?.Id, Is.EqualTo(folder.Id));
    }

    [Test]
    public async Task AFailedMoveShowsTheErrorAndKeepsTheTree()
    {
        A.CallTo(() => _service.MoveAsync(A<Guid>._, A<Guid>._, A<Guid?>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Failure(ResultErrors.Validation("A folder with that name exists."))));
        var rootsBefore = _tree.Sidebar.Roots.ToList();

        await _tree.Sidebar.MoveAsync(_tree.Node("F1"), _tree.Node("F2"), DropPosition.After);

        Assert.That(_tree.Sidebar.StatusMessage, Does.Contain("A folder with that name exists."));
        Assert.That(_tree.Sidebar.Roots, Is.EqualTo(rootsBefore));
    }
}
