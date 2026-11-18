using RestDraft.Domain.Collections;

namespace RestDraft.Application.History;

// UrlText matches any part of the resolved URL, ignoring case. An empty text matches every URL.
// Method, when set, matches exactly. InBodies also accepts an entry whose request body contains the text.
public sealed record HistoryFilter(string UrlText, RequestMethod? Method, bool InBodies = false)
{
    public static HistoryFilter All { get; } = new(string.Empty, null);
}
