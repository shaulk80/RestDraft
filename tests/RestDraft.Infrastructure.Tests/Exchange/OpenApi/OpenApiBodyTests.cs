using NUnit.Framework;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

[TestFixture]
public class OpenApiBodyTests
{
    private static ExchangeBody ReadBody(string content, string more = "")
    {
        var document = Read(Operation("{\"requestBody\":{\"content\":" + content + "}}", more));
        return new ExchangeBody(SingleRequest(document).Body, Messages(document));
    }

    [TestCase("""{"application/xml":{"example":"<a/>"},"application/json":{"example":1}}""", "application/json", "1")]
    [TestCase("""{"text/plain":{"example":"t"},"application/problem+json":{"example":2}}""", "application/problem+json", "2")]
    [TestCase("""{"application/json; charset=utf-8":{"example":3}}""", "application/json; charset=utf-8", "3")]
    [TestCase("""{"*/*":{"example":4}}""", "application/json", "4")]
    [TestCase("""{"application/json":{"example":"{\"a\": 1}"}}""", "application/json", "{\"a\": 1}")]
    [TestCase("""{"application/json":{"example":"{\"a\": 1}","schema":{"type":"string"}}}""", "application/json", "\"{\\\"a\\\": 1}\"")]
    [TestCase("""{"application/json":{"example":"{not json"}}""", "application/json", "\"{not json\"")]
    [TestCase("""{"application/json":{"examples":{"first":{"value":5},"second":{"value":6}},"schema":{"example":7}}}""", "application/json", "5")]
    [TestCase("""{"application/json":{"schema":{"example":7}}}""", "application/json", "7")]
    [TestCase("""{"application/json":{}}""", "application/json", "")]
    [TestCase("""{"application/xml":{"example":"<a/>"}}""", "application/xml", "<a/>")]
    [TestCase("""{"text/plain":{"schema":{"type":"string","default":"hello"}}}""", "text/plain", "hello")]
    public void ARawBodyIsTheExampleOrASample(string content, string expectedType, string expectedText)
    {
        var read = ReadBody(content);

        Assert.That(read.Body, Is.EqualTo(new RawBody(expectedType, expectedText)));
    }

    [Test]
    public void AJsonExampleKeepsItsOrderAndCharacters()
    {
        var read = ReadBody("""{"application/json":{"example":{"b":"<é>","a":[1.50,null,{}]}}}""");

        var text = ((RawBody)read.Body).Text;
        Assert.That(text, Is.EqualTo("{\n  \"b\": \"<é>\",\n  \"a\": [\n    1.50,\n    null,\n    {}\n  ]\n}"));
    }

    [Test]
    public void AUrlEncodedBodyBecomesFields()
    {
        var read = ReadBody("""
            {
              "multipart/form-data": {},
              "application/x-www-form-urlencoded": {
                "schema": {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "example": "Rex" },
                    "tags": { "type": "array", "items": { "type": "string", "default": "dog" } },
                    "empty": { "type": "array", "items": { "$ref": "#/components/schemas/Missing" } },
                    "id": { "type": "integer", "readOnly": true }
                  }
                }
              }
            }
            """);

        var fields = ((FormUrlEncodedBody)read.Body).Fields;
        Assert.That(fields, Is.EqualTo(new[]
        {
            new KeyValueEntry("name", "Rex", true),
            new KeyValueEntry("tags", "dog", true),
            new KeyValueEntry("empty", string.Empty, true),
        }));
    }

    [Test]
    public void AMultipartBodyHasAFilePartForEveryFileProperty()
    {
        var read = ReadBody(
            """
            {
              "multipart/form-data": {
                "schema": {
                  "allOf": [
                    { "$ref": "#/components/schemas/Files" },
                    { "type": "object", "properties": { "note": { "type": "string", "default": "n" } } }
                  ]
                },
                "example": { "note": "from the example", "scan": "ignored" }
              }
            }
            """,
            """
            "components": { "schemas": { "Files": { "type": "object", "properties": {
              "scan": { "type": "string", "format": "binary" },
              "pages": { "type": "array", "items": { "type": "string", "format": "binary" } }
            } } } }
            """);

        var parts = ((MultipartBody)read.Body).Parts;
        Assert.That(parts, Is.EqualTo(new[]
        {
            new MultipartPart("note", "from the example", null),
            new MultipartPart("scan", null, string.Empty),
            new MultipartPart("pages", null, string.Empty),
        }));
    }

    [TestCase("""{"application/x-www-form-urlencoded":{"schema":{"type":"object","properties":{}}}}""")]
    [TestCase("""{"multipart/form-data":{}}""")]
    [TestCase("{}")]
    public void AFormWithoutFieldsAndABodyWithoutContentAreNoBody(string content)
    {
        var read = ReadBody(content);

        Assert.That(read.Body, Is.InstanceOf<NoBody>());
    }

    [Test]
    public void AnUnsupportedBodyTypeGivesNoBodyAndAWarning()
    {
        var read = ReadBody("""{"application/octet-stream":{"schema":{"type":"string","format":"binary"}}}""");

        Assert.That(read.Body, Is.InstanceOf<NoBody>());
        Assert.That(read.Warnings, Has.Member("The body type 'application/octet-stream' is not supported. The request has no body."));
    }

    [Test]
    public void ATextBodyWithoutATextSampleIsEmptyWithAWarning()
    {
        var read = ReadBody("""{"application/xml":{"schema":{"type":"object","properties":{"a":{"type":"string"}}}}}""");

        Assert.That(read.Body, Is.EqualTo(new RawBody("application/xml", string.Empty)));
        Assert.That(read.Warnings, Has.Member("No sample can be made for the body type 'application/xml', so the body is empty."));
    }

    [Test]
    public void AReferenceToARequestBodyIsFollowed()
    {
        var document = Read(Operation(
            """{ "requestBody": { "$ref": "#/components/requestBodies/New" } }""",
            """ "components": { "requestBodies": { "New": { "content": { "application/json": { "example": 8 } } } } } """));

        Assert.That(SingleRequest(document).Body, Is.EqualTo(new RawBody("application/json", "8")));
    }

    private sealed record ExchangeBody(BodyDefinition Body, IReadOnlyList<string> Warnings);
}
