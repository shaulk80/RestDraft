using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Follows the references inside a specification. The library throws on a reference that leads back to itself, so a
// reference is followed one step at a time, and nothing of it is read before it is known to lead somewhere.
internal sealed class OpenApiReferences
{
    private readonly ImportWarningList _warnings;

    public OpenApiReferences(ImportWarningList warnings)
    {
        _warnings = warnings;
    }

    // The item itself, or what it refers to. Null when there is nothing to use: no item, or a reference that leads to
    // another file, to nothing or back to itself. The reason goes to the warnings.
    public T? Follow<T>(T? item)
        where T : class
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = item;
        while (current is not null && StepOf(current) is { } step)
        {
            var name = step.Reference.Id ?? string.Empty;
            if (step.Reference.IsExternal)
            {
                _warnings.AddOnce("References to other files are not followed.");
                return null;
            }

            if (!visited.Add(name))
            {
                _warnings.AddOnce($"The reference '{name}' leads back to itself and is left out.");
                return null;
            }

            current = step.Target as T;
            if (current is null)
            {
                _warnings.AddOnce($"The reference '{name}' leads to nothing in the specification.");
            }
        }

        return current;
    }

    // Null for an item that is not a reference.
    private static ReferenceStep? StepOf(object item)
    {
        return item switch
        {
            OpenApiSchemaReference schema => new ReferenceStep(schema.Reference, schema.Target),
            OpenApiParameterReference parameter => new ReferenceStep(parameter.Reference, parameter.Target),
            OpenApiRequestBodyReference body => new ReferenceStep(body.Reference, body.Target),
            OpenApiPathItemReference path => new ReferenceStep(path.Reference, path.Target),
            OpenApiMediaTypeReference media => new ReferenceStep(media.Reference, media.Target),
            OpenApiExampleReference example => new ReferenceStep(example.Reference, example.Target),
            OpenApiSecuritySchemeReference scheme => new ReferenceStep(scheme.Reference, scheme.Target),
            _ => null,
        };
    }
}
