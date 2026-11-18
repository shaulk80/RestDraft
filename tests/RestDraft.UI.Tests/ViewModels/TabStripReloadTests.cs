using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Collections;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

// After an import overwrote requests, their open tabs show the saved version again.
[TestFixture]
public class TabStripReloadTests
{
    private IRequestService _requests = null!;
    private TabStripViewModel _strip = null!;
    private RequestDefinition _list = null!;
    private RequestDefinition _create = null!;

    [SetUp]
    public async Task SetUp()
    {
        _requests = A.Fake<IRequestService>();
        _strip = TabStripFactory.Create(RequestTabServicesFactory.Create(A.Fake<IRequestSender>(), _requests, A.Fake<IDestinationPicker>()));
        _list = new RequestDefinition { Id = Guid.NewGuid(), Name = "List", Url = "http://old/list" };
        _create = new RequestDefinition { Id = Guid.NewGuid(), Name = "Create", Url = "http://old/create" };
        Serve(_list);
        Serve(_create);
        await _strip.OpenRequestAsync(_list.Id);
        await _strip.OpenRequestAsync(_create.Id);
    }

    [Test]
    public async Task TabsOfTheOverwrittenRequestsShowTheNewContentAndOthersAreKept()
    {
        var untouched = _strip.Tabs[1];
        Serve(_list with { Url = "http://new/list" });

        await _strip.ReloadLinkedAsync([_list.Id]);

        Assert.That(_strip.Tabs, Has.Count.EqualTo(2));
        Assert.That(_strip.Tabs[0].Url, Is.EqualTo("http://new/list"));
        Assert.That(_strip.Tabs[0].IsDirty, Is.False);
        Assert.That(_strip.Tabs[1], Is.SameAs(untouched));
    }

    [Test]
    public async Task EditsInAReloadedTabAreReplaced()
    {
        _strip.Tabs[0].Url = "http://typed/list";
        Serve(_list with { Url = "http://new/list" });

        await _strip.ReloadLinkedAsync([_list.Id]);

        Assert.That(_strip.Tabs[0].Url, Is.EqualTo("http://new/list"));
        Assert.That(_strip.Tabs[0].IsDirty, Is.False);
    }

    [Test]
    public async Task TheSelectedTabStaysSelectedAndKeepsItsSection()
    {
        _strip.SelectedTab = _strip.Tabs[0];
        _strip.Tabs[0].SectionIndex = 2;

        await _strip.ReloadLinkedAsync([_list.Id]);

        Assert.That(_strip.SelectedTab, Is.SameAs(_strip.Tabs[0]));
        Assert.That(_strip.Tabs[0].SectionIndex, Is.EqualTo(2));
    }

    [Test]
    public async Task ARequestThatCannotBeLoadedLeavesItsTabAlone()
    {
        var before = _strip.Tabs[0];
        A.CallTo(() => _requests.GetAsync(_list.Id, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<RequestDefinition>.Failure(ResultErrors.NotFound("The request"))));

        await _strip.ReloadLinkedAsync([_list.Id]);

        Assert.That(_strip.Tabs[0], Is.SameAs(before));
    }

    [Test]
    public void OnlyDirtyTabsOfTheGivenRequestsAreCounted()
    {
        _strip.Tabs[0].Url = "http://typed/list";

        Assert.That(_strip.CountLinkedTabsWithEdits([_list.Id, _create.Id]), Is.EqualTo(1));
        Assert.That(_strip.CountLinkedTabsWithEdits([_create.Id]), Is.EqualTo(0));
        Assert.That(_strip.CountLinkedTabsWithEdits([]), Is.EqualTo(0));
    }

    private void Serve(RequestDefinition request)
    {
        A.CallTo(() => _requests.GetAsync(request.Id, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<RequestDefinition>.Success(request)));
    }
}
