using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// What a file holds: one collection, and what was left out or changed while reading it.
public sealed record ExchangeDocument(Collection Collection, IReadOnlyList<ImportWarning> Warnings);
