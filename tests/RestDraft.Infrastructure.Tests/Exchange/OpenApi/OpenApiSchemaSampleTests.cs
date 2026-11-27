using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;
using RestDraft.Domain.Bodies;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

// The sample that is made for a JSON body from its schema.
[TestFixture]
public class OpenApiSchemaSampleTests
{
    // The sample on one line, so that a test case can say it.
    private static string Sample(string schema, string schemas = "{}")
    {
        var document = Read(Operation(
            "{\"requestBody\":{\"content\":{\"application/json\":{\"schema\":" + schema + "}}}}",
            "\"components\":{\"schemas\":" + schemas + "}"));
        var text = ((RawBody)SingleRequest(document).Body).Text;
        return text.Length == 0 ? text : JsonNode.Parse(text)?.ToJsonString() ?? "null";
    }

    [TestCase("""{"type":"string"}""", "\"string\"")]
    [TestCase("""{"type":"string","format":"date-time"}""", "\"2026-01-01T00:00:00Z\"")]
    [TestCase("""{"type":"string","format":"uuid"}""", "\"00000000-0000-0000-0000-000000000000\"")]
    [TestCase("""{"type":"string","format":"email"}""", "\"user@example.com\"")]
    [TestCase("""{"type":"string","format":"uri"}""", "\"https://example.com\"")]
    [TestCase("""{"type":"string","format":"unheard-of"}""", "\"string\"")]
    [TestCase("""{"type":"integer"}""", "0")]
    [TestCase("""{"type":"number"}""", "0")]
    [TestCase("""{"type":"boolean"}""", "false")]
    [TestCase("""{"type":"object"}""", "{}")]
    [TestCase("""{"type":"array","items":{"type":"integer"}}""", "[0]")]
    [TestCase("""{"type":"array"}""", "[]")]
    [TestCase("""{}""", "null")]
    [TestCase("""{"type":"string","enum":["red","green"]}""", "\"red\"")]
    [TestCase("""{"type":"string","default":"d","enum":["a","d"]}""", "\"d\"")]
    [TestCase("""{"type":"string","example":"e","default":"d"}""", "\"e\"")]
    [TestCase("""{"type":"object","example":{"x":1},"properties":{"y":{"type":"string"}}}""", "{\"x\":1}")]
    [TestCase("""{"type":"string","example":null}""", "null")]
    [TestCase("""{"properties":{"a":{"type":"string"},"b":{"type":"string","readOnly":true}}}""", "{\"a\":\"string\"}")]
    [TestCase("""{"oneOf":[{"type":"integer","minimum":1},{"type":"object"}]}""", "0")]
    [TestCase("""{"anyOf":[{"type":"boolean","title":"b"},{"type":"object"}]}""", "false")]
    // The reader joins bare types of OpenAPI 3.0 into one list of types, and a text is taken where it is allowed.
    [TestCase("""{"oneOf":[{"type":"integer"},{"type":"string"}]}""", "\"string\"")]
    [TestCase(
        """{"allOf":[{"properties":{"a":{"type":"integer"}}},{"properties":{"b":{"type":"boolean"}}}],"properties":{"c":{"type":"string"}}}""",
        "{\"c\":\"string\",\"a\":0,\"b\":false}")]
    [TestCase("""{"allOf":[{"type":"string","enum":["only"]}]}""", "\"only\"")]
    [TestCase("""{"properties":{"kind":{"type":"string"}},"oneOf":[{"required":["kind"]}]}""", "{\"kind\":\"string\"}")]
    public void AValueIsMadeFromTheSchema(string schema, string expected)
    {
        Assert.That(Sample(schema), Is.EqualTo(expected));
    }

    // A schema that leads back to itself ends where it would start again.
    [TestCase(
        """{"Node":{"type":"object","properties":{"name":{"type":"string"},"parent":{"$ref":"#/components/schemas/Node"},"children":{"type":"array","items":{"$ref":"#/components/schemas/Node"}}}}}""",
        "{\"name\":\"string\",\"children\":[]}")]
    [TestCase(
        """{"Node":{"allOf":[{"$ref":"#/components/schemas/Node"}],"properties":{"a":{"type":"integer"}}}}""",
        "{\"a\":0}")]
    [TestCase(
        """{"Node":{"properties":{"other":{"$ref":"#/components/schemas/Other"}}},"Other":{"properties":{"back":{"$ref":"#/components/schemas/Node"}}}}""",
        "{\"other\":{}}")]
    [TestCase(
        """{"Node":{"properties":{"loop":{"$ref":"#/components/schemas/A"},"ok":{"type":"integer"}}},"A":{"$ref":"#/components/schemas/B"},"B":{"$ref":"#/components/schemas/A"}}""",
        "{\"ok\":0}")]
    public void RecursionStops(string schemas, string expected)
    {
        Assert.That(Sample("""{"$ref":"#/components/schemas/Node"}""", schemas), Is.EqualTo(expected));
    }

    [Test]
    public void ADeepSchemaIsCut()
    {
        var schema = new StringBuilder();
        const int Levels = 30;
        for (var level = 0; level < Levels; level++)
        {
            schema.Append("{\"properties\":{\"a\":");
        }

        schema.Append("{\"type\":\"string\"}").Append('}', Levels * 2);

        var sample = Sample(schema.ToString());

        Assert.That(sample.Split("\"a\"").Length - 1, Is.EqualTo(8));
    }

    // Twelve schemas with ten properties each, every one referring to the next, would make 10^12 values.
    [Test]
    public void ASampleThatWouldGrowWithoutEndIsCut()
    {
        var schemas = Enumerable.Range(0, 12).Select(level =>
        {
            var properties = Enumerable.Range(0, 10)
                .Select(property => $"\"p{property}\":{{\"$ref\":\"#/components/schemas/S{level + 1}\"}}");
            return $"\"S{level}\":{{\"properties\":{{{string.Join(',', properties)}}}}}";
        }).Append("\"S12\":{\"type\":\"string\"}");

        var sample = Sample("""{"$ref":"#/components/schemas/S0"}""", "{" + string.Join(',', schemas) + "}");

        Assert.That(sample, Has.Length.LessThan(20_000));
    }

    [Test]
    public void AValueNextToAReferenceIsUsedInOpenApi31()
    {
        var specification = Operation(
            """{"requestBody":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Name","examples":["here"]}}}}}""",
            """ "components": { "schemas": { "Name": { "type": "string" } } } """)
            .Replace("3.0.3", "3.1.0", StringComparison.Ordinal);

        var body = (RawBody)SingleRequest(Read(specification)).Body;

        Assert.That(body.Text, Is.EqualTo("\"here\""));
    }
}
