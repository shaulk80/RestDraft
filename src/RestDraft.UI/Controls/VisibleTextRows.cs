using AvaloniaEdit.Rendering;

namespace RestDraft.UI.Controls;

// The rows of text on screen. A wrapped line can have many thousands of rows, so only those in view are given.
public static class VisibleTextRows
{
    public static IEnumerable<VisibleTextRow> Of(TextView textView)
    {
        var top = textView.ScrollOffset.Y;
        var bottom = top + textView.Bounds.Height;
        return textView.VisualLines.SelectMany(line => Of(line, top, bottom));
    }

    // Each question to the line about a row walks the rows before it, so only the first row in view and the bottom of
    // each row in view are asked for.
    private static IEnumerable<VisibleTextRow> Of(VisualLine line, double top, double bottom)
    {
        var first = line.GetTextLineByVisualYPosition(top);
        var column = line.GetTextLineVisualStartColumn(first);
        var rowTop = line.GetTextLineVisualYPosition(first, VisualYPosition.LineTop);
        for (var index = line.TextLines.IndexOf(first); index < line.TextLines.Count && rowTop < bottom; index++)
        {
            var text = line.TextLines[index];
            var rowBottom = line.GetTextLineVisualYPosition(text, VisualYPosition.LineBottom);
            yield return new VisibleTextRow(line, text, column, rowTop, rowBottom - rowTop);
            column += text.Length;
            rowTop = rowBottom;
        }
    }
}
