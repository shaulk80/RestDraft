using System.Buffers;
using System.Text;

namespace RestDraft.Application.Search;

// Finds a text in a longer one, as the find box of a browser does. The matches do not overlap.
public static class TextMatchFinder
{
    public static IReadOnlyList<TextMatch> Find(string text, string query, TextSearchOptions options)
    {
        var matches = new List<TextMatch>();
        if (query.Length == 0)
        {
            return matches;
        }

        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var start = text.IndexOf(query, comparison);
        while (start >= 0)
        {
            var match = new TextMatch(start, query.Length);
            var isAccepted = !options.WholeWord || IsWholeWord(text, match);
            if (isAccepted)
            {
                matches.Add(match);
            }

            // A place that is not a whole word may overlap one that is, so the search moves on by one character only.
            var next = isAccepted ? match.End : start + 1;
            start = text.IndexOf(query, next, comparison);
        }

        return matches;
    }

    // A whole word has no letter, digit or underscore running on past either end of the match. A match that begins or
    // ends with another character, such as a quote, has no word to run on at that end.
    private static bool IsWholeWord(string text, TextMatch match)
    {
        var runsOnBefore = IsWordCharacterBefore(text, match.Start) && IsWordCharacterAt(text, match.Start);
        var runsOnAfter = IsWordCharacterBefore(text, match.End) && IsWordCharacterAt(text, match.End);
        return !runsOnBefore && !runsOnAfter;
    }

    // Whole characters are read, so a letter that takes two UTF-16 units counts as a letter.
    private static bool IsWordCharacterAt(string text, int index)
    {
        var status = Rune.DecodeFromUtf16(text.AsSpan(index), out var character, out _);
        return status == OperationStatus.Done && IsWordCharacter(character);
    }

    private static bool IsWordCharacterBefore(string text, int index)
    {
        var status = Rune.DecodeLastFromUtf16(text.AsSpan(0, index), out var character, out _);
        return status == OperationStatus.Done && IsWordCharacter(character);
    }

    private static bool IsWordCharacter(Rune character)
    {
        return Rune.IsLetterOrDigit(character) || character.Value == '_';
    }
}
