using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// The folders (not the root) and the requests under a folder.
public sealed record TreeCounts(int Folders, int Requests)
{
    public bool IsEmpty => Folders + Requests == 0;

    public static TreeCounts Of(Folder root)
    {
        var inner = root.Folders.Select(Of).Aggregate(new TreeCounts(0, 0), (sum, next) => sum + next);
        return new TreeCounts(root.Folders.Count, root.Requests.Count) + inner;
    }

    public static TreeCounts operator +(TreeCounts first, TreeCounts second)
    {
        return new TreeCounts(first.Folders + second.Folders, first.Requests + second.Requests);
    }
}
