namespace RestDraft.UI.ViewModels;

// Turns "this node was dropped before, inside or after that row" into the arguments of a move.
// A folder keeps its subfolders in front of its requests, so a folder never goes between requests and a request
// never goes above a folder: a drop next to a node of the other kind lands at the edge of its own kind.
public static class DropPlanner
{
    // False when the target is the node itself or lies inside it: a node cannot be dropped into its own subtree.
    public static bool CanDrop(TreeNodeViewModel source, TreeNodeViewModel target)
    {
        for (var node = target; node is not null; node = node.Parent)
        {
            if (ReferenceEquals(node, source))
            {
                return false;
            }
        }

        return true;
    }

    // Null when the drop would leave the node where it is.
    public static DropPlan? Plan(TreeNodeViewModel source, TreeNodeViewModel target, DropPosition position)
    {
        var parent = target.Parent;
        if (parent is null || (position == DropPosition.Inside && target.CanContainChildren))
        {
            return new DropPlan(target.DropFolderId, null, target.Id);
        }

        var siblings = parent.Children.Where(sibling => sibling.IsRequest == source.IsRequest).ToList();
        var anchor = FindAnchor(source, target, position, siblings);
        if (ReferenceEquals(anchor, source) || KeepsOrder(source, parent, anchor, siblings))
        {
            return null;
        }

        return new DropPlan(parent.DropFolderId, anchor?.Id, parent.Id);
    }

    // The sibling of the dragged kind that the node goes in front of; null puts it last.
    private static TreeNodeViewModel? FindAnchor(
        TreeNodeViewModel source,
        TreeNodeViewModel target,
        DropPosition position,
        List<TreeNodeViewModel> siblings)
    {
        if (target.IsRequest != source.IsRequest)
        {
            return FirstRequest(source, siblings);
        }

        if (position == DropPosition.Before)
        {
            return target;
        }

        var next = siblings.IndexOf(target) + 1;
        if (next < siblings.Count)
        {
            return siblings[next];
        }

        return null;
    }

    // A request dropped beside a folder goes in front of the requests; a folder beside a request goes behind the folders.
    private static TreeNodeViewModel? FirstRequest(TreeNodeViewModel source, List<TreeNodeViewModel> siblings)
    {
        if (source.IsRequest && siblings.Count > 0)
        {
            return siblings[0];
        }

        return null;
    }

    private static bool KeepsOrder(
        TreeNodeViewModel source,
        TreeNodeViewModel parent,
        TreeNodeViewModel? anchor,
        List<TreeNodeViewModel> siblings)
    {
        if (!ReferenceEquals(source.Parent, parent))
        {
            return false;
        }

        var others = siblings.Where(sibling => !ReferenceEquals(sibling, source)).ToList();
        var insertAt = anchor is null ? others.Count : others.IndexOf(anchor);
        others.Insert(insertAt, source);
        return others.SequenceEqual(siblings);
    }
}
