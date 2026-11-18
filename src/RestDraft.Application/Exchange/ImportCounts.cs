namespace RestDraft.Application.Exchange;

public sealed record ImportCounts(
    int NewFolders,
    int NewRequests,
    int OverwrittenRequests,
    int SkippedRequests)
{
    public static ImportCounts None { get; } = new(0, 0, 0, 0);

    // Whether importing changes anything. A folder that exists and has nothing new in it does not.
    public bool HasChanges => NewFolders + NewRequests + OverwrittenRequests > 0;

    public int ExistingRequests => OverwrittenRequests + SkippedRequests;
}
