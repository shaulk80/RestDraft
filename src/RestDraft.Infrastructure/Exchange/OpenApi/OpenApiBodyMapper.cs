using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Turns a request body into a RestDraft body: the example of the specification, or else a sample made from the schema.
// Of several media types the first in the order of OpenApiBodyKind is taken, so JSON goes before the others.
internal sealed class OpenApiBodyMapper
{
    private const string JsonMediaType = "application/json";

    private readonly OpenApiReferences _references;
    private readonly ImportWarningList _warnings;

    public OpenApiBodyMapper(OpenApiReferences references, ImportWarningList warnings)
    {
        _references = references;
        _warnings = warnings;
    }

    // label names the operation in warnings.
    public BodyDefinition Map(IOpenApiRequestBody? requestBody, string label)
    {
        var body = _references.Follow(requestBody);
        if (body?.Content is not { Count: > 0 } content)
        {
            return new NoBody();
        }

        var chosen = content.OrderBy(entry => KindOf(entry.Key)).First();
        var media = _references.Follow(chosen.Value);
        if (media is null)
        {
            return new NoBody();
        }

        var example = OpenApiValues.MediaExample(media, _references) ?? OpenApiValues.ExampleExtension(body.Extensions);
        return KindOf(chosen.Key) switch
        {
            OpenApiBodyKind.Json => JsonBody(chosen.Key, media.Schema, example),
            OpenApiBodyKind.FormUrlEncoded => FormBody(media.Schema, example),
            OpenApiBodyKind.Multipart => Multipart(media.Schema, example),
            OpenApiBodyKind.Text => TextBody(chosen.Key, media.Schema, example, label),
            _ => Unsupported(chosen.Key, label),
        };
    }

    private static OpenApiBodyKind KindOf(string mediaType)
    {
        var type = mediaType.Split(';')[0].Trim().ToLowerInvariant();
        if (type is JsonMediaType or "text/json" or "*/*" or "application/*" || type.EndsWith("+json", StringComparison.Ordinal))
        {
            return OpenApiBodyKind.Json;
        }

        if (type == "application/x-www-form-urlencoded")
        {
            return OpenApiBodyKind.FormUrlEncoded;
        }

        if (type == "multipart/form-data")
        {
            return OpenApiBodyKind.Multipart;
        }

        var isText = type.StartsWith("text/", StringComparison.Ordinal)
            || type.EndsWith("xml", StringComparison.Ordinal)
            || type.EndsWith("yaml", StringComparison.Ordinal)
            || type is "application/graphql" or "application/javascript";
        return isText ? OpenApiBodyKind.Text : OpenApiBodyKind.Unsupported;
    }

    private SchemaSample? SampleOf(IOpenApiSchema? schema, JsonNode? example)
    {
        return example is null
            ? new SchemaSampleBuilder(_references).Build(schema)
            : new SchemaSample(example);
    }

    // The fields of a form: the example when it is an object, else the sample of the schema.
    private JsonObject FieldsOf(IOpenApiSchema? schema, JsonNode? example)
    {
        return example as JsonObject
            ?? new SchemaSampleBuilder(_references).Build(schema)?.Value as JsonObject
            ?? [];
    }

    private RawBody JsonBody(string mediaType, IOpenApiSchema? schema, JsonNode? example)
    {
        var contentType = mediaType.Contains('*') ? JsonMediaType : mediaType;
        var sample = SampleOf(schema, example);
        if (sample is null)
        {
            return new RawBody(contentType, string.Empty);
        }

        // Some specifications write a JSON example as one text.
        var text = IsJsonWrittenAsText(sample.Value, schema)
            ? sample.Value!.GetValue<string>()
            : OpenApiValues.ToIndentedJson(sample.Value);
        return new RawBody(contentType, text);
    }

    private bool IsJsonWrittenAsText(JsonNode? value, IOpenApiSchema? schema)
    {
        if (value?.GetValueKind() != JsonValueKind.String)
        {
            return false;
        }

        var type = _references.Follow(schema)?.Type;
        if (type?.HasFlag(JsonSchemaType.String) == true)
        {
            return false;
        }

        var text = value.GetValue<string>().Trim();
        return (text.StartsWith('{') || text.StartsWith('[')) && IsValidJson(text);
    }

    private static bool IsValidJson(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            // Text that only looks like JSON stays a text value.
            return false;
        }
    }

    // A form without fields is no body. Some specifications declare one for every operation, also for GET.
    private BodyDefinition FormBody(IOpenApiSchema? schema, JsonNode? example)
    {
        var fields = FieldsOf(schema, example)
            .SelectMany(field => TextsOf(field.Value).Select(text => new KeyValueEntry(field.Key, text, true)))
            .ToList();
        return fields.Count > 0 ? new FormUrlEncodedBody(fields) : new NoBody();
    }

    // A file part has no path yet: the user picks the file before sending.
    private BodyDefinition Multipart(IOpenApiSchema? schema, JsonNode? example)
    {
        var fields = FieldsOf(schema, example);
        var fileNames = FileProperties(schema, new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance)).Distinct().ToList();
        var parts = fields
            .SelectMany(field => fileNames.Contains(field.Key)
                ? [FilePart(field.Key)]
                : TextsOf(field.Value).Select(text => new MultipartPart(field.Key, text, null)))
            .ToList();
        parts.AddRange(fileNames.Where(name => !fields.ContainsKey(name)).Select(FilePart));
        return parts.Count > 0 ? new MultipartBody(parts) : new NoBody();
    }

    private static MultipartPart FilePart(string name)
    {
        return new MultipartPart(name, null, string.Empty);
    }

    // The names of the properties that hold a file, in the schema and in the schemas it is combined from.
    private List<string> FileProperties(IOpenApiSchema? schema, HashSet<IOpenApiSchema> visited)
    {
        var target = _references.Follow(schema);
        if (target is null || !visited.Add(target))
        {
            return [];
        }

        var own = (target.Properties ?? new Dictionary<string, IOpenApiSchema>())
            .Where(property => IsFile(property.Value))
            .Select(property => property.Key);
        var combined = (target.AllOf ?? []).SelectMany(part => FileProperties(part, visited));
        return own.Concat(combined).ToList();
    }

    private bool IsFile(IOpenApiSchema schema)
    {
        var target = _references.Follow(schema);
        var item = target?.Items is null ? target : _references.Follow(target.Items);
        if (item is null)
        {
            return false;
        }

        if (item.Format == "binary")
        {
            return true;
        }

        // OpenAPI 3.1: content of a media type that is not encoded as text.
        return item is OpenApiSchema { ContentMediaType.Length: > 0 } content && string.IsNullOrEmpty(content.ContentEncoding);
    }

    // A list gives one field per item. An empty list still shows the field.
    private static List<string> TextsOf(JsonNode? value)
    {
        return value is JsonArray { Count: > 0 } list
            ? list.Select(OpenApiValues.ToText).ToList()
            : [value is JsonArray ? string.Empty : OpenApiValues.ToText(value)];
    }

    private RawBody TextBody(string mediaType, IOpenApiSchema? schema, JsonNode? example, string label)
    {
        var value = SampleOf(schema, example)?.Value;
        if (value?.GetValueKind() == JsonValueKind.String)
        {
            return new RawBody(mediaType, value.GetValue<string>());
        }

        _warnings.Add($"No sample can be made for the body type '{mediaType}', so the body is empty.", label);
        return new RawBody(mediaType, string.Empty);
    }

    private NoBody Unsupported(string mediaType, string label)
    {
        _warnings.Add($"The body type '{mediaType}' is not supported. The request has no body.", label);
        return new NoBody();
    }
}
