using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FakeItEasy;
using NUnit.Framework;
using RestDraft.Domain.Bodies;
using RestDraft.UI.Controls;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;
using RestDraft.UI.Tests.ViewModels;

namespace RestDraft.UI.Tests.Views;

[TestFixture]
public class BodyEditorViewLayoutTests
{
    [AvaloniaTest]
    public void RawBodyShowsEditorWithHeight()
    {
        var window = ShowEditor(new RawBody("text/plain", "hello"));

        var editor = window.GetVisualDescendants().OfType<CodeEditor>().Single();

        Assert.That(editor.IsEffectivelyVisible, Is.True);
        Assert.That(editor.TextArea.Bounds.Height, Is.GreaterThan(0));
    }

    [AvaloniaTest]
    public void NoBodyStillShowsEditorSectionHint()
    {
        var window = ShowEditor(new NoBody());

        var hint = window.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "NoBodyHint");

        Assert.That(hint.IsEffectivelyVisible, Is.True);
    }

    [AvaloniaTest]
    [TestCase(400)]
    [TestCase(600)]
    [TestCase(900)]
    public void RawEditorFillsTheHeightOfTheWindow(int windowHeight)
    {
        var window = ShowEditor(new RawBody("text/plain", "hello"), windowHeight);

        var editor = window.GetVisualDescendants().OfType<CodeEditor>().Single();

        // The toolbar and the margins take well under 100 pixels.
        Assert.That(editor.Bounds.Height, Is.GreaterThan(windowHeight - 100));
    }

    [AvaloniaTest]
    public void FormatErrorIsShownInTheToolbarWithoutTakingHeightFromTheEditor()
    {
        var body = new RawBody("application/json", "{ not json");
        var window = ShowEditor(body, 600);
        var viewModel = (BodyEditorViewModel)((BodyEditorView)window.Content!).DataContext!;
        var editor = window.GetVisualDescendants().OfType<CodeEditor>().Single();
        var heightBefore = editor.Bounds.Height;

        viewModel.FormatCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var message = window.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "FormatMessage");
        Assert.That(message.IsEffectivelyVisible, Is.True);
        Assert.That(message.Text, Is.Not.Empty);
        Assert.That(editor.Bounds.Height, Is.EqualTo(heightBefore));
    }

    [AvaloniaTest]
    public void MessageIsHiddenWhenThereIsNone()
    {
        var window = ShowEditor(new RawBody("application/json", "{}"), 600);

        var message = window.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "FormatMessage");

        Assert.That(message.IsEffectivelyVisible, Is.False);
    }

    private static Window ShowEditor(RestDraft.Domain.Bodies.BodyDefinition body, int windowHeight = 600)
    {
        var viewModel = new BodyEditorViewModel(RequestTabServicesFactory.CreateFormatting(), A.Fake<IFilePickerService>(), body);
        var window = new Window
        {
            Width = 800,
            Height = windowHeight,
            Content = new BodyEditorView { DataContext = viewModel },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
