using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// One item of the file as the preview shows it. The id is the one the item has in the document.
public sealed record PlanNode(
    Guid Id,
    string Name,
    PlanNodeKind Kind,
    PlanStatus Status,
    RequestMethod? Method,
    IReadOnlyList<PlanNode> Children);
