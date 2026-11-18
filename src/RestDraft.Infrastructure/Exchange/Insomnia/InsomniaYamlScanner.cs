using RestDraft.Application.Exchange;
using RestDraft.Domain.Results;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Looks at the YAML events before the file is turned into objects. It finds the type of the file, and it refuses what a
// collection never needs and a hostile file could abuse: aliases (they can multiply a small file into a huge one) and
// extreme nesting.
internal static class InsomniaYamlScanner
{
    // Every folder is two levels (the item and its children), and a request needs about four more below the list.
    private const int MaxDepth = (ExchangeLimits.MaxFolderDepth * 2) + 8;
    private const string TypeKey = "type";
    private const int RootMappingDepth = 1;

    // The value of the top-level key "type", or null when there is none or the text is not YAML.
    public static string? FindType(string text)
    {
        try
        {
            return ReadType(text);
        }
        catch (YamlException)
        {
            return null;
        }
    }

    public static Result Validate(string text)
    {
        var depth = 0;
        try
        {
            var parser = new Parser(new StringReader(text));
            while (parser.MoveNext())
            {
                if (parser.Current is not { } current)
                {
                    continue;
                }

                var failure = Check(current, ref depth);
                if (failure is not null)
                {
                    return Result.Failure(ResultErrors.Validation(failure));
                }
            }
        }
        catch (YamlException exception)
        {
            return Result.Failure(ResultErrors.Validation($"The file is not valid YAML: {exception.Message}"));
        }

        return Result.Success();
    }

    private static string? ReadType(string text)
    {
        var parser = new Parser(new StringReader(text));
        var depth = 0;
        var isKey = true;
        var isTypeKey = false;
        while (parser.MoveNext())
        {
            if (parser.Current is not { } current)
            {
                continue;
            }

            if (depth == RootMappingDepth && current is Scalar scalar)
            {
                if (isTypeKey)
                {
                    return scalar.Value;
                }

                isTypeKey = isKey && scalar.Value == TypeKey;
                isKey = !isKey;
                continue;
            }

            depth += DepthChange(current);
            if (depth == RootMappingDepth && current is MappingEnd or SequenceEnd)
            {
                isKey = true;
                isTypeKey = false;
            }

            if (depth == 0 && current is DocumentEnd)
            {
                return null;
            }
        }

        return null;
    }

    private static string? Check(ParsingEvent current, ref int depth)
    {
        if (current is AnchorAlias)
        {
            return "The file uses YAML aliases, which are not supported.";
        }

        depth += DepthChange(current);
        return depth > MaxDepth
            ? $"The file is nested deeper than {ExchangeLimits.MaxFolderDepth} folders."
            : null;
    }

    private static int DepthChange(ParsingEvent current)
    {
        return current switch
        {
            MappingStart or SequenceStart => 1,
            MappingEnd or SequenceEnd => -1,
            _ => 0,
        };
    }
}
