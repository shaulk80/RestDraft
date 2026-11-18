namespace RestDraft.UI.Services;

// What the user chose in the export window.
public sealed record ExportSelection(Guid CollectionId, string CollectionName, IReadOnlySet<Guid> TickedIds);
