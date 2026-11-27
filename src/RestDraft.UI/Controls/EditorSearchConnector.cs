using Avalonia.Threading;
using AvaloniaEdit.Rendering;
using RestDraft.Application.Search;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Controls;

// Shows a response search in an editor: highlights the matches, scrolls to the current one, and selects it when the
// panel closes. The editor is shared by all tabs, so the search that is connected changes with the tab.
public sealed class EditorSearchConnector
{
    private readonly CodeEditor _editor;
    private readonly SearchHighlightRenderer _renderer = new();
    private ResponseSearchViewModel? _search;

    public EditorSearchConnector(CodeEditor editor)
    {
        _editor = editor;
        _editor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
        // The editor adds a find box of its own with its template. It is removed, so two find boxes cannot compete.
        _editor.TemplateApplied += (_, _) => _editor.SearchPanel.Uninstall();
        _editor.ActualThemeVariantChanged += (_, _) => Redraw();
    }

    public void Connect(ResponseSearchViewModel? search)
    {
        if (_search is not null)
        {
            _search.ResultsChanged -= OnResultsChanged;
            _search.Closed -= OnClosed;
        }

        _search = search;
        _renderer.Search = search;
        if (search is not null)
        {
            search.ResultsChanged += OnResultsChanged;
            search.Closed += OnClosed;
        }

        ShowResults();
    }

    // A selection counts from its start, so the selected text itself is the current match.
    public void OpenFromSelection()
    {
        var cursor = _editor.SelectionLength > 0 ? _editor.SelectionStart : _editor.CaretOffset;
        _search?.Open(_editor.SelectedText, cursor);
    }

    public void OpenAtCursor()
    {
        _search?.Open(string.Empty, _editor.CaretOffset);
    }

    private void OnResultsChanged(object? sender, EventArgs e)
    {
        ShowResults();
    }

    private void OnClosed(object? sender, SearchClosedEventArgs e)
    {
        if (e.Match is { } match && IsInText(match))
        {
            _editor.Select(match.Start, match.Length);
        }

        _editor.TextArea.Focus();
    }

    // The scroll is posted, so a new text is laid out, and a panel that just opened has taken its room, before it.
    private void ShowResults()
    {
        Redraw();
        Dispatcher.UIThread.Post(ScrollToCurrentMatch, DispatcherPriority.Background);
    }

    private void ScrollToCurrentMatch()
    {
        if (_search?.CurrentMatch is not { } match || !IsInText(match))
        {
            return;
        }

        var location = _editor.Document.GetLocation(match.Start);
        _editor.ScrollTo(location.Line, location.Column);
    }

    // On a tab switch the editor may still hold the text of the other tab for a moment.
    private bool IsInText(TextMatch match)
    {
        return match.End <= _editor.Document.TextLength;
    }

    private void Redraw()
    {
        _editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }
}
