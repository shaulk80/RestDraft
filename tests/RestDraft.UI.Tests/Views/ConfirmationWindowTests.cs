using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

[TestFixture]
public class ConfirmationWindowTests
{
    private ConfirmationWindow? _window;

    [TearDown]
    public void TearDown()
    {
        _window?.Close();
    }

    [TestCaseSource(nameof(Questions))]
    [AvaloniaTest]
    public void TheWindowShowsTheWordsOfItsQuestion(ConfirmationRequest question, string title, string confirm)
    {
        _window = new ConfirmationWindow { DataContext = new ConfirmationViewModel(question) };

        _window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = _window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToList();
        var buttons = _window.GetVisualDescendants().OfType<Button>().Select(button => button.Content as string).ToList();
        Assert.That(_window.Title, Is.EqualTo(title));
        Assert.That(texts, Does.Contain(question.Message));
        Assert.That(buttons, Is.EqualTo(new[] { confirm, "Cancel" }));
    }

    private static IEnumerable<TestCaseData> Questions()
    {
        yield return new TestCaseData(ConfirmationRequest.Delete("Delete 'Login'?"), "Confirm delete", "Delete")
            .SetArgDisplayNames("Delete");
        yield return new TestCaseData(ConfirmationRequest.Import("Import anyway?"), "Confirm import", "Import")
            .SetArgDisplayNames("Import");
    }
}
