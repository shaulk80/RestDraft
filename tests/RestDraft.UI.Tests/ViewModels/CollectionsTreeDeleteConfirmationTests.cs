using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class CollectionsTreeDeleteConfirmationTests
{
    private TreeFixtures _fixtures = null!;
    private CollectionsTreeViewModel _sidebar = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixtures = new TreeFixtures();
        A.CallTo(() => _fixtures.Requests.DeleteAsync(A<Guid>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Success()));
        _sidebar = _fixtures.CreateSidebar();
        await _sidebar.ReloadAsync();
    }

    [Test]
    public async Task DeleteRemovesRequestWhenUserConfirms()
    {
        var node = _sidebar.FindRequest(_fixtures.Login.Id)!;

        await _sidebar.DeleteAsync(node);

        A.CallTo(() => _fixtures.Requests.DeleteAsync(_fixtures.Login.Id, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task DeleteDoesNothingWhenUserCancels()
    {
        A.CallTo(() => _fixtures.Confirmations.ConfirmAsync(A<ConfirmationRequest>._, A<CancellationToken>._)).Returns(Task.FromResult(false));
        var node = _sidebar.FindRequest(_fixtures.Login.Id)!;

        await _sidebar.DeleteAsync(node);

        A.CallTo(() => _fixtures.Requests.DeleteAsync(A<Guid>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task DeleteAsksToDeleteWithTheItemName()
    {
        var node = _sidebar.FindRequest(_fixtures.Login.Id)!;

        await _sidebar.DeleteAsync(node);

        A.CallTo(() => _fixtures.Confirmations.ConfirmAsync(
                A<ConfirmationRequest>.That.Matches(question => question.ConfirmText == "Delete" && question.Message.Contains("Login")),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
