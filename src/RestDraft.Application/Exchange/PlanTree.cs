using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// The tree of the preview: every item of the file, and whether the target already has an item of that name.
internal static class PlanTree
{
    // targetRoot is null for a collection that does not exist yet, where everything is new.
    public static PlanNode Build(Collection source, Folder? targetRoot)
    {
        var status = targetRoot is null ? PlanStatus.New : PlanStatus.Exists;
        return new PlanNode(source.Id, source.Name, PlanNodeKind.Collection, status, null, Children(source.Root, targetRoot));
    }

    private static List<PlanNode> Children(Folder source, Folder? target)
    {
        var folders = source.Folders.Select(child => FolderNode(child, target));
        var requests = source.Requests.Select(request => RequestNode(request, target));
        return [.. folders, .. requests];
    }

    private static PlanNode FolderNode(Folder folder, Folder? targetParent)
    {
        var match = targetParent?.Folders.FirstOrDefault(existing => ChildNameRules.IsSameName(existing.Name, folder.Name));
        var status = match is null ? PlanStatus.New : PlanStatus.Exists;
        return new PlanNode(folder.Id, folder.Name, PlanNodeKind.Folder, status, null, Children(folder, match));
    }

    private static PlanNode RequestNode(RequestDefinition request, Folder? targetParent)
    {
        var exists = targetParent?.Requests.Any(existing => ChildNameRules.IsSameName(existing.Name, request.Name)) ?? false;
        return new PlanNode(
            request.Id,
            request.Name,
            PlanNodeKind.Request,
            exists ? PlanStatus.Exists : PlanStatus.New,
            request.Method,
            []);
    }
}
