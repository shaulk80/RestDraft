using RestDraft.Application.Collections;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

// Works out what an import does, without changing anything. It is pure, so the preview can ask again at every tick.
public static class ImportPlanner
{
    public static Result<ImportPlan> Plan(
        IReadOnlyList<Collection> existing,
        Collection source,
        ImportTarget target,
        bool overwrite,
        IReadOnlySet<Guid> ticked)
    {
        var resolved = ResolveTarget(existing, source, target);
        if (!resolved.IsSuccess)
        {
            return Result<ImportPlan>.Failure(resolved.Error!);
        }

        var targetCollection = resolved.Value!;
        var selected = SelectionPruner.Prune(source, ticked);
        var tally = new MergeTally();
        var merged = new FolderMerger(overwrite, tally).Merge(targetCollection.Root, selected.Root);
        var tree = PlanTree.Build(source, target.IsNewCollection ? null : targetCollection.Root);
        return Result<ImportPlan>.Success(new ImportPlan(
            Replace(existing, targetCollection, merged, target.IsNewCollection),
            targetCollection.Id,
            targetCollection.Name,
            tree,
            tally.ToCounts(),
            tally.OverwrittenRequestIds));
    }

    private static Result<Collection> ResolveTarget(IReadOnlyList<Collection> existing, Collection source, ImportTarget target)
    {
        if (target.ExistingCollectionId is { } id)
        {
            var found = CollectionSet.Find(existing, id);
            return found is null
                ? Result<Collection>.Failure(ResultErrors.NotFound("The target collection"))
                : Result<Collection>.Success(found);
        }

        var nameResult = ChildNameRules.ValidateName(target.NewCollectionName);
        if (!nameResult.IsSuccess)
        {
            return Result<Collection>.Failure(nameResult.Error!);
        }

        var created = new Collection(source.Id, target.NewCollectionName.Trim(), source.Root with { Folders = [], Requests = [] });
        return Result<Collection>.Success(created);
    }

    private static List<Collection> Replace(IReadOnlyList<Collection> existing, Collection target, Folder merged, bool isNew)
    {
        var changed = target with { Root = merged };
        if (isNew)
        {
            return [.. existing, changed];
        }

        return CollectionSet.Replace(existing, target.Id, _ => changed);
    }
}
