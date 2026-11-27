using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using NUnit.Framework;
using RestDraft.UI.Controls;
using RestDraft.UI.Highlighting;
using RestDraft.UI.Services;

namespace RestDraft.UI.Tests.Highlighting;

[TestFixture]
public class LimitedLineHighlighterTests
{
    private const int Limit = LimitedLineHighlighter.ColoredCharactersPerLine;

    // The second line checks that the limit counts from the start of the line, not of the document.
    [TestCase(1)]
    [TestCase(2)]
    [AvaloniaTest]
    public void OnlyTheStartOfALongLineIsColored(int lineNumber)
    {
        var longLine = "[" + string.Join(",", Enumerable.Repeat("1", Limit)) + "]";
        var document = new TextDocument(longLine + "\n" + longLine);
        using var highlighter = Highlighter(document);

        var line = highlighter.HighlightLine(lineNumber);

        var lineStart = document.GetLineByNumber(lineNumber).Offset;
        var starts = line.Sections.Select(section => section.Offset - lineStart).ToList();
        Assert.That(starts, Has.All.LessThan(Limit));
        // A number is every second character, so the colored ones reach up to the limit.
        Assert.That(starts.Max(), Is.GreaterThanOrEqualTo(Limit - 2));
    }

    [TestCase("""{"a": 1}""", 2)]
    [TestCase("""{"a": "b", "c": true}""", 4)]
    [AvaloniaTest]
    public void AShortLineKeepsAllItsColors(string text, int expectedSections)
    {
        using var highlighter = Highlighter(new TextDocument(text));

        var line = highlighter.HighlightLine(1);

        Assert.That(line.Sections, Has.Count.EqualTo(expectedSections));
    }

    [AvaloniaTest]
    public void TheEditorColorsThroughTheLimit()
    {
        var editor = new CodeEditor { EditorText = "{\"a\": 1}", Language = EditorLanguage.Json };
        var window = new Window { Content = editor };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var highlighter = editor.TextArea.TextView.GetService(typeof(IHighlighter));

        Assert.That(highlighter, Is.InstanceOf<LimitedLineHighlighter>());
        Assert.That(editor.TextArea.TextView.LineTransformers.OfType<LimitedLineColorizer>().Count(), Is.EqualTo(1));
        window.Close();
    }

    private static LimitedLineHighlighter Highlighter(TextDocument document)
    {
        var definition = SyntaxTheme.Light.DefinitionFor(EditorLanguage.Json)!;
        return new LimitedLineHighlighter(new DocumentHighlighter(document, definition));
    }
}
