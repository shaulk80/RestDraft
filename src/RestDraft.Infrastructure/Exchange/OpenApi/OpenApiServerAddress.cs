using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// The first server address of a specification, with each {variable} of the address replaced by its default value. A
// variable without one becomes a RestDraft variable.
internal static partial class OpenApiServerAddress
{
    // Empty when the specification declares no server.
    public static string Read(IList<OpenApiServer>? servers)
    {
        var server = servers?.FirstOrDefault(candidate => candidate is not null);
        if (server is null)
        {
            return string.Empty;
        }

        return ServerVariable().Replace(server.Url ?? string.Empty, match => ValueOf(server, match.Groups[1].Value));
    }

    private static string ValueOf(OpenApiServer server, string name)
    {
        if (server.Variables is { } variables && variables.TryGetValue(name, out var variable) && !string.IsNullOrEmpty(variable.Default))
        {
            return variable.Default;
        }

        return "{{" + name + "}}";
    }

    // Single braces only: a {{variable}} that is already written stays as it is.
    [GeneratedRegex(@"(?<!\{)\{([^{}]+)\}(?!\})")]
    private static partial Regex ServerVariable();
}
