namespace RestDraft.UI.ViewModels;

// Which part of a row the pointer is over decides what a drop means. A request is split in halves (before, after).
// A folder has an edge of a quarter at the top (before) and at the bottom (after), and the middle is inside.
// A collection is always inside, and so is the bottom edge of an open folder with children, because the row below
// it is its first child, not its next sibling.
public static class DropZones
{
    private const double EdgeFraction = 0.25;

    public static DropPosition Resolve(TreeNodeViewModel target, double y, double height)
    {
        if (target.IsCollection || height <= 0)
        {
            return DropPosition.Inside;
        }

        var fraction = y / height;
        if (target.IsRequest)
        {
            return fraction < 0.5 ? DropPosition.Before : DropPosition.After;
        }

        if (fraction < EdgeFraction)
        {
            return DropPosition.Before;
        }

        var showsChildrenBelow = target.IsExpanded && target.Children.Count > 0;
        if (fraction > 1 - EdgeFraction && !showsChildrenBelow)
        {
            return DropPosition.After;
        }

        return DropPosition.Inside;
    }
}
