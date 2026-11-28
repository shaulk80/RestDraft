using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Results;
using RestDraft.UI.Services;
using RestDraft.UI.Tests.ViewModels;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

[TestFixture]
public class ImportUrlWindowTests
{
    private const string PageAddress = "http://api.test/swagger";

    private Window _owner = null!;
    private FakeRemoteImport _import = null!;
    private ImportUrlViewModel _viewModel = null!;
    private ImportUrlWindow _window = null!;

    [SetUp]
    public void SetUp()
    {
        _import = new FakeRemoteImport();
        _viewModel = new ImportUrlViewModel(_import, PageAddress);
    }

    [TearDown]
    public void TearDown()
    {
        _owner.Close();
    }

    [AvaloniaTest]
    public void TheDialogOpensWithTheAddressSelected()
    {
        Show();

        var box = AddressBox();

        Assert.That(box.IsFocused, Is.True);
        Assert.That(box.SelectedText, Is.EqualTo(PageAddress));
    }

    [AvaloniaTest]
    public void AllControlsAreNamedWithTheHeadersAndTheListShown()
    {
        Show();
        _window.GetVisualDescendants().OfType<Expander>().Single().IsExpanded = true;
        _viewModel.Headers.AddRowCommand.Execute(null);
        StartAndFinish(FakeRemoteImport.Offering("orders", "users"));

        var list = _window.GetVisualDescendants().OfType<ComboBox>().Single();

        Assert.That(list.IsEffectivelyVisible, Is.True);
        Assert.That(list.SelectedIndex, Is.Zero);
        Assert.That(AccessibleNameChecker.FindUnnamed(_window), Is.Empty);
    }

    // A screen reader reads the value of the list. The address may hold a password and is not part of it.
    [AvaloniaTest]
    public void TheListIsReadAsTheNameOfTheSpecification()
    {
        Show();
        StartAndFinish(FakeRemoteImport.Offering("orders", "users"));
        var list = _window.GetVisualDescendants().OfType<ComboBox>().Single();

        var value = ControlAutomationPeer.CreatePeerForElement(list).GetProvider<IValueProvider>()?.Value;

        Assert.That(value, Is.EqualTo("orders"));
    }

    [AvaloniaTest]
    public void EnterInTheAddressDownloadsAndTheResultClosesTheDialog()
    {
        var closed = Show();

        _window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        WaitUntil(() => _import.Started.Task.IsCompleted);
        var isReadOnly = AddressBox().IsReadOnly;
        _import.Finish(FakeRemoteImport.Found(PageAddress));
        WaitUntil(() => closed.IsCompleted);

        Assert.That(isReadOnly, Is.True);
        Assert.That(closed.IsCompletedSuccessfully, Is.True);
        Assert.That(closed.Result?.TypedAddress, Is.EqualTo(new Uri(PageAddress)));
    }

    [AvaloniaTest]
    public void CancelWithoutADownloadClosesTheDialogWithNothing()
    {
        var closed = Show();

        Click("Cancel");

        Assert.That(closed.IsCompletedSuccessfully, Is.True);
        Assert.That(closed.Result, Is.Null);
    }

    [AvaloniaTest]
    public void CancelDuringADownloadStopsItAndASecondOneClosesTheDialog()
    {
        var closed = Show();
        _viewModel.NextCommand.Execute(null);
        WaitUntil(() => _import.Started.Task.IsCompleted);
        var token = _import.Token;

        Click("Cancel");
        var openAfterTheFirst = !closed.IsCompleted;
        _import.Finish(FakeRemoteImport.Found(PageAddress));
        WaitUntil(() => !_viewModel.IsBusy);
        Click("Cancel");

        Assert.That(token.IsCancellationRequested, Is.True);
        Assert.That(openAfterTheFirst, Is.True);
        Assert.That(closed.IsCompletedSuccessfully, Is.True);
        Assert.That(closed.Result, Is.Null);
    }

    [AvaloniaTest]
    public void ClosingTheDialogStopsItsDownload()
    {
        var closed = Show();
        _viewModel.NextCommand.Execute(null);
        WaitUntil(() => _import.Started.Task.IsCompleted);
        var token = _import.Token;

        _window.Close();
        _import.Finish(FakeRemoteImport.Found(PageAddress));
        WaitUntil(() => !_viewModel.IsBusy);

        Assert.That(token.IsCancellationRequested, Is.True);
        Assert.That(closed.Result, Is.Null);
    }

    // The download runs on another thread and comes back through the dispatcher, which a headless test has to run itself.
    private static void WaitUntil(Func<bool> condition)
    {
        var reached = SpinWait.SpinUntil(
            () =>
            {
                Dispatcher.UIThread.RunJobs();
                return condition();
            },
            TimeSpan.FromSeconds(10));
        Assert.That(reached, Is.True);
    }

    // The task completes with the result of the dialog when it closes.
    private Task<ImportUrlResult?> Show()
    {
        _owner = new Window();
        _owner.Show();
        _window = new ImportUrlWindow { DataContext = _viewModel };
        var closed = _window.ShowDialog<ImportUrlResult?>(_owner);
        Dispatcher.UIThread.RunJobs();
        return closed;
    }

    private void StartAndFinish(Result<RemoteImport> result)
    {
        _viewModel.NextCommand.Execute(null);
        WaitUntil(() => _import.Started.Task.IsCompleted);
        _import.Finish(result);
        WaitUntil(() => !_viewModel.IsBusy);
    }

    private TextBox AddressBox()
    {
        return _window.GetVisualDescendants().OfType<TextBox>()
            .Single(box => AutomationProperties.GetName(box) == "URL of the specification or of the Swagger UI page");
    }

    private void Click(string text)
    {
        var button = _window.GetVisualDescendants().OfType<Button>().Single(candidate => candidate.Content as string == text);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }
}
