using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;
using RestDraft.Application.Search;

namespace RestDraft.UI.Controls;

// One row of text on screen: a line of the editor, or one of the rows a wrapped line is shown in. Positions are those in
// the whole document, before scrolling.
public sealed record VisibleTextRow(VisualLine Line, TextLine Text, int StartColumn, double Top, double Height)
{
    public int StartOffset { get; } = OffsetOf(Line, StartColumn);

    public int EndOffset { get; } = OffsetOf(Line, StartColumn + Text.Length);

    // The areas taken by the part of the match that is in this row.
    public IEnumerable<Rect> AreasOf(TextMatch match)
    {
        var start = ColumnOf(Math.Max(match.Start, StartOffset));
        var end = ColumnOf(Math.Min(match.End, EndOffset));
        if (end <= start)
        {
            return [];
        }

        return Text.GetTextBounds(start, end - start)
            .Select(bounds => new Rect(bounds.Rectangle.X, Top, bounds.Rectangle.Width, Height));
    }

    private static int OffsetOf(VisualLine line, int column)
    {
        return line.FirstDocumentLine.Offset + line.GetRelativeOffset(Math.Min(column, line.VisualLength));
    }

    private int ColumnOf(int offset)
    {
        return Line.GetVisualColumn(offset - Line.FirstDocumentLine.Offset);
    }
}
