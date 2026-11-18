using RestDraft.Domain.Collections;

namespace RestDraft.Application.Search;

// Prunes a copy of the collections to what the filter finds. Nothing is changed in the stored collections.
// A request is kept when it matches. A collection or folder is kept when it holds a kept item, or when its own name
// matches the text (a method filter cannot match a name, so then only containers that hold a matching request stay).
// A container whose name matches keeps its whole subtree, narrowed by the method only.
// KeepId is an item the user has just created or renamed: it stays visible even when it does not match.
public static class CollectionSearch
{
    public static IReadOnlyList<Collection> Apply(IReadOnlyList<Collection> collections, RequestFilter filter, Guid? keepId)
    {
        if (!filter.IsActive)
        {
            return collections;
        }

        return collections
            .Select(collection => PruneCollection(collection, filter, keepId))
            .OfType<Collection>()
            .ToList();
    }

    private static Collection? PruneCollection(Collection collection, RequestFilter filter, Guid? keepId)
    {
        var matched = RequestMatcher.NameMatches(collection.Name, filter);
        var root = PruneFolder(collection.Root, filter, matched, keepId);
        var isKept = HasContent(root) || collection.Id == keepId || (matched && filter.Method is null);
        return isKept ? collection with { Root = root } : null;
    }

    private static Folder? PruneChild(Folder folder, RequestFilter filter, bool matchedByParent, Guid? keepId)
    {
        var matched = matchedByParent || RequestMatcher.NameMatches(folder.Name, filter);
        var pruned = PruneFolder(folder, filter, matched, keepId);
        var isKept = HasContent(pruned) || folder.Id == keepId || (matched && filter.Method is null);
        return isKept ? pruned : null;
    }

    private static Folder PruneFolder(Folder folder, RequestFilter filter, bool matched, Guid? keepId)
    {
        return folder with
        {
            Folders = folder.Folders
                .Select(child => PruneChild(child, filter, matched, keepId))
                .OfType<Folder>()
                .ToList(),
            Requests = folder.Requests
                .Where(request => request.Id == keepId || RequestMatcher.Matches(request, filter, matched))
                .ToList(),
        };
    }

    private static bool HasContent(Folder folder)
    {
        return folder.Folders.Count > 0 || folder.Requests.Count > 0;
    }
}
