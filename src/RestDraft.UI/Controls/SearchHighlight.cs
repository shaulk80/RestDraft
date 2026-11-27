using Avalonia;
using RestDraft.Application.Search;

namespace RestDraft.UI.Controls;

// An area of the editor to paint for a match. A match that wraps has more than one.
public sealed record SearchHighlight(TextMatch Match, Rect Area);
