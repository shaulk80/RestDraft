using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// Cuts a collection down to what the user ticked. A ticked request keeps its folders, a ticked folder keeps all of its
// content, and the collection (or its root folder) ticked keeps everything. The root folder always stays.
public static class SelectionPruner
{
    public static Collection Prune(Collection collection, IReadOnlySet<Guid> ticked)
    {
        var allTicked = ticked.Contains(collection.Id);
        var root = PruneFolder(collection.Root, ticked, allTicked) ?? collection.Root with { Folders = [], Requests = [] };
        return collection with { Root = root };
    }

    // Null when nothing in the folder is kept.
    private static Folder? PruneFolder(Folder folder, IReadOnlySet<Guid> ticked, bool isInsideTicked)
    {
        var isTicked = isInsideTicked || ticked.Contains(folder.Id);
        var folders = folder.Folders
            .Select(child => PruneFolder(child, ticked, isTicked))
            .OfType<Folder>()
            .ToList();
        var requests = folder.Requests
            .Where(request => isTicked || ticked.Contains(request.Id))
            .ToList();
        if (!isTicked && folders.Count == 0 && requests.Count == 0)
        {
            return null;
        }

        return folder with { Folders = folders, Requests = requests };
    }
}
