namespace RestDraft.Application.Exchange;

public sealed record ImportOutcome(
    Guid CollectionId,
    string CollectionName,
    ImportCounts Counts,
    IReadOnlyList<Guid> OverwrittenRequestIds,
    int WarningCount);
