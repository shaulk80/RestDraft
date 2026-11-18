using RestDraft.Domain.Collections;

namespace RestDraft.Application.Search;

// What the search popup asks for. Text matches any part of a name or URL, ignoring case, and Method matches exactly.
// InBodies adds the request bodies to the places the text is looked for. A filter without text and method is not active.
public sealed record RequestFilter(string Text, RequestMethod? Method, bool InBodies)
{
    public static RequestFilter None { get; } = new(string.Empty, null, false);

    public bool IsActive => Text.Length > 0 || Method is not null;
}
