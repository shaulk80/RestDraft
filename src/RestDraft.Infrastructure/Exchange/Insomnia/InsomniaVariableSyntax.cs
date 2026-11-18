using System.Buffers;
using System.Text.RegularExpressions;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Insomnia writes a variable as {{ _.name }}, RestDraft as {{name}}. Tags and other expressions have no equivalent.
internal static partial class InsomniaVariableSyntax
{
    private static readonly SearchValues<char> UnconvertibleCharacters = SearchValues.Create("|()'\"%");

    public static string ToRestDraft(string text)
    {
        return InsomniaVariable().Replace(text, "{{$1}}");
    }

    public static string ToInsomnia(string text)
    {
        return AnyBraces().Replace(text, match =>
        {
            var name = match.Groups[1].Value.Trim();
            return name.Length == 0 ? match.Value : "{{ _." + name + " }}";
        });
    }

    // True for text (already converted by ToRestDraft) that still holds something that is not a plain variable.
    public static bool HasUnconvertible(string converted)
    {
        if (converted.Contains("{%", StringComparison.Ordinal))
        {
            return true;
        }

        return AnyBraces().Matches(converted).Any(match => IsUnconvertible(match.Groups[1].Value));
    }

    private static bool IsUnconvertible(string inner)
    {
        return inner != inner.Trim()
            || inner.StartsWith("_.", StringComparison.Ordinal)
            || inner.AsSpan().IndexOfAny(UnconvertibleCharacters) >= 0;
    }

    [GeneratedRegex(@"\{\{\s*_\.([^\s{}.|()'""%]+)\s*\}\}")]
    private static partial Regex InsomniaVariable();

    [GeneratedRegex(@"\{\{([^{}]*)\}\}")]
    private static partial Regex AnyBraces();
}
