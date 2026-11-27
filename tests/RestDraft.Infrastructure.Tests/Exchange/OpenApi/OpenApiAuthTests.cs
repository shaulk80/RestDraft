using NUnit.Framework;
using RestDraft.Domain.Auth;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

[TestFixture]
public class OpenApiAuthTests
{
    private const string BearerWarning = "OAuth 2 and OpenID Connect are set as a bearer token, {{token}}.";

    private static IEnumerable<TestCaseData> Schemes()
    {
        yield return Case("basic", """{"type":"http","scheme":"Basic"}""", new BasicAuth("{{username}}", "{{password}}"), null);
        yield return Case("bearer", """{"type":"http","scheme":"bearer"}""", new BearerAuth("{{token}}"), null);
        yield return Case("API key in a header", """{"type":"apiKey","in":"header","name":"X-Key"}""", new ApiKeyAuth("X-Key", "{{apiKey}}", ApiKeyLocation.Header), null);
        yield return Case("API key in the query", """{"type":"apiKey","in":"query","name":"key"}""", new ApiKeyAuth("key", "{{apiKey}}", ApiKeyLocation.Query), null);
        yield return Case("API key in a cookie", """{"type":"apiKey","in":"cookie","name":"sid"}""", new NoAuth(), "API keys in cookies are not supported. Their requests have no authentication.");
        yield return Case("OAuth 2", """{"type":"oauth2","flows":{}}""", new BearerAuth("{{token}}"), BearerWarning);
        yield return Case("OpenID Connect", """{"type":"openIdConnect","openIdConnectUrl":"https://x/.well-known"}""", new BearerAuth("{{token}}"), BearerWarning);
        yield return Case("digest", """{"type":"http","scheme":"digest"}""", new NoAuth(), "The authentication scheme 's' is not supported. Its requests have no authentication.");
        yield return Case("mutual TLS", """{"type":"mutualTLS"}""", new NoAuth(), "The authentication scheme 's' is not supported. Its requests have no authentication.");
    }

    [TestCaseSource(nameof(Schemes))]
    public void ASchemeBecomesTheAuthenticationOfTheRequest(string scheme, AuthDefinition expected, string? warning)
    {
        var specification = Operation(
            "{}",
            "\"security\":[{\"s\":[]}],\"components\":{\"securitySchemes\":{\"s\":" + scheme + "}}")
            .Replace("3.0.3", "3.1.0", StringComparison.Ordinal);

        var document = Read(specification);

        Assert.That(SingleRequest(document).Auth, Is.EqualTo(expected));
        Assert.That(Messages(document), warning is null ? Is.Empty : Is.EqualTo(new[] { warning }));
    }

    [TestCase("{}", "global")]
    [TestCase("""{"security":[]}""", "")]
    [TestCase("""{"security":[{}]}""", "")]
    [TestCase("""{"security":[{"own":[]}]}""", "own")]
    [TestCase("""{"security":[{},{"own":[]}]}""", "own")]
    [TestCase("""{"security":[{"own":[]},{"global":[]}]}""", "own")]
    public void TheSecurityOfTheOperationReplacesTheOneOfTheDocument(string operation, string expectedKey)
    {
        var document = Read(Operation(operation, """
            "security": [{ "global": [] }],
            "components": { "securitySchemes": {
              "global": { "type": "apiKey", "in": "header", "name": "global" },
              "own": { "type": "apiKey", "in": "header", "name": "own" }
            } }
            """));

        var auth = SingleRequest(document).Auth;
        Assert.That(auth is ApiKeyAuth key ? key.Key : string.Empty, Is.EqualTo(expectedKey));
    }

    [Test]
    public void OfSchemesThatAreCombinedTheFirstOneIsUsedWithAWarning()
    {
        var document = Read(Operation(
            """{ "security": [{ "first": [], "second": [] }] }""",
            """
            "components": { "securitySchemes": {
              "first": { "type": "http", "scheme": "bearer" },
              "second": { "type": "http", "scheme": "basic" }
            } }
            """));

        Assert.That(SingleRequest(document).Auth, Is.InstanceOf<BearerAuth>());
        Assert.That(Messages(document), Has.Some.Contains("Only the first one is set"));
    }

    [TestCase("{}", "The reference 'ghost' leads to nothing in the specification.")]
    [TestCase("""{"securitySchemes":{"ghost":{"$ref":"#/components/securitySchemes/ghost"}}}""", "The reference 'ghost' leads back to itself and is left out.")]
    public void ASchemeThatCannotBeFoundGivesNoAuthenticationAndAWarning(string components, string expected)
    {
        var document = Read(Operation("""{ "security": [{ "ghost": [] }] }""", "\"components\":" + components));

        Assert.That(SingleRequest(document).Auth, Is.InstanceOf<NoAuth>());
        Assert.That(Messages(document), Has.Member(expected));
    }

    private static TestCaseData Case(string name, string scheme, AuthDefinition expected, string? warning)
    {
        return new TestCaseData(scheme, expected, warning).SetName($"{{m}}({name})");
    }
}
