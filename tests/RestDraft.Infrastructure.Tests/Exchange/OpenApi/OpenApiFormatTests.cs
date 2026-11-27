using NUnit.Framework;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

// Which texts the format reads, the server address, the warnings for the whole file, and files that are broken or hostile.
[TestFixture]
public class OpenApiFormatTests
{
    private const string Info = """ "info": { "title": "T", "version": "1" }, "paths": {} """;

    private static readonly string[] DroppedParts =
    [
        "Descriptions are not imported.",
        "Response definitions are not imported.",
        "Callbacks are not imported.",
        "Webhooks are not imported.",
    ];

    private static readonly string[] ImportedUrls = ["/good", "/worse"];

    [TestCase("openapi: 3.0.0\n", true)]
    [TestCase("info: {}\nswagger: '2.0'\n", true)]
    [TestCase("""{"openapi":"3.1.0"}""", true)]
    [TestCase("""  {"components":{"a":[1,{"openapi":2}]},"swagger":"2.0"} """, true)]
    [TestCase("{\n\t\"paths\": {},\n\t\"openapi\": \"3.0.0\"\n}", true)]
    [TestCase("""{"openapi":"3.0.0", "paths": {""", true)]
    [TestCase("""{"info":{"openapi":"3.0.0"}}""", false)]
    [TestCase("info:\n  openapi: 3.0.0\n", false)]
    [TestCase("type: collection.insomnia.rest/5.0\n", false)]
    [TestCase("""{"_type":"export"}""", false)]
    [TestCase("""{"broken""", false)]
    [TestCase("- openapi\n", false)]
    [TestCase("just text", false)]
    [TestCase("", false)]
    public void RecognisesASpecificationByItsTopLevelKey(string text, bool expected)
    {
        var result = Create().CanRead(text);

        Assert.That(result.Value, Is.EqualTo(expected));
    }

    // The address as the file declares it. BaseUrlResolver makes the base URL of it.
    [TestCase("""{"swagger":"2.0","host":"h.example:81","basePath":"/api","schemes":["http","https"],INFO}""", "http://h.example:81/api")]
    [TestCase("""{"swagger":"2.0","host":"h.example",INFO}""", "//h.example")]
    [TestCase("""{"swagger":"2.0","basePath":"/api",INFO}""", "/api")]
    [TestCase("""{"swagger":"2.0","basePath":"/api","schemes":["https"],INFO}""", "https:///api")]
    [TestCase("""{"swagger":"2.0",INFO}""", "")]
    [TestCase("""{"openapi":"3.0.0",INFO}""", "")]
    [TestCase("""{"openapi":"3.0.0","servers":[],INFO}""", "")]
    [TestCase("""{"openapi":"3.0.0","servers":[{"url":"v1"}],INFO}""", "v1")]
    [TestCase("""{"openapi":"3.0.0","servers":[{"url":"https://{host}:{port}/{none}","variables":{"host":{"default":"a.example"},"port":{"default":"8443"}}}],INFO}""", "https://a.example:8443/{{none}}")]
    [TestCase("""{"openapi":"3.0.0","servers":[{"url":"{{base}}/v2"}],INFO}""", "{{base}}/v2")]
    public void ReportsTheDeclaredServerAddress(string specification, string expected)
    {
        var document = Read(specification.Replace("INFO", Info, StringComparison.Ordinal));

        Assert.That(document.BaseUrl, Is.EqualTo(expected));
    }

    [Test]
    public void SeveralServersAndServersOfOperationsGiveWarnings()
    {
        var document = Read(Json(
            """{ "/a": { "get": { "servers": [{ "url": "https://other" }] } } }""",
            """ "servers": [{ "url": "https://first" }, { "url": "https://second" }] """));

        Assert.That(document.BaseUrl, Is.EqualTo("https://first"));
        Assert.That(Messages(document), Has.Member("The specification has several server addresses. The first one is used."));
        Assert.That(Messages(document), Has.Member("Server addresses of single paths or operations are not used."));
    }

    [Test]
    public void WhatIsNotImportedIsNamedOnce()
    {
        var document = Read(Json(
            """
            {
              "/a": {
                "get": { "description": "d", "responses": { "200": { "description": "ok" } }, "callbacks": { "c": {} } },
                "post": { "description": "d", "responses": { "200": { "description": "ok" } } }
              }
            }
            """,
            """ "webhooks": { "w": { "post": {} } } """).Replace("3.0.3", "3.1.0", StringComparison.Ordinal));

        Assert.That(Messages(document), Is.EquivalentTo(DroppedParts));
    }

    [Test]
    public void ErrorsOfTheSpecificationAreReportedAndTheRestIsImported()
    {
        var document = Read(Json("""
            {
              "/good": { "get": {} },
              "/bad": 5,
              "/worse": { "get": { "parameters": [{ "name": "q", "in": "nowhere" }] } }
            }
            """));

        var warning = document.Warnings.Single(candidate => candidate.Message.StartsWith("The specification has", StringComparison.Ordinal));
        Assert.That(warning.Message, Does.Match(@"has \d+ errors, so something may be missing\. The first one: "));
        Assert.That(warning.Path, Does.StartWith("#/paths/"));
        Assert.That(AllRequests(document.Collection.Root).Select(request => request.Url), Is.EqualTo(ImportedUrls));
    }

    [Test]
    public void APropertyThatTheReaderSkipsIsToldApartFromAnError()
    {
        var document = Read(Json("""{ "/a": { "get": {}, "unheard": {} } }"""));

        var warning = document.Warnings.Single();
        Assert.That(warning.Message, Does.StartWith("The specification has 1 property that the reader skipped, as unknown."));
    }

    [TestCase("""{"openapi":"4.0.0",INFO}""", "'4.0.0' is not supported")]
    [TestCase("""{"swagger":"1.2",INFO}""", "'1.2' is not supported")]
    [TestCase("""{"openapi":{},INFO}""", "could not be read")]
    [TestCase("""{"openapi":"3.0.0", "paths": {""", "not valid JSON")]
    [TestCase("""{"openapi":"3.0.0","openapi":"3.0.0"}""", "not valid JSON")]
    [TestCase("openapi: 4.0.0\n", "'4.0.0' is not supported")]
    [TestCase("openapi: &v 3.0.0\ncopy: *v\n", "aliases")]
    // Files that the reader library fails on with an exception of its own choice.
    [TestCase("""{"openapi":"3.0.0","paths":{"/a":{"get":{"tags":["  "]}}}}""", "could not be read")]
    [TestCase("""{"openapi":"3.0.0","paths":{"/a":{"get":{"parameters":[{"$ref":""}]}}}}""", "could not be read")]
    [TestCase("""{"swagger":"2.0","paths":{"/a":{"get":{"parameters":[{"$ref":5}]}}}}""", "could not be read")]
    [TestCase("""{"openapi":"3.0.0","paths":{},"servers":[{"url":"x{a}","variables":{"a":null}}]}""", "could not be read")]
    public void ABrokenFileIsRefusedWithTheReason(string specification, string expectedInMessage)
    {
        var result = Create().Read(specification.Replace("INFO", Info, StringComparison.Ordinal));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Does.Contain(expectedInMessage));
    }

    [TestCase("{\"openapi\":\"3.0.0\",\"x\":", "[", "]", "}", "maximum configured depth")]
    [TestCase("openapi: 3.0.0\nx: ", "[", "]", "", "nested too deeply")]
    public void ExtremeNestingIsRefused(string start, string open, string close, string end, string expectedInMessage)
    {
        const int Levels = 5000;
        var text = start + string.Concat(Enumerable.Repeat(open, Levels)) + string.Concat(Enumerable.Repeat(close, Levels)) + end;

        var result = Create().Read(text);

        Assert.That(result.Error?.Message, Does.Contain(expectedInMessage));
    }

    // The address of the other file is never asked for: the format has no way to download or open anything.
    [TestCase("https://example.invalid/other.json")]
    [TestCase("other.yaml")]
    [TestCase("file:///c:/secret.json")]
    public void ReferencesToOtherFilesAreNotFollowed(string file)
    {
        var document = Read(Json(
            $$"""
            {
              "/a": {
                "post": {
                  "parameters": [{ "$ref": "{{file}}#/components/parameters/P" }],
                  "requestBody": { "content": { "application/json": { "schema": { "$ref": "{{file}}#/components/schemas/S" } } } }
                }
              },
              "/b": { "$ref": "{{file}}#/paths/b" }
            }
            """));

        var request = SingleRequest(document);
        Assert.That(request.QueryParams, Is.Empty);
        Assert.That(Messages(document), Has.Member("References to other files are not followed."));
    }

    [Test]
    public void ReferencesThatLeadBackToThemselvesAreLeftOut()
    {
        var document = Read(Operation(
            """{ "parameters": [{ "$ref": "#/components/parameters/A" }], "requestBody": { "$ref": "#/components/requestBodies/A" } }""",
            """
            "components": {
              "parameters": { "A": { "$ref": "#/components/parameters/B" }, "B": { "$ref": "#/components/parameters/A" } },
              "requestBodies": { "A": { "$ref": "#/components/requestBodies/A" } }
            }
            """));

        var request = SingleRequest(document);
        Assert.That(request.QueryParams, Is.Empty);
        Assert.That(Messages(document), Has.Some.Contains("leads back to itself"));
    }
}
