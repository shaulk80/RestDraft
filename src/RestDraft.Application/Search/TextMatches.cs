namespace RestDraft.Application.Search;

// Lookups in a list of matches that is in text order and has no overlaps, as TextMatchFinder returns it.
public static class TextMatches
{
    // The index of the first match that starts at or after the offset, or the number of matches when none does.
    public static int FirstIndexFrom(IReadOnlyList<TextMatch> matches, int offset)
    {
        return FirstIndex(matches, match => match.Start >= offset);
    }

    // The matches that reach into the part of the text between the two offsets.
    public static IEnumerable<TextMatch> Overlapping(IReadOnlyList<TextMatch> matches, int start, int end)
    {
        for (var index = FirstIndex(matches, match => match.End > start); index < matches.Count; index++)
        {
            var match = matches[index];
            if (match.Start >= end)
            {
                yield break;
            }

            yield return match;
        }
    }

    // A binary search: every match before the one sought fails the condition, and every one from it on passes.
    private static int FirstIndex(IReadOnlyList<TextMatch> matches, Func<TextMatch, bool> isReached)
    {
        var low = 0;
        var high = matches.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (isReached(matches[middle]))
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return low;
    }
}
