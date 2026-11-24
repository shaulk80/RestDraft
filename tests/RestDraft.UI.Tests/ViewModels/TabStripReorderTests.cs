using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Collections;
using RestDraft.Application.Tabs;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

// Dragging a tab to another place in the strip.
[TestFixture]
public class TabStripReorderTests
{
    private ITabService _tabService = null!;
    private AutosaveScheduler _autosave = null!;
    private TabStripViewModel _strip = null!;
    private IReadOnlyList<TabSnapshot>? _stored;

    [SetUp]
    public void SetUp()
    {
        _stored = null;
        _tabService = TabStripFactory.CreateTabService();
        A.CallTo(() => _tabService.SaveAsync(A<IReadOnlyList<TabSnapshot>>._, A<CancellationToken>._))
            .Invokes((IReadOnlyList<TabSnapshot> tabs, CancellationToken _) => _stored = tabs)
            .Returns(Task.FromResult(Result.Success()));
        _autosave = new AutosaveScheduler(TabStripFactory.NeverDelay);
        var services = RequestTabServicesFactory.Create(A.Fake<IRequestSender>(), A.Fake<IRequestService>(), A.Fake<IDestinationPicker>());
        _strip = TabStripFactory.Create(services, _tabService, autosave: _autosave);
        foreach (var name in new[] { "A", "B", "C", "D" })
        {
            _strip.OpenUnsaved(new RequestDefinition { Id = Guid.NewGuid(), Name = name });
        }
    }

    [TearDown]
    public void TearDown()
    {
        _autosave.Dispose();
    }

    [TestCase(0, 3, "BCDA")]
    [TestCase(3, 0, "DABC")]
    [TestCase(1, 2, "ACBD")]
    [TestCase(2, 1, "ACBD")]
    [TestCase(0, 0, "ABCD")]
    [TestCase(0, 4, "ABCD")]
    [TestCase(0, -1, "ABCD")]
    public void MovingATabPutsItAtTheNewIndex(int from, int to, string expected)
    {
        _strip.MoveTab(_strip.Tabs[from], to);

        Assert.That(string.Concat(_strip.Tabs.Select(tab => tab.Name)), Is.EqualTo(expected));
    }

    [Test]
    public void MovingATabThatIsNotInTheStripChangesNothing()
    {
        var other = new TabStripViewModel(
            RequestTabServicesFactory.Create(A.Fake<IRequestSender>(), A.Fake<IRequestService>(), A.Fake<IDestinationPicker>()),
            _tabService,
            TabStripFactory.CreatePrompt(SaveChangesChoice.Cancel),
            _autosave);
        other.NewTab();

        _strip.MoveTab(other.Tabs[0], 1);

        Assert.That(string.Concat(_strip.Tabs.Select(tab => tab.Name)), Is.EqualTo("ABCD"));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(3)]
    public void TheSelectedTabStaysSelectedWhenAnotherTabMoves(int selectedIndex)
    {
        var selected = _strip.Tabs[selectedIndex];
        _strip.SelectedTab = selected;

        _strip.MoveTab(_strip.Tabs[1], 3);

        Assert.That(_strip.SelectedTab, Is.SameAs(selected));
    }

    [Test]
    public async Task TheNewOrderIsStored()
    {
        await _strip.RestoreAsync();

        _strip.MoveTab(_strip.Tabs[0], 3);
        await _strip.FlushAsync();

        Assert.That(_stored, Is.Not.Null);
        Assert.That(string.Concat(_stored!.Select(snapshot => snapshot.Current.Name)), Is.EqualTo("BCDA"));
    }

    [TestCase(0, 3, true, 3)]
    [TestCase(0, 3, false, 2)]
    [TestCase(0, 1, true, 1)]
    [TestCase(0, 1, false, 0)]
    [TestCase(0, 0, true, 0)]
    [TestCase(3, 0, false, 0)]
    [TestCase(3, 0, true, 1)]
    [TestCase(3, 2, true, 3)]
    [TestCase(3, 2, false, 2)]
    [TestCase(1, 3, true, 3)]
    public void TheDropIndexAccountsForTheTabLeavingItsPlace(int source, int target, bool afterTarget, int expected)
    {
        Assert.That(TabDropIndex.Of(source, target, afterTarget), Is.EqualTo(expected));
    }
}
