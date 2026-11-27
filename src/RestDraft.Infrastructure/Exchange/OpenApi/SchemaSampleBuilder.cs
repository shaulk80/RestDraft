using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Makes a sample value from a schema, for a body that has no example. One builder makes one sample.
internal sealed class SchemaSampleBuilder
{
    private const int MaxDepth = 8;

    // Schemas that share their parts can make a sample grow without end although none refers to itself.
    private const int MaxSchemas = 1000;

    private readonly OpenApiReferences _references;
    private readonly HashSet<IOpenApiSchema> _open = new(ReferenceEqualityComparer.Instance);
    private int _visited;

    public SchemaSampleBuilder(OpenApiReferences references)
    {
        _references = references;
    }

    // Null when there is no sample: no schema, a broken reference, recursion, or a sample that is deep or large enough.
    public SchemaSample? Build(IOpenApiSchema? schema)
    {
        return Build(schema, 0);
    }

    private SchemaSample? Build(IOpenApiSchema? schema, int depth)
    {
        var target = _references.Follow(schema);
        if (target is null || depth > MaxDepth || _visited >= MaxSchemas || !_open.Add(target))
        {
            return null;
        }

        _visited++;
        // The reference is asked first: in OpenAPI 3.1 it can give a value of its own.
        var sample = Given(schema!) ?? FromShape(target, depth);
        _open.Remove(target);
        return sample;
    }

    // What the specification says the value is or may be.
    private static SchemaSample? Given(IOpenApiSchema schema)
    {
        var value = OpenApiValues.SingleExample(schema)
            ?? schema.Examples?.FirstOrDefault()
            ?? (schema.Const is null ? null : JsonValue.Create(schema.Const))
            ?? schema.Default
            ?? schema.Enum?.FirstOrDefault();
        return value is null ? null : new SchemaSample(OpenApiValues.Normalize(value)?.DeepClone());
    }

    private SchemaSample? FromShape(IOpenApiSchema schema, int depth)
    {
        var parts = PartsOf(schema);
        if (parts.Count > 0)
        {
            return Combine(schema, parts, depth);
        }

        var type = schema.Type.GetValueOrDefault() & ~JsonSchemaType.Null;
        if (type.HasFlag(JsonSchemaType.Object) || schema.Properties is { Count: > 0 })
        {
            return new SchemaSample(ObjectOf(schema, depth));
        }

        if (type.HasFlag(JsonSchemaType.Array) || schema.Items is not null)
        {
            return new SchemaSample(ListOf(schema, depth));
        }

        return new SchemaSample(SimpleValue(type, schema.Format));
    }

    // All of allOf, and the first choice of oneOf or anyOf that is more than null.
    private List<IOpenApiSchema> PartsOf(IOpenApiSchema schema)
    {
        var choices = schema.OneOf is { Count: > 0 } ? schema.OneOf : schema.AnyOf;
        var choice = choices?.FirstOrDefault(candidate => _references.Follow(candidate)?.Type != JsonSchemaType.Null);
        var parts = new List<IOpenApiSchema>(schema.AllOf ?? []);
        if (choice is not null)
        {
            parts.Add(choice);
        }

        return parts;
    }

    // The objects of the schema and of its parts become one object. Without an object the first sample counts.
    private SchemaSample? Combine(IOpenApiSchema schema, List<IOpenApiSchema> parts, int depth)
    {
        var samples = new List<SchemaSample>();
        if (schema.Properties is { Count: > 0 })
        {
            samples.Add(new SchemaSample(ObjectOf(schema, depth)));
        }

        samples.AddRange(parts.Select(part => Build(part, depth)).OfType<SchemaSample>());
        var objects = samples.Select(sample => sample.Value).OfType<JsonObject>().ToList();
        if (objects.Count == 0)
        {
            return samples.FirstOrDefault();
        }

        var combined = new JsonObject();
        foreach (var property in objects.SelectMany(part => part))
        {
            combined[property.Key] = property.Value?.DeepClone();
        }

        return new SchemaSample(combined);
    }

    // A read-only property is only in responses, so a request leaves it out.
    private JsonObject ObjectOf(IOpenApiSchema schema, int depth)
    {
        var result = new JsonObject();
        foreach (var property in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (_references.Follow(property.Value) is null || property.Value.ReadOnly)
            {
                continue;
            }

            if (Build(property.Value, depth + 1) is { } sample)
            {
                result[property.Key] = sample.Value;
            }
        }

        return result;
    }

    private JsonArray ListOf(IOpenApiSchema schema, int depth)
    {
        var item = Build(schema.Items, depth + 1);
        return item is null ? [] : new JsonArray(item.Value);
    }

    private static JsonValue? SimpleValue(JsonSchemaType type, string? format)
    {
        if (type.HasFlag(JsonSchemaType.String))
        {
            return JsonValue.Create(TextOfFormat(format));
        }

        if (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number))
        {
            return JsonValue.Create(0);
        }

        return type.HasFlag(JsonSchemaType.Boolean) ? JsonValue.Create(false) : null;
    }

    private static string TextOfFormat(string? format)
    {
        return format switch
        {
            "date-time" => "2026-01-01T00:00:00Z",
            "date" => "2026-01-01",
            "time" => "00:00:00",
            "uuid" => "00000000-0000-0000-0000-000000000000",
            "email" => "user@example.com",
            "uri" or "url" => "https://example.com",
            "hostname" => "example.com",
            "ipv4" => "192.0.2.1",
            "ipv6" => "2001:db8::1",
            "byte" or "binary" => string.Empty,
            _ => "string",
        };
    }
}
