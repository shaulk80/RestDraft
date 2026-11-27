using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using RestDraft.Application.Search;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Controls;

// Paints the search matches behind the text of an editor. Only the matches on screen are painted, because a long text can
// hold many thousands.
public sealed class SearchHighlightRenderer : IBackgroundRenderer
{
    private const string MatchBrushKey = "SearchMatchBrush";
    private const string CurrentBrushKey = "SearchCurrentMatchBrush";
    private const string CurrentBorderBrushKey = "SearchCurrentMatchBorderBrush";
    private const double CurrentBorderThickness = 1.5;

    public KnownLayer Layer => KnownLayer.Selection;

    public ResponseSearchViewModel? Search { get; set; }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (Search is not { } search || search.Matches.Count == 0 || !textView.VisualLinesValid)
        {
            return;
        }

        var matchBrush = FindBrush(textView, MatchBrushKey);
        var currentBrush = FindBrush(textView, CurrentBrushKey);
        var currentBorder = new Pen(FindBrush(textView, CurrentBorderBrushKey), CurrentBorderThickness);
        foreach (var highlight in Highlights(textView, search.Matches))
        {
            var isCurrent = highlight.Match == search.CurrentMatch;
            drawingContext.DrawRectangle(isCurrent ? currentBrush : matchBrush, isCurrent ? currentBorder : null, highlight.Area);
        }
    }

    // Where the matches on screen are, relative to the editor. BackgroundGeometryBuilder of the editor is not used: for a
    // match far down a very long wrapped line it takes a good part of a second, for each match and each repaint.
    public static IEnumerable<SearchHighlight> Highlights(TextView textView, IReadOnlyList<TextMatch> matches)
    {
        var scrolled = -textView.ScrollOffset;
        foreach (var row in VisibleTextRows.Of(textView))
        {
            foreach (var match in TextMatches.Overlapping(matches, row.StartOffset, row.EndOffset))
            {
                foreach (var area in row.AreasOf(match))
                {
                    yield return new SearchHighlight(match, area.Translate(scrolled));
                }
            }
        }
    }

    private static IBrush? FindBrush(TextView textView, string key)
    {
        return textView.TryFindResource(key, textView.ActualThemeVariant, out var resource) ? resource as IBrush : null;
    }
}
