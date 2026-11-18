using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// Puts the content of a source folder into a target folder. Items match by name: a folder with a folder, a request with a
// request, in the same parent. Nothing in the target is deleted.
internal sealed class FolderMerger
{
    private readonly bool _overwrite;
    private readonly MergeTally _tally;

    public FolderMerger(bool overwrite, MergeTally tally)
    {
        _overwrite = overwrite;
        _tally = tally;
    }

    public Folder Merge(Folder target, Folder source)
    {
        var folders = target.Folders.ToList();
        var requests = target.Requests.ToList();
        foreach (var child in source.Folders)
        {
            MergeFolder(folders, requests, child);
        }

        foreach (var request in source.Requests)
        {
            MergeRequest(folders, requests, request);
        }

        return target with { Folders = folders, Requests = requests };
    }

    private void MergeFolder(List<Folder> folders, List<RequestDefinition> requests, Folder child)
    {
        var index = folders.FindIndex(existing => ChildNameRules.IsSameName(existing.Name, child.Name));
        if (index >= 0)
        {
            folders[index] = Merge(folders[index], child);
            return;
        }

        var counts = TreeCounts.Of(child);
        _tally.NewFolders += 1 + counts.Folders;
        _tally.NewRequests += counts.Requests;
        folders.Add(child with { Name = UniqueName(child.Name, folders, requests) });
    }

    private void MergeRequest(List<Folder> folders, List<RequestDefinition> requests, RequestDefinition request)
    {
        var index = requests.FindIndex(existing => ChildNameRules.IsSameName(existing.Name, request.Name));
        if (index < 0)
        {
            _tally.NewRequests++;
            requests.Add(request with { Name = UniqueName(request.Name, folders, requests) });
            return;
        }

        if (!_overwrite)
        {
            _tally.SkippedRequests++;
            return;
        }

        var existing = requests[index];
        requests[index] = request with { Id = existing.Id, Name = existing.Name };
        _tally.OverwrittenRequests++;
        _tally.OverwrittenRequestIds.Add(existing.Id);
    }

    // A folder and a request cannot share a name, so a new item that meets the other kind gets a number.
    private static string UniqueName(string name, List<Folder> folders, List<RequestDefinition> requests)
    {
        var taken = folders.Select(folder => folder.Name).Concat(requests.Select(request => request.Name)).ToList();
        return ChildNameRules.NextAvailableName(name, taken);
    }
}
