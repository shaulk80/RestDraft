using RestDraft.Application.Search;

namespace RestDraft.UI.ViewModels;

// The match that was current when the search panel closed, so the editor can select it.
public sealed class SearchClosedEventArgs : EventArgs
{
    public SearchClosedEventArgs(TextMatch? match)
    {
        Match = match;
    }

    public TextMatch? Match { get; }
}
