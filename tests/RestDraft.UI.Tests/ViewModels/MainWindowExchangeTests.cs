using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Environments;
using RestDraft.Application.History;
using RestDraft.Application.Secrets;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class MainWindowExchangeTests
{
    private TreeFixtures _fixtures = null!;
    private IExchangeDialogs _exchange = null!;
    private MainWindowViewModel _window = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixtures = new TreeFixtures();
        _exchange = A.Fake<IExchangeDialogs>();
        var sidebar = _fixtures.CreateSidebar();
        var settings = SettingsServiceFactory.Create();
        _window = new MainWindowViewModel(
            sidebar,
            new HistoryListViewModel(A.Fake<IHistoryService>()),
            TabStripFactory.Create(RequestTabServicesFactory.Create(A.Fake<IRequestSender>(), _fixtures.Requests, A.Fake<IDestinationPicker>())),
            new EnvironmentSelectorViewModel(A.Fake<IEnvironmentService>(), A.Fake<IEnvironmentDialogs>()),
            new SecretStatusViewModel(A.Fake<ISecretService>(), A.Fake<ISecretPrompt>()),
            new WorkspaceLayoutViewModel(settings),
            settings,
            A.Fake<ISettingsDialogs>(),
            _exchange);
        await sidebar.ReloadAsync();
    }

    [Test]
    public async Task ImportReloadsTheTreeSelectsTheCollectionAndShowsTheMessage()
    {
        A.CallTo(() => _exchange.ImportAsync(A<Guid?>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ExchangeOutcome.Imported("Imported 2 new requests into 'Shop'.", _fixtures.Shop.Id, [])));

        _window.ImportCommand.Execute(null);
        await WaitUntil(() => _window.Notice.Length > 0);

        Assert.That(_window.Notice, Is.EqualTo("Imported 2 new requests into 'Shop'."));
        Assert.That(_window.Sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Shop.Id));
        Assert.That(_window.Sidebar.StatusMessage, Is.Empty);
    }

    [Test]
    public async Task ImportFromAnAddressOffersTheSelectedCollectionAndShowsTheOutcome()
    {
        _window.Sidebar.SelectedNode = _window.Sidebar.Roots.Single().Children.First();
        A.CallTo(() => _exchange.ImportFromUrlAsync(A<Guid?>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ExchangeOutcome.Imported("Imported 2 new requests into 'Shop'.", _fixtures.Shop.Id, [])));

        _window.ImportFromUrlCommand.Execute(null);
        await WaitUntil(() => _window.Notice.Length > 0);

        Assert.That(_window.Notice, Is.EqualTo("Imported 2 new requests into 'Shop'."));
        Assert.That(_window.Sidebar.SelectedNode?.Id, Is.EqualTo(_fixtures.Shop.Id));
        A.CallTo(() => _exchange.ImportFromUrlAsync(_fixtures.Shop.Id, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task TheCollectionSelectedInTheTreeIsOfferedAsTheTarget()
    {
        _window.Sidebar.SelectedNode = _window.Sidebar.Roots.Single().Children.First();
        A.CallTo(() => _exchange.ImportAsync(A<Guid?>._, A<CancellationToken>._)).Returns(Task.FromResult(ExchangeOutcome.Cancelled));

        _window.ImportCommand.Execute(null);
        await WaitUntil(() => Fake.GetCalls(_exchange).Any());

        A.CallTo(() => _exchange.ImportAsync(_fixtures.Shop.Id, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AFailedImportIsShownAsAnError()
    {
        A.CallTo(() => _exchange.ImportAsync(A<Guid?>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ExchangeOutcome.Failed("The file is not in a supported format.")));

        _window.ImportCommand.Execute(null);
        await WaitUntil(() => _window.Sidebar.StatusMessage.Length > 0);

        Assert.That(_window.Sidebar.StatusMessage, Is.EqualTo("The file is not in a supported format."));
        Assert.That(_window.Notice, Is.Empty);
    }

    [Test]
    public async Task ACancelledDialogLeavesTheMessagesEmpty()
    {
        A.CallTo(() => _exchange.ExportAsync(A<Guid?>._, A<CancellationToken>._)).Returns(Task.FromResult(ExchangeOutcome.Cancelled));

        _window.ExportCommand.Execute(null);
        await WaitUntil(() => Fake.GetCalls(_exchange).Any());
        await Task.Delay(20);

        Assert.That(_window.Notice, Is.Empty);
        Assert.That(_window.Sidebar.StatusMessage, Is.Empty);
    }

    [Test]
    public async Task ExportShowsTheMessageWithoutReloadingTheTree()
    {
        A.CallTo(() => _exchange.ExportAsync(A<Guid?>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ExchangeOutcome.Done("Exported 2 requests to C:\\x.yaml.")));

        _window.ExportCommand.Execute(null);
        await WaitUntil(() => _window.Notice.Length > 0);

        Assert.That(_window.Notice, Is.EqualTo("Exported 2 requests to C:\\x.yaml."));
        A.CallTo(() => _fixtures.Collections.ListAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExportIsDisabledWithoutCollectionsAndEnabledAfterOneAppears()
    {
        var sidebar = _window.Sidebar;
        _fixtures.SetCollections([]);
        await sidebar.ReloadAsync();
        var disabled = !_window.ExportCommand.CanExecute(null);

        _fixtures.SetCollections([_fixtures.Shop]);
        await sidebar.ReloadAsync();

        Assert.That(disabled, Is.True);
        Assert.That(_window.ExportCommand.CanExecute(null), Is.True);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(20);
        }
    }
}
