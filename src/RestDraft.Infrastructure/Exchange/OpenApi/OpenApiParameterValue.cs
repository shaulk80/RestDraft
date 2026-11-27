using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// The example or the default value that a specification gives for a parameter, as the texts to send.
internal sealed class OpenApiParameterValue
{
    private readonly OpenApiReferences _references;

    public OpenApiParameterValue(OpenApiReferences references)
    {
        _references = references;
    }

    // Empty when the specification gives no value. A list gives one text per item.
    public IReadOnlyList<string> Read(IOpenApiParameter parameter)
    {
        var value = OpenApiValues.Normalize(parameter.Example)
            ?? OpenApiValues.FirstExample(parameter.Examples, _references)
            ?? FromContent(parameter)
            ?? OpenApiValues.ExampleExtension(parameter.Extensions)
            ?? FromSchema(parameter.Schema);
        return value switch
        {
            null => [],
            JsonArray list => list.Select(OpenApiValues.ToText).ToList(),
            _ => [OpenApiValues.ToText(value)],
        };
    }

    // A parameter can describe its value with a media type in place of a schema.
    private JsonNode? FromContent(IOpenApiParameter parameter)
    {
        var media = _references.Follow(parameter.Content?.Values.FirstOrDefault());
        if (media is null)
        {
            return null;
        }

        return OpenApiValues.MediaExample(media, _references) ?? FromSchema(media.Schema);
    }

    // The value of the schema, or else of the items of a list. Only one level down: a list can hold itself.
    private JsonNode? FromSchema(IOpenApiSchema? schema)
    {
        var target = _references.Follow(schema);
        if (target is null)
        {
            return null;
        }

        var own = OpenApiValues.SchemaValue(schema!);
        if (own is not null || target.Items is null || _references.Follow(target.Items) is null)
        {
            return own;
        }

        return OpenApiValues.SchemaValue(target.Items);
    }
}
