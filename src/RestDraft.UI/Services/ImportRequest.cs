using RestDraft.Application.Exchange;

namespace RestDraft.UI.Services;

// What the user confirmed in the import preview. OpenTabsWithEdits is the number of open tabs that the import replaces
// although they have unsaved edits, so the caller can ask once more. BaseUrl goes in front of every request URL, and is
// null for a document whose URLs are complete.
public sealed record ImportRequest(
    ImportTarget Target,
    bool Overwrite,
    IReadOnlySet<Guid> TickedIds,
    int OpenTabsWithEdits,
    string? BaseUrl = null);
