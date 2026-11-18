namespace RestDraft.Application.Exchange;

internal sealed class MergeTally
{
    public int NewFolders { get; set; }

    public int NewRequests { get; set; }

    public int OverwrittenRequests { get; set; }

    public int SkippedRequests { get; set; }

    public List<Guid> OverwrittenRequestIds { get; } = [];

    public ImportCounts ToCounts()
    {
        return new ImportCounts(NewFolders, NewRequests, OverwrittenRequests, SkippedRequests);
    }
}
