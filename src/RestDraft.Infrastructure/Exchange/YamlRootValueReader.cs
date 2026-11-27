using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace RestDraft.Infrastructure.Exchange;

// Finds a key of the top-level mapping without reading the whole file into objects, to tell what kind of file it is.
internal static class YamlRootValueReader
{
    private const int RootMappingDepth = 1;

    // The value of the first of the top-level keys that has a plain text value. Null when there is none, or when the text
    // stops being YAML before one is found.
    public static string? Find(string text, params IReadOnlyCollection<string> keys)
    {
        try
        {
            return Read(new Parser(new StringReader(text)), keys);
        }
        catch (YamlException)
        {
            // Not being YAML only means that this is not a file of the asking format.
            return null;
        }
    }

    private static string? Read(Parser parser, IReadOnlyCollection<string> keys)
    {
        var depth = 0;
        var isKey = true;
        var isWantedKey = false;
        while (parser.MoveNext())
        {
            if (parser.Current is not { } current)
            {
                continue;
            }

            if (depth == RootMappingDepth && current is Scalar scalar)
            {
                if (isWantedKey)
                {
                    return scalar.Value;
                }

                isWantedKey = isKey && keys.Contains(scalar.Value);
                isKey = !isKey;
                continue;
            }

            depth += YamlSafetyCheck.DepthChange(current);
            if (depth == RootMappingDepth && current is MappingEnd or SequenceEnd)
            {
                isKey = true;
                isWantedKey = false;
            }

            if (depth == 0 && current is DocumentEnd)
            {
                return null;
            }
        }

        return null;
    }
}
