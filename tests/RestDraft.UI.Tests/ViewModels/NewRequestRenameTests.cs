using FakeItEasy;
using NUnit.Framework;
using RestDraft.Domain.Results;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

// The rename that follows "New request": what Enter, Escape and leaving the box do with the name and the focus.
[TestFixture]
public class NewRequestRenameTests
{
    private const string TypedName = "Sign in";

    private TreeFixtures _fixtures = null!;
    private CollectionsTreeViewModel _sidebar = null!;
    private int _urlFocusRequests;

    public enum RenameEnd
    {
        Enter,
        Escape,
        LostFocus,
    }

    [SetUp]
    public async Task SetUp()
    {
        _fixtures = new TreeFixtures();
        A.CallTo(() => _fixtures.Requests.RenameAsync(A<Guid>._, A<string>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Success()));
        _sidebar = _fixtures.CreateSidebar();
        await _sidebar.ReloadAsync();
        _urlFocusRequests = 0;
        _sidebar.UrlFocusRequested += (_, _) => _urlFocusRequests++;
    }

    [TestCase(RenameEnd.Enter, true, 1, 1)]
    [TestCase(RenameEnd.Escape, true, 0, 1)]
    [TestCase(RenameEnd.LostFocus, true, 1, 0)]
    [TestCase(RenameEnd.Enter, false, 1, 0)]
    [TestCase(RenameEnd.Escape, false, 0, 0)]
    [TestCase(RenameEnd.LostFocus, false, 1, 0)]
    public void EndingARenameSavesTheNameAndAsksForTheUrlOnlyForANewRequestEndedByKey(
        RenameEnd end, bool isNewRequest, int expectedRenames, int expectedUrlFocusRequests)
    {
        var node = StartRename(isNewRequest);
        node.EditName = TypedName;

        End(node, end);

        Assert.That(node.IsEditing, Is.False);
        Assert.That(node.IsNamingNewRequest, Is.False);
        A.CallTo(() => _fixtures.Requests.RenameAsync(node.Id, TypedName, A<CancellationToken>._))
            .MustHaveHappened(expectedRenames, Times.Exactly);
        Assert.That(_urlFocusRequests, Is.EqualTo(expectedUrlFocusRequests));
    }

    // The box loses the focus as soon as Enter or Escape has hidden it, so a second command follows the first.
    [TestCase(RenameEnd.Enter, 1)]
    [TestCase(RenameEnd.Escape, 0)]
    public void LosingTheFocusAfterAKeyChangesNothingMore(RenameEnd key, int expectedRenames)
    {
        var node = StartRename(isNewRequest: true);
        node.EditName = TypedName;

        End(node, key);
        End(node, RenameEnd.LostFocus);

        A.CallTo(() => _fixtures.Requests.RenameAsync(A<Guid>._, A<string>._, A<CancellationToken>._))
            .MustHaveHappened(expectedRenames, Times.Exactly);
        Assert.That(_urlFocusRequests, Is.EqualTo(1));
    }

    [Test]
    public void EnterWithTheUnchangedNameStillAsksForTheUrl()
    {
        var node = StartRename(isNewRequest: true);

        End(node, RenameEnd.Enter);

        A.CallTo(() => _fixtures.Requests.RenameAsync(A<Guid>._, A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        Assert.That(_urlFocusRequests, Is.EqualTo(1));
    }

    [Test]
    public void ARefusedNameIsReportedAndTheUrlIsStillAskedFor()
    {
        A.CallTo(() => _fixtures.Requests.RenameAsync(A<Guid>._, A<string>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Failure(new Error(ErrorCategory.Validation, "That name is taken."))));
        var node = StartRename(isNewRequest: true);
        node.EditName = TypedName;

        End(node, RenameEnd.Enter);

        Assert.That(_sidebar.StatusMessage, Is.EqualTo("That name is taken."));
        Assert.That(_urlFocusRequests, Is.EqualTo(1));
    }

    // A later rename of the same node, with F2 or the menu, is an ordinary one.
    [Test]
    public void RenamingAgainIsNoLongerTheNamingOfANewRequest()
    {
        var node = StartRename(isNewRequest: true);

        node.BeginRename();
        End(node, RenameEnd.Enter);

        Assert.That(_urlFocusRequests, Is.Zero);
    }

    [Test]
    public async Task CreatingARequestAsksForItToBeNamedAndNotJustOpened()
    {
        var created = new List<Guid>();
        var opened = new List<Guid>();
        _sidebar.RequestCreated += created.Add;
        _sidebar.RequestOpenRequested += opened.Add;
        var id = Guid.NewGuid();
        A.CallTo(() => _fixtures.Requests.CreateAsync(A<Guid>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<Guid>.Success(id)));

        await _sidebar.CreateRequestAsync(_sidebar.Roots[0]);

        Assert.That(created, Is.EqualTo(new[] { id }));
        Assert.That(opened, Is.Empty);
    }

    [Test]
    public void TheNewRequestIsSelectedShownAndInRenameMode()
    {
        _sidebar.BeginRenameOfNew(_fixtures.Login.Id);

        var node = _sidebar.FindRequest(_fixtures.Login.Id)!;
        Assert.That(_sidebar.SelectedNode, Is.SameAs(node));
        Assert.That(node.IsEditing, Is.True);
        Assert.That(node.EditName, Is.EqualTo(_fixtures.Login.Name));
        Assert.That(node.Parent!.IsExpanded, Is.True);
    }

    private RequestNodeViewModel StartRename(bool isNewRequest)
    {
        var node = _sidebar.FindRequest(_fixtures.Login.Id)!;
        if (isNewRequest)
        {
            node.BeginRenameOfNewRequest();
        }
        else
        {
            node.BeginRename();
        }

        return node;
    }

    // The fake services complete at once, so a command has done its work when Execute returns.
    private static void End(TreeNodeViewModel node, RenameEnd end)
    {
        var command = end switch
        {
            RenameEnd.Enter => node.AcceptRenameCommand,
            RenameEnd.Escape => node.CancelRenameCommand,
            _ => node.CommitRenameCommand,
        };
        command.Execute(null);
    }
}
