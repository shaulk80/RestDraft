namespace RestDraft.Application.Search;

// A place in a text where a search text was found.
public readonly record struct TextMatch(int Start, int Length)
{
    public int End => Start + Length;
}
