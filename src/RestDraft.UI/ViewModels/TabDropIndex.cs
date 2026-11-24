namespace RestDraft.UI.ViewModels;

// Where a dragged tab ends up when it is dropped on the left or right half of another tab.
public static class TabDropIndex
{
    // The dragged tab leaves its place first, so every tab behind it moves one step down before the gap is counted.
    public static int Of(int sourceIndex, int targetIndex, bool afterTarget)
    {
        var gap = afterTarget ? targetIndex + 1 : targetIndex;
        return gap > sourceIndex ? gap - 1 : gap;
    }
}
