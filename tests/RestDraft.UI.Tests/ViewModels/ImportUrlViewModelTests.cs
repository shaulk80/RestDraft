using NUnit.Framework;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class ImportUrlViewModelTests
{
    private const string PageAddress = "http://api.test/swagger";

    private FakeRemoteImport _import = null!;
    private ImportUrlViewModel _viewModel = null!;
    private List<ImportUrlResult> _found = null!;

    [SetUp]
    public void SetUp()
    {
        _import = new FakeRemoteImport();
        _viewModel = new ImportUrlViewModel(_import, PageAddress);
        _found = [];
        _viewModel.Found += _found.Add;
    }

    [Test]
    public void TheRememberedAddressIsFilledInWithoutHeaders()
    {
        Assert.That(_viewModel.Address, Is.EqualTo(PageAddress));
        Assert.That(_viewModel.Headers.Rows, Is.Empty);
        Assert.That(_viewModel.HasChoices, Is.False);
    }

    [TestCase("")]
    [TestCase("api.test/swagger")]
    [TestCase("ftp://api.test/spec.json")]
    public async Task AnAddressThatIsNotAWebAddressIsNotDownloaded(string address)
    {
        _viewModel.Address = address;

        await _viewModel.NextAsync();

        Assert.That(_viewModel.Error, Does.Contain("http://"));
        Assert.That(_import.Addresses, Is.Empty);
    }

    [Test]
    public async Task AFoundSpecificationEndsTheDialogWithBothAddresses()
    {
        await FinishAsync(FakeRemoteImport.Found("http://api.test/v1.json"));

        var result = _found.Single();
        Assert.That(result.TypedAddress, Is.EqualTo(new Uri(PageAddress)));
        Assert.That(result.DownloadAddress, Is.EqualTo(new Uri("http://api.test/v1.json")));
        Assert.That(result.Document.Collection.Name, Is.EqualTo("Shop"));
    }

    [Test]
    public async Task TheTypedHeadersGoWithTheTypedAddress()
    {
        _viewModel.Headers.AddRowCommand.Execute(null);
        _viewModel.Headers.Rows[0].Key = "Authorization";
        _viewModel.Headers.Rows[0].Value = "Bearer {{token}}";

        await FinishAsync(FakeRemoteImport.Found(PageAddress));

        Assert.That(_import.Headers?.AllowedAddress, Is.EqualTo(new Uri(PageAddress)));
        Assert.That(_import.Headers?.Entries, Is.EqualTo(new[] { new KeyValueEntry("Authorization", "Bearer {{token}}", true) }));
    }

    [Test]
    public async Task AFailureKeepsTheDialogWithTheReason()
    {
        await FinishAsync(Result<RemoteImport>.Failure(new Error(ErrorCategory.Network, "The server could not be reached.", "Refused.")));

        Assert.That(_viewModel.Error, Is.EqualTo("The server could not be reached. (Refused.)"));
        Assert.That(_viewModel.IsBusy, Is.False);
        Assert.That(_found, Is.Empty);
    }

    [Test]
    public async Task ADownloadInProgressIsShownAndNextWaitsForIt()
    {
        var running = _viewModel.NextAsync();
        await _import.Started.Task;

        var isBusy = _viewModel.IsBusy;
        var status = _viewModel.Status;
        var canContinue = _viewModel.NextCommand.CanExecute(null);
        await _viewModel.NextAsync();
        _import.Finish(FakeRemoteImport.Found(PageAddress));
        await running;

        Assert.That(isBusy, Is.True);
        Assert.That(status, Is.EqualTo("Downloading..."));
        Assert.That(canContinue, Is.False);
        Assert.That(_import.Addresses, Has.Count.EqualTo(1));
        Assert.That(_viewModel.IsBusy, Is.False);
        Assert.That(_viewModel.Status, Is.Empty);
        Assert.That(_viewModel.NextCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task SeveralSpecificationsAreOfferedAndTheChosenOneIsDownloaded()
    {
        await FinishAsync(FakeRemoteImport.Offering("orders", "users"));
        var offered = _viewModel.Choices.Select(choice => choice.Name).ToList();
        var preselected = _viewModel.SelectedChoice?.Name;
        var status = _viewModel.Status;

        _viewModel.SelectedChoice = _viewModel.Choices[1];
        await FinishAsync(FakeRemoteImport.Found("http://api.test/users.json"));

        Assert.That(offered, Is.EqualTo(new[] { "orders", "users" }));
        Assert.That(preselected, Is.EqualTo("orders"));
        Assert.That(status, Is.EqualTo("The page offers 2 specifications. Choose one and press Next."));
        Assert.That(_import.Addresses[1], Is.EqualTo(new Uri("http://api.test/users.json")));
        Assert.That(_import.Headers?.AllowedAddress, Is.EqualTo(new Uri(PageAddress)));
        Assert.That(_found.Single().TypedAddress, Is.EqualTo(new Uri(PageAddress)));
    }

    [Test]
    public async Task AFailedChoiceKeepsTheList()
    {
        await FinishAsync(FakeRemoteImport.Offering("orders", "users"));

        await FinishAsync(Result<RemoteImport>.Failure(ResultErrors.Validation("The specification is broken.")));

        Assert.That(_viewModel.HasChoices, Is.True);
        Assert.That(_viewModel.Error, Is.EqualTo("The specification is broken."));
    }

    [Test]
    public async Task ChangingTheAddressDropsTheList()
    {
        await FinishAsync(FakeRemoteImport.Offering("orders", "users"));

        _viewModel.Address = "http://api.test/other";

        Assert.That(_viewModel.HasChoices, Is.False);
        Assert.That(_viewModel.SelectedChoice, Is.Null);
        Assert.That(_viewModel.Status, Is.Empty);
    }

    // A specification of the list may need a header that the page did not.
    [Test]
    public async Task AHeaderAddedAfterwardsKeepsTheListAndGoesWithTheChoice()
    {
        await FinishAsync(FakeRemoteImport.Offering("orders", "users"));
        _viewModel.SelectedChoice = _viewModel.Choices[1];

        _viewModel.Headers.AddRowCommand.Execute(null);
        _viewModel.Headers.Rows[0].Key = "X-Token";
        await FinishAsync(FakeRemoteImport.Found("http://api.test/users.json"));

        Assert.That(_import.Addresses[1], Is.EqualTo(new Uri("http://api.test/users.json")));
        Assert.That(_import.Headers?.Entries.Single().Key, Is.EqualTo("X-Token"));
    }

    // The answer may still come after the cancellation, and must not be used.
    [Test]
    public async Task CancellingStopsTheDownloadAndKeepsTheDialog()
    {
        var running = _viewModel.NextAsync();
        await _import.Started.Task;
        var token = _import.Token;

        var stopped = _viewModel.CancelDownload();
        _import.Finish(FakeRemoteImport.Found(PageAddress));
        await running;

        Assert.That(stopped, Is.True);
        Assert.That(token.IsCancellationRequested, Is.True);
        Assert.That(_found, Is.Empty);
        Assert.That(_viewModel.Error, Is.EqualTo("The download was cancelled."));
        Assert.That(_viewModel.IsBusy, Is.False);
    }

    [Test]
    public async Task ThereIsNothingToCancelBeforeAndAfterADownload()
    {
        var before = _viewModel.CancelDownload();

        await FinishAsync(FakeRemoteImport.Found(PageAddress));

        Assert.That(before, Is.False);
        Assert.That(_viewModel.CancelDownload(), Is.False);
    }

    private async Task FinishAsync(Result<RemoteImport> result)
    {
        var running = _viewModel.NextAsync();
        await _import.Started.Task;
        _import.Finish(result);
        await running;
    }
}
