using NUnit.Framework;
using RestDraft.Domain.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

[TestFixture]
public class OpenApiParameterTests
{
    [TestCase("""{"schema":{"type":"string"}}""", "/pets/{{petId}}")]
    [TestCase("""{"schema":{"type":"string","enum":["a","b"]}}""", "/pets/{{petId}}")]
    [TestCase("""{"example":7,"schema":{"type":"integer","default":3}}""", "/pets/7")]
    [TestCase("""{"examples":{"one":{"value":"first"},"two":{"value":"second"}}}""", "/pets/first")]
    [TestCase("""{"schema":{"type":"integer","default":3}}""", "/pets/3")]
    [TestCase("""{"schema":{"type":"string","example":"fromSchema","default":"d"}}""", "/pets/fromSchema")]
    [TestCase("""{"x-example":"fromExtension","schema":{"type":"string","default":"d"}}""", "/pets/fromExtension")]
    [TestCase("""{"content":{"application/json":{"example":"fromContent"}}}""", "/pets/fromContent")]
    [TestCase("""{"example":"a b/c?d#e"}""", "/pets/a%20b%2Fc%3Fd%23e")]
    [TestCase("""{"schema":{"type":"array","items":{"type":"string","default":"x"}}}""", "/pets/x")]
    public void APathParameterIsTheGivenValueOrAVariable(string parameter, string expectedUrl)
    {
        var members = "\"name\":\"petId\",\"in\":\"path\",\"required\":true," + parameter[1..];

        var request = SingleRequest(Read(Operation("{\"parameters\":[{" + members + "]}")
            .Replace("/items", "/pets/{petId}", StringComparison.Ordinal)));

        Assert.That(request.Url, Is.EqualTo(expectedUrl));
    }

    [Test]
    public void AVariableOfThePathWithoutAParameterBecomesAVariable()
    {
        var request = SingleRequest(Read(Json("""{ "/a/{first}/b/{second-id}.json": { "get": {} } }""")));

        Assert.That(request.Url, Is.EqualTo("/a/{{first}}/b/{{second-id}}.json"));
    }

    [Test]
    public void QueryAndHeaderParametersAreAllListedAndRequiredOnesAreEnabled()
    {
        var request = SingleRequest(Read(Operation("""
            {
              "parameters": [
                { "name": "must", "in": "query", "required": true, "example": "1" },
                { "name": "may", "in": "query" },
                { "name": "X-Must", "in": "header", "required": true, "schema": { "type": "boolean", "default": false } },
                { "name": "X-May", "in": "header", "schema": { "default": null } }
              ]
            }
            """)));

        Assert.That(request.QueryParams, Is.EqualTo(new[] { new KeyValueEntry("must", "1", true), new KeyValueEntry("may", string.Empty, false) }));
        Assert.That(request.Headers, Is.EqualTo(new[] { new KeyValueEntry("X-Must", "false", true), new KeyValueEntry("X-May", string.Empty, false) }));
    }

    [TestCase("\"style\":\"form\"", new[] { "a", "b" })]
    [TestCase("\"explode\":false", new[] { "a,b" })]
    [TestCase("\"style\":\"pipeDelimited\",\"explode\":false", new[] { "a|b" })]
    [TestCase("\"style\":\"spaceDelimited\",\"explode\":false", new[] { "a b" })]
    public void AListInTheQueryIsSplitOrJoined(string style, string[] expected)
    {
        var request = SingleRequest(Read(Operation(
            "{\"parameters\":[{\"name\":\"q\",\"in\":\"query\"," + style + ",\"example\":[\"a\",\"b\"]}]}")));

        Assert.That(request.QueryParams.Select(entry => entry.Value), Is.EqualTo(expected));
    }

    [Test]
    public void AParameterOfTheOperationReplacesTheOneOfThePath()
    {
        var request = SingleRequest(Read(Json("""
            {
              "/a": {
                "parameters": [
                  { "name": "q", "in": "query", "example": "path" },
                  { "name": "other", "in": "query", "example": "kept" }
                ],
                "get": { "parameters": [{ "name": "q", "in": "query", "required": true, "example": "operation" }] }
              }
            }
            """)));

        Assert.That(
            request.QueryParams,
            Is.EqualTo(new[] { new KeyValueEntry("q", "operation", true), new KeyValueEntry("other", "kept", false) }));
    }

    [Test]
    public void AReferenceToAParameterIsFollowed()
    {
        var request = SingleRequest(Read(Operation(
            """{ "parameters": [{ "$ref": "#/components/parameters/Page" }] }""",
            """ "components": { "parameters": { "Page": { "name": "page", "in": "query", "schema": { "default": 2 } } } } """)));

        Assert.That(request.QueryParams, Is.EqualTo(new[] { new KeyValueEntry("page", "2", false) }));
    }

    [Test]
    public void ACookieParameterIsLeftOutWithAWarning()
    {
        var document = Read(Operation("""{ "parameters": [{ "name": "session", "in": "cookie" }] }"""));

        var request = SingleRequest(document);
        Assert.That(request.QueryParams, Is.Empty);
        Assert.That(request.Headers, Is.Empty);
        Assert.That(Messages(document), Has.Member("Cookie parameters are not imported."));
    }
}
