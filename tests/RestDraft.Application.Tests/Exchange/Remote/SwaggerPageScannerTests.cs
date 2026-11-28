using NUnit.Framework;
using RestDraft.Application.Exchange.Remote;

namespace RestDraft.Application.Tests.Exchange.Remote;

[TestFixture]
public class SwaggerPageScannerTests
{
    private static IEnumerable<TestCaseData> Pages()
    {
        yield return Case("Swagger UI page", SwaggerPageSamples.SwaggerUiPage, "script ./swagger-initializer.js");
        yield return Case(
            "Swagger UI initializer",
            SwaggerPageSamples.SwaggerUiInitializer,
            "direct https://petstore.swagger.io/v2/swagger.json");
        yield return Case(
            "Swagger sites initializer",
            SwaggerPageSamples.SwaggerSitesInitializer,
            "host petstore3.swagger.io https://petstore3.swagger.io/api/v3/openapi.json",
            "host petstore.swagger.io https://petstore.swagger.io/v2/swagger.json",
            "host generator.swagger.io https://generator.swagger.io/api/swagger.json");
        yield return Case(
            "Swashbuckle index.js",
            SwaggerPageSamples.SwashbuckleScript,
            "listed v1/swagger.json (My API V1)",
            "listed /swagger/v2/swagger.json?a=1&b=2 (Ann's API)");
        yield return Case("swagger-ui-express with an address", SwaggerPageSamples.ExpressScriptWithAnAddress, "direct /api-docs.json");
        yield return Case(
            "springdoc initializer",
            SwaggerPageSamples.SpringdocInitializer,
            "direct https://petstore.swagger.io/v2/swagger.json",
            "config /v3/api-docs/swagger-config");
        yield return Case(
            "springdoc swagger-config",
            SwaggerPageSamples.SpringdocConfiguration,
            "listed /v3/api-docs/orders (orders)",
            "listed /v3/api-docs/users (users)",
            "config /v3/api-docs/swagger-config");
        yield return Case("FastAPI page", SwaggerPageSamples.FastApiPage, "direct /openapi.json");
        yield return Case("Redoc page", SwaggerPageSamples.RedocPage, "direct /openapi.json?a=1&b=2");
    }

    [TestCaseSource(nameof(Pages))]
    public void FindsTheLinksOfTheCommonTools(string text, string[] expected)
    {
        var links = SwaggerPageScanner.Scan(text);

        Assert.That(Describe(links), Is.EqualTo(expected));
        Assert.That(links.Embedded, Is.Empty);
    }

    [Test]
    public void TakesTheDocumentOfSwaggerUiExpressAndNoLinkOutOfIt()
    {
        var links = SwaggerPageScanner.Scan(SwaggerPageSamples.ExpressScript);

        Assert.That(Describe(links), Is.Empty);
        Assert.That(Lines(links.Embedded.Single()), Is.EqualTo(Lines(SwaggerPageSamples.ExpressDocument)));
    }

    [TestCase("""url: "/a.json",""", "direct /a.json")]
    [TestCase("""{"url":"https:\/\/h\/a.json"}""", "direct https://h/a.json")]
    [TestCase("url: '/a.json' }", "direct /a.json")]
    [TestCase("""urls: [{ url: "/a.json" }], url: "/b.json" """, "listed /a.json|direct /b.json")]
    [TestCase("""urls: [], url: "/b.json" """, "direct /b.json")]
    [TestCase("""validatorUrl: "/a", oauth2RedirectUrl: "/b", "data-url": "/c" """, "")]
    [TestCase("""window.url: "/a", myurl: "/b" """, "")]
    [TestCase("""url: "" """, "")]
    [TestCase("""url: "https://" + host + "/a.json" """, "")]
    [TestCase("url: window.location.origin", "")]
    [TestCase("  // url: \"/a.json\"", "")]
    [TestCase("""urls: [{ url: "/a.json" """, "listed /a.json")]
    [TestCase("""spec: { "url": "/inside.json" """, "")]
    public void TakesOnlyTextThatIsAnAddressFromAScript(string script, string expected)
    {
        var links = SwaggerPageScanner.Scan(script);

        Assert.That(string.Join('|', Describe(links)), Is.EqualTo(expected));
    }

    [TestCase("swagger-initializer.js", true)]
    [TestCase("./swagger-ui-init.js?v=2", true)]
    [TestCase("/docs/index.js", true)]
    [TestCase("custom-swagger-config.js", true)]
    [TestCase("swagger-ui-bundle.js", false)]
    [TestCase("swagger-ui-standalone-preset.js", false)]
    [TestCase("redoc.standalone.js", false)]
    [TestCase("init.min.js", false)]
    [TestCase("config.json", false)]
    public void ReadsOnlyTheScriptsThatMayHoldTheLinks(string source, bool isRead)
    {
        var links = SwaggerPageScanner.Scan($"<html><script src=\"{source}\"></script></html>");

        Assert.That(links.Scripts, isRead ? Is.EqualTo(new[] { source }) : Is.Empty);
    }

    [TestCase("<script src=swagger-initializer.js></script>", "script swagger-initializer.js")]
    [TestCase("<SCRIPT SRC='index.js'></SCRIPT>", "script index.js")]
    [TestCase("<html><script>url: '/a.json'", "direct /a.json")]
    [TestCase("<html><!-- <script>url: '/a.json'</script>", "")]
    [TestCase("<html><body>url: '/a.json'</body></html>", "")]
    [TestCase("<html><script></script><p>url: '/a.json'</p></html>", "")]
    public void ReadsTheScriptsOfAPage(string page, string expected)
    {
        var links = SwaggerPageScanner.Scan(page);

        Assert.That(string.Join('|', Describe(links)), Is.EqualTo(expected));
    }

    [TestCase("<!DOCTYPE html>", true)]
    [TestCase("\uFEFF  \r\n<html>", true)]
    [TestCase("{\"openapi\": \"3.0.0\"}", false)]
    [TestCase("openapi: 3.0.0", false)]
    [TestCase("window.onload = function() {}", false)]
    [TestCase("", false)]
    public void TellsAPageFromOtherText(string text, bool isPage)
    {
        Assert.That(SwaggerPageScanner.IsPage(text), Is.EqualTo(isPage));
    }

    private static TestCaseData Case(string name, string text, params string[] expected)
    {
        return new TestCaseData(text, expected).SetArgDisplayNames(name);
    }

    private static IEnumerable<string> Describe(SwaggerPageLinks links)
    {
        return
        [
            .. links.Listed.Select(link => $"listed {Describe(link)}"),
            .. links.Direct.Select(link => $"direct {Describe(link)}"),
            .. links.ByHost.Select(link => $"host {link.Host} {link.Address}"),
            .. links.Configurations.Select(address => $"config {address}"),
            .. links.Scripts.Select(address => $"script {address}"),
        ];
    }

    private static string Describe(PageLink link)
    {
        return link.Name.Length > 0 ? $"{link.Address} ({link.Name})" : link.Address;
    }

    private static IEnumerable<string> Lines(string text)
    {
        return text.Split('\n').Select(line => line.Trim());
    }
}
