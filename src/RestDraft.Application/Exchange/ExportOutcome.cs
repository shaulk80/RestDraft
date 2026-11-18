namespace RestDraft.Application.Exchange;

public sealed record ExportOutcome(string Path, int Folders, int Requests);
