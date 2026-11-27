using NUnit.Framework;
using RestDraft.Domain.Bodies;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

// How a value written in YAML arrives in a request. These tests go through the format, so they hold for any YAML reader.
[TestFixture]
public class OpenApiYamlValueTests
{
    private const string Specification = """
        openapi: 3.0.3
        info:
          title: Values
          version: 1.0
        paths:
          /items:
            post:
              parameters:
                - name: fromExample
                  in: query
                  example: EXAMPLE
                - name: fromDefault
                  in: query
                  schema:
                    default: EXAMPLE
              requestBody:
                content:
                  application/json:
                    schema:
                      type: object
                      properties:
                        fromExample:
                          example: EXAMPLE
                        fromDefault:
                          default: EXAMPLE
        """;

    // written: the value in the YAML file. sent: the text of a parameter. json: the value in a JSON body.
    [TestCase("1.50", "1.50", "1.50")]
    [TestCase("-0", "-0", "-0")]
    [TestCase("12", "12", "12")]
    [TestCase("1e3", "1e3", "1e3")]
    [TestCase("123456789012345678901234567890", "123456789012345678901234567890", "123456789012345678901234567890")]
    [TestCase("00501", "00501", "\"00501\"")]
    [TestCase("+12", "+12", "\"+12\"")]
    [TestCase("0x1F", "0x1F", "\"0x1F\"")]
    [TestCase(".5", ".5", "\".5\"")]
    [TestCase("1.", "1.", "\"1.\"")]
    [TestCase("true", "true", "true")]
    [TestCase("True", "true", "true")]
    [TestCase("FALSE", "false", "false")]
    [TestCase("yes", "yes", "\"yes\"")]
    [TestCase("null", "", "null")]
    [TestCase("~", "", "null")]
    [TestCase("2026-01-31", "2026-01-31", "\"2026-01-31\"")]
    [TestCase("\"12\"", "12", "\"12\"")]
    [TestCase("'true'", "true", "\"true\"")]
    [TestCase("'null'", "null", "\"null\"")]
    [TestCase("\"\"", "", "\"\"")]
    [TestCase("!!str 12", "12", "\"12\"")]
    [TestCase("plain text: with <marks> & é", null, null)]
    [TestCase("\"plain text: with <marks> & é\"", "plain text: with <marks> & é", "\"plain text: with <marks> & é\"")]
    public void AValueKeepsItsMeaning(string written, string? sent, string? json)
    {
        var result = Create().Read(Specification.Replace("EXAMPLE", written, StringComparison.Ordinal));

        if (sent is null)
        {
            // A colon followed by a space is not allowed in a value without quotes.
            Assert.That(result.Error?.Message, Does.Contain("not valid YAML"));
            return;
        }

        Assert.That(result.IsSuccess, Is.True, result.Error?.Message);
        var request = SingleRequest(result.Value!);
        Assert.That(request.QueryParams.Select(entry => entry.Value), Is.All.EqualTo(sent));
        var body = (RawBody)request.Body;
        Assert.That(body.Text, Is.EqualTo($"{{\n  \"fromExample\": {json},\n  \"fromDefault\": {json}\n}}"));
    }

    [TestCase("|-\n                          123", "\"123\"")]
    [TestCase(">-\n                          true", "\"true\"")]
    [TestCase("|\n                          a\n                          b", "\"a\\nb\"")]
    public void ABlockValueIsText(string written, string json)
    {
        var yaml = """
            openapi: 3.0.3
            info: { title: Blocks, version: "1" }
            paths:
              /items:
                post:
                  requestBody:
                    content:
                      application/json:
                        schema:
                          example: BLOCK
            """.Replace("BLOCK", written, StringComparison.Ordinal);

        var request = SingleRequest(Read(yaml));

        Assert.That(((RawBody)request.Body).Text, Is.EqualTo(json));
    }

    [Test]
    public void KeysAreTextAlsoWhenTheyLookLikeNumbers()
    {
        var document = Read("""
            openapi: 3.0.3
            info:
              title: 2026
              version: 1.0
            paths:
              /items:
                get:
                  responses:
                    200:
                      description: ok
                  requestBody:
                    content:
                      application/json:
                        example:
                          1: one
                          true: yes
            """);

        Assert.That(document.Collection.Name, Is.EqualTo("2026"));
        var body = (RawBody)SingleRequest(document).Body;
        Assert.That(body.Text, Is.EqualTo("{\n  \"1\": \"one\",\n  \"true\": \"yes\"\n}"));
        Assert.That(Messages(document), Has.None.Contains("error"));
    }

    [TestCase("openapi: 3.0.3\npaths:\n  /a: {}\n  /a: {}\n", "Duplicate key")]
    [TestCase("openapi: 3.0.3\n? [a, b]\n: value\n", "key that is not text")]
    [TestCase("openapi: 3.0.3\n---\nother: 1\n", "exactly one YAML document")]
    [TestCase("openapi: 3.0.3\npaths: [unclosed\n", "not valid YAML")]
    [TestCase("openapi: 3.0.3\na: &x 1\nb: *x\n", "aliases")]
    public void AFileThatCannotBeConvertedIsRefused(string yaml, string expectedInMessage)
    {
        var result = Create().Read(yaml);

        Assert.That(result.Error?.Message, Does.Contain(expectedInMessage));
    }
}
