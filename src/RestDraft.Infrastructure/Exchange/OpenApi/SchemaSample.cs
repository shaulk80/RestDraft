using System.Text.Json.Nodes;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// A sample value. A null value is the JSON null, which is different from having no sample.
internal sealed record SchemaSample(JsonNode? Value);
