using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// The values that a specification holds (examples and defaults), and how they are written into a request.
internal static class OpenApiValues
{
    // Swagger 2.0 has no example for a parameter or a body, and this extension is the usual way to give one.
    private const string ExampleExtensionName = "x-example";

    private static readonly JsonSerializerOptions Compact = Options(indented: false);

    private static readonly JsonSerializerOptions Indented = Options(indented: true);

    // The reader hands over a JSON null of the document as a special node. It counts as no value here.
    public static JsonNode? Normalize(JsonNode? node)
    {
        return node is null || JsonNullSentinel.IsJsonNullSentinel(node) ? null : node;
    }

    public static string ToText(JsonNode? node)
    {
        var value = Normalize(node);
        if (value is null)
        {
            return string.Empty;
        }

        return value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString(Compact);
    }

    public static string ToIndentedJson(JsonNode? node)
    {
        return Normalize(node)?.ToJsonString(Indented) ?? "null";
    }

    // The value of a schema itself: example, constant or default. The caller has made sure that a reference leads somewhere.
    public static JsonNode? SchemaValue(IOpenApiSchema schema)
    {
        var constant = schema.Const is null ? null : JsonValue.Create(schema.Const);
        return Normalize(SingleExample(schema))
            ?? Normalize(schema.Examples?.FirstOrDefault())
            ?? constant
            ?? Normalize(schema.Default);
    }

    // OpenAPI 2.0 and 3.0 write "example". The library marks it as obsolete, but it is where their value is.
    public static JsonNode? SingleExample(IOpenApiSchema schema)
    {
#pragma warning disable CS0618
        return schema.Example;
#pragma warning restore CS0618
    }

    public static JsonNode? MediaExample(IOpenApiMediaType media, OpenApiReferences references)
    {
        return Normalize(media.Example) ?? FirstExample(media.Examples, references);
    }

    // The first of the named examples that holds its value here and not at an address.
    public static JsonNode? FirstExample(IDictionary<string, IOpenApiExample>? examples, OpenApiReferences references)
    {
        return (examples?.Values ?? [])
            .Select(references.Follow)
            .OfType<IOpenApiExample>()
            .Select(ExampleValue)
            .FirstOrDefault(value => value is not null);
    }

    public static JsonNode? ExampleExtension(IDictionary<string, IOpenApiExtension>? extensions)
    {
        if (extensions is null || !extensions.TryGetValue(ExampleExtensionName, out var extension))
        {
            return null;
        }

        return Normalize((extension as JsonNodeExtension)?.Node);
    }

    private static JsonNode? ExampleValue(IOpenApiExample example)
    {
        var text = example.SerializedValue is null ? null : JsonValue.Create(example.SerializedValue);
        return Normalize(example.Value) ?? Normalize(example.DataValue) ?? text;
    }

    private static JsonSerializerOptions Options(bool indented)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = indented,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // A made sample can nest an example, so it may be deeper than the file it came from.
            MaxDepth = OpenApiFormat.MaxDepth * 2,
        };
    }
}
