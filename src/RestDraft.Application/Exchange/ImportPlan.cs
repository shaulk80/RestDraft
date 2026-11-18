using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// What an import does. The preview shows it and the import applies it, so both are the same.
public sealed record ImportPlan(
    IReadOnlyList<Collection> Collections,
    Guid TargetCollectionId,
    string TargetCollectionName,
    PlanNode Tree,
    ImportCounts Counts,
    IReadOnlyList<Guid> OverwrittenRequestIds);
