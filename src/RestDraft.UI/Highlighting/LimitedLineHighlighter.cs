using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;

namespace RestDraft.UI.Highlighting;

// Colors only the start of a very long line. The time the editor needs to draw a line grows with the square of the
// number of colored tokens in it, so a minified body on one line would freeze the program for minutes.
public sealed class LimitedLineHighlighter(IHighlighter inner) : IHighlighter
{
    public const int ColoredCharactersPerLine = 10_000;

    public IDocument Document => inner.Document;

    public HighlightingColor DefaultTextColor => inner.DefaultTextColor;

    public event HighlightingStateChangedEventHandler HighlightingStateChanged
    {
        add => inner.HighlightingStateChanged += value;
        remove => inner.HighlightingStateChanged -= value;
    }

    public HighlightedLine HighlightLine(int lineNumber)
    {
        var line = inner.HighlightLine(lineNumber);
        var limit = line.DocumentLine.Offset + ColoredCharactersPerLine;
        // The sections are ordered by where they start, so the ones to drop are at the end.
        while (line.Sections.Count > 0 && line.Sections[^1].Offset >= limit)
        {
            line.Sections.RemoveAt(line.Sections.Count - 1);
        }

        return line;
    }

    public IEnumerable<HighlightingColor> GetColorStack(int lineNumber)
    {
        return inner.GetColorStack(lineNumber);
    }

    public void UpdateHighlightingState(int lineNumber)
    {
        inner.UpdateHighlightingState(lineNumber);
    }

    public void BeginHighlighting()
    {
        inner.BeginHighlighting();
    }

    public void EndHighlighting()
    {
        inner.EndHighlighting();
    }

    public HighlightingColor GetNamedColor(string name)
    {
        return inner.GetNamedColor(name);
    }

    public void Dispose()
    {
        inner.Dispose();
    }
}
