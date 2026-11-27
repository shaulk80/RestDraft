using RestDraft.Domain.Results;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace RestDraft.Infrastructure.Exchange;

// Looks at the YAML events before a file is turned into objects, and refuses what a collection or a specification never
// needs and a hostile file could abuse: aliases (they can multiply a small file into a huge one) and extreme nesting.
internal static class YamlSafetyCheck
{
    // maxDepth counts nested mappings and sequences. tooDeepMessage is what the user reads when there are more.
    public static Result Validate(string text, int maxDepth, string tooDeepMessage)
    {
        var depth = 0;
        try
        {
            var parser = new Parser(new StringReader(text));
            while (parser.MoveNext())
            {
                if (parser.Current is AnchorAlias)
                {
                    return Failure("The file uses YAML aliases, which are not supported.");
                }

                depth += DepthChange(parser.Current);
                if (depth > maxDepth)
                {
                    return Failure(tooDeepMessage);
                }
            }
        }
        catch (YamlException exception)
        {
            return Failure($"The file is not valid YAML: {exception.Message}");
        }

        return Result.Success();
    }

    public static int DepthChange(ParsingEvent? current)
    {
        return current switch
        {
            MappingStart or SequenceStart => 1,
            MappingEnd or SequenceEnd => -1,
            _ => 0,
        };
    }

    private static Result Failure(string message)
    {
        return Result.Failure(ResultErrors.Validation(message));
    }
}
