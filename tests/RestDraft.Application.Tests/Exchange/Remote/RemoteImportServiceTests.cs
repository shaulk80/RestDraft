using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Tests.Exchange.Remote;

[TestFixture]
public class RemoteImportServiceTests
{
    private const string PageAddress = "http://api.test/swagger/index.html";
    private const string Specification = "SPEC";
    private const string BrokenSpecification = "SPEC broken";

    private FakeSpecificationDownloader _web = null!;
    private ExchangeDocument _document = null!;
    private DownloadHeaders _headers = null!;
    private RemoteImportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _web = new FakeSpecificationDownloader();
        var root = new Folder(Guid.NewGuid(), string.Empty, [], []);
        _document = new ExchangeDocument(new Collection(Guid.NewGuid(), "Shop", root), [], "/api");
        var format = A.Fake<ICollectionFormat>();
        A.CallTo(() => format.CanRead(A<string>._))
            .ReturnsLazily((string text) => Result<bool>.Success(text.StartsWith("SPEC", StringComparison.Ordinal) || text.Contains("\"openapi\"", StringComparison.Ordinal)));
        A.CallTo(() => format.Read(A<string>._)).ReturnsLazily((string text) => text == BrokenSpecification
            ? Result<ExchangeDocument>.Failure(ResultErrors.Validation("The specification is broken."))
            : Result<ExchangeDocument>.Success(_document));
        _headers = new DownloadHeaders([new KeyValueEntry("X-Token", "t", true)], new Uri(PageAddress));
        _service = new RemoteImportService(_web, new CollectionTextReader([format]));
    }

    [Test]
    public async Task ASpecificationAtTheAddressIsRead()
    {
        _web.Serve("http://api.test/openapi.json", Specification);

        var result = await ReadAsync("http://api.test/openapi.json");

        AssertFound(result, "http://api.test/openapi.json");
        Assert.That(_web.Asked, Has.Count.EqualTo(1));
        Assert.That(_web.Headers.Single(), Is.SameAs(_headers));
    }

    [Test]
    public async Task ABrokenSpecificationAtTheAddressGivesItsReason()
    {
        _web.Serve("http://api.test/openapi.json", BrokenSpecification);

        var result = await ReadAsync("http://api.test/openapi.json");

        Assert.That(result.Error?.Message, Is.EqualTo("The specification is broken."));
    }

    [Test]
    public async Task AFailedFirstDownloadEndsTheSearch()
    {
        _web.Fail(PageAddress, "The server answered 401 (Unauthorized).");

        var result = await ReadAsync(PageAddress);

        Assert.That(result.Error?.Message, Is.EqualTo("The server answered 401 (Unauthorized)."));
        Assert.That(_web.Asked, Has.Count.EqualTo(1));
    }

    [TestCase("hello")]
    [TestCase("{\"status\": \"ok\"}")]
    public async Task TextThatIsNeitherASpecificationNorAPageEndsTheSearch(string text)
    {
        _web.Serve("http://api.test/status", text);

        var result = await ReadAsync("http://api.test/status");

        Assert.That(result.Error?.Message, Does.Contain("neither a specification nor a Swagger UI page"));
        Assert.That(_web.Asked, Has.Count.EqualTo(1));
    }

    [TestCase("v1/spec.json", "http://api.test/swagger/v1/spec.json")]
    [TestCase("./spec.json", "http://api.test/swagger/spec.json")]
    [TestCase("../spec.json", "http://api.test/spec.json")]
    [TestCase("/spec.json", "http://api.test/spec.json")]
    [TestCase("//docs.test/spec.json", "http://docs.test/spec.json")]
    [TestCase("https://docs.test/spec.json", "https://docs.test/spec.json")]
    public async Task TheLinkOfAPageIsTakenRelativeToThePage(string link, string expected)
    {
        _web.Serve(PageAddress, PageWith($"url: '{link}'"));
        _web.Serve(expected, Specification);

        var result = await ReadAsync(PageAddress);

        AssertFound(result, expected);
    }

    [Test]
    public async Task ALinkInAScriptIsTakenRelativeToThePageAfterARedirect()
    {
        _web.Redirect("http://api.test/swagger", PageAddress, "<script src='js/swagger-initializer.js'></script>");
        _web.Serve("http://api.test/swagger/js/swagger-initializer.js", "url: 'spec.json'");
        _web.Serve("http://api.test/swagger/spec.json", Specification);

        var result = await ReadAsync("http://api.test/swagger");

        AssertFound(result, "http://api.test/swagger/spec.json");
    }

    [TestCase("http://cdn.test/swagger-initializer.js")]
    [TestCase("https://api.test/swagger-initializer.js")]
    [TestCase("http://api.test:81/swagger-initializer.js")]
    public async Task AScriptOfAnotherHostIsNotRead(string script)
    {
        _web.Serve(PageAddress, $"<script src='{script}'></script>");
        _web.Serve(script, "url: 'spec.json'");

        await ReadAsync(PageAddress);

        Assert.That(_web.Asked, Does.Not.Contain(script));
    }

    [Test]
    public async Task AScriptThatRedirectsToAnotherHostIsNotUsed()
    {
        _web.Serve(PageAddress, "<script src='swagger-initializer.js'></script>");
        _web.Redirect("http://api.test/swagger/swagger-initializer.js", "http://cdn.test/init.js", "url: '/from-cdn.json'");
        _web.Serve("http://api.test/from-cdn.json", Specification);

        var result = await ReadAsync(PageAddress);

        Assert.That(result.IsSuccess, Is.False);
    }

    // The script has no link of its own, only a list that names the specification of each host.
    [TestCase("petstore.swagger.io", "https://petstore.swagger.io/v2/swagger.json")]
    [TestCase("petstore3.swagger.io", "https://petstore3.swagger.io/api/v3/openapi.json")]
    [TestCase("generator.swagger.io", "https://generator.swagger.io/api/swagger.json")]
    public async Task AListOfSpecificationsByHostGivesTheOneOfThePageHost(string host, string expected)
    {
        _web.Serve($"https://{host}/", "<script src='./swagger-initializer.js'></script>");
        _web.Serve($"https://{host}/swagger-initializer.js", SwaggerPageSamples.SwaggerSitesInitializer);
        _web.Serve(expected, Specification);

        var result = await ReadAsync($"https://{host}/");

        AssertFound(result, expected);
        Assert.That(_web.Asked, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task AListOfSpecificationsByHostIsOfNoUseOnAnotherHost()
    {
        _web.Serve(PageAddress, PageWith("var list = `petstore.swagger.io=https://petstore.swagger.io/v2/swagger.json`"));
        _web.Serve("https://petstore.swagger.io/v2/swagger.json", Specification);

        var result = await ReadAsync(PageAddress);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(_web.Asked, Does.Not.Contain("https://petstore.swagger.io/v2/swagger.json"));
    }

    [Test]
    public async Task SeveralSpecificationsAreOfferedWithoutDownloadingThem()
    {
        _web.Serve(PageAddress, PageWith("urls: [{url: 'v1.json', name: 'One'}, {url: '/v2.json'}, {url: 'v1.json', name: 'Again'}]"));

        var result = await ReadAsync(PageAddress);

        var choices = result.Value as SpecificationChoices;
        Assert.That(choices?.Links, Is.EqualTo(new[]
        {
            new SpecificationLink("One", new Uri("http://api.test/swagger/v1.json")),
            new SpecificationLink("http://api.test/v2.json", new Uri("http://api.test/v2.json")),
        }));
        Assert.That(_web.Asked, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AConfigurationFileDecidesOverTheLinkOfThePage()
    {
        _web.Serve(PageAddress, PageWith("url: 'https://sample.test/petstore.json', configUrl: '/config'"));
        _web.Serve("http://api.test/config", """{"url": "/v3/spec"}""");
        _web.Serve("https://sample.test/petstore.json", Specification);
        _web.Serve("http://api.test/v3/spec", Specification);

        var result = await ReadAsync(PageAddress);

        AssertFound(result, "http://api.test/v3/spec");
        Assert.That(_web.Asked, Does.Not.Contain("https://sample.test/petstore.json"));
    }

    // The page keeps the link to the sample of Swagger UI beside the configuration file, which must not be imported instead.
    [TestCase("""{"validatorUrl": ""}""", "/own.json", "http://api.test/own.json")]
    [TestCase(null, "/own.json", "http://api.test/own.json")]
    [TestCase(null, "https://sample.test/petstore.json", null)]
    public async Task AConfigurationFileWithoutLinksLeavesOnlyTheLinksToTheSameHost(string? configuration, string link, string? expected)
    {
        _web.Serve(PageAddress, PageWith($"url: '{link}', configUrl: '/config'"));
        if (configuration is not null)
        {
            _web.Serve("http://api.test/config", configuration);
        }

        _web.Serve("http://api.test/own.json", Specification);
        _web.Serve("https://sample.test/petstore.json", Specification);

        var result = await ReadAsync(PageAddress);

        var found = result.Value as FoundSpecification;
        Assert.That(found?.Address, Is.EqualTo(expected is null ? null : new Uri(expected)));
    }

    [Test]
    public async Task ASpecificationInsideAScriptIsReadWithTheAddressOfThePage()
    {
        _web.Serve(PageAddress, "<script src='swagger-ui-init.js'></script>");
        _web.Serve("http://api.test/swagger/swagger-ui-init.js", """var options = { "swaggerDoc": { "openapi": "3.0.0" } };""");

        var result = await ReadAsync(PageAddress);

        AssertFound(result, PageAddress);
        Assert.That(_web.Asked, Has.Count.EqualTo(2));
    }

    [TestCase("http://api.test/swagger/swagger/v1/swagger.json")]
    [TestCase("http://api.test/swagger/v1/swagger.json")]
    [TestCase("http://api.test/swagger/openapi.json")]
    [TestCase("http://api.test/openapi/v1.json")]
    [TestCase("http://api.test/v3/api-docs")]
    public async Task TheUsualPathsAreTriedWhenThePageLeadsNowhere(string address)
    {
        _web.Serve(PageAddress, PageWith("url: '/gone.json'"));
        _web.Serve(address, Specification);

        var result = await ReadAsync(PageAddress);

        AssertFound(result, address);
        Assert.That(_web.Asked[1], Is.EqualTo("http://api.test/gone.json"));
    }

    [Test]
    public async Task APageAtAUsualPathDoesNotEndTheSearch()
    {
        _web.Serve(PageAddress, "<html></html>");
        _web.Serve("http://api.test/swagger/v1/swagger.json", "<html>not found</html>");
        _web.Serve("http://api.test/openapi.json", Specification);

        var result = await ReadAsync(PageAddress);

        AssertFound(result, "http://api.test/openapi.json");
    }

    [Test]
    public async Task ABrokenSpecificationBehindThePageGivesItsReason()
    {
        _web.Serve(PageAddress, PageWith("url: 'spec.json'"));
        _web.Serve("http://api.test/swagger/spec.json", BrokenSpecification);

        var result = await ReadAsync(PageAddress);

        Assert.That(result.Error?.Message, Is.EqualTo("The specification is broken."));
    }

    [Test]
    public async Task WhatWasTriedIsReportedWhenNothingIsFound()
    {
        _web.Serve(PageAddress, PageWith("url: 'http://user:secret@api.test/gone.json?key=secret'"));
        _web.Serve("http://api.test/swagger.json", "{}");

        var result = await ReadAsync(PageAddress);

        var message = result.Error!.Message;
        Assert.That(message, Does.StartWith($"No specification was found at {PageAddress}."));
        Assert.That(message, Does.Contain("http://api.test/gone.json: The server answered 404 (Not Found)."));
        Assert.That(message, Does.Contain("http://api.test/swagger.json: Not a specification."));
        Assert.That(message, Does.Not.Contain("secret"));
        Assert.That(_web.Asked, Has.Count.EqualTo(12));
        Assert.That(_web.Asked, Is.Unique);
    }

    [Test]
    public async Task TheSearchStopsAtTheDownloadLimit()
    {
        var scripts = Enumerable.Range(1, 30).Select(number => $"<script src='init{number}.js'></script>");
        _web.Serve(PageAddress, string.Concat(scripts));

        var result = await ReadAsync(PageAddress);

        Assert.That(_web.Asked, Has.Count.EqualTo(20));
        Assert.That(result.Error?.Message, Does.Contain("stopped after 20 downloads"));
    }

    [TestCase("url: 'ftp://api.test/spec.json'")]
    [TestCase("url: 'file:///etc/passwd'")]
    [TestCase("url: 'javascript:alert(1)'")]
    public async Task ALinkThatIsNotAWebAddressIsNotFollowed(string script)
    {
        _web.Serve(PageAddress, PageWith(script));

        await ReadAsync(PageAddress);

        Assert.That(_web.Asked, Is.All.StartsWith("http://api.test/"));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public async Task ACancelledSearchStopsDownloading(int cancelledAt)
    {
        using var cancellation = new CancellationTokenSource();
        _web.Serve(PageAddress, PageWith("url: 'spec.json'"));
        _web.OnDownload = _ =>
        {
            if (_web.Asked.Count == cancelledAt)
            {
                cancellation.Cancel();
            }
        };

        var result = await _service.ReadAsync(new Uri(PageAddress), _headers, cancellation.Token);

        Assert.That(result.Error, Is.EqualTo(DownloadErrors.Cancelled));
        Assert.That(_web.Asked, Has.Count.EqualTo(cancelledAt));
    }

    private static string PageWith(string script)
    {
        return $"<html><script>SwaggerUIBundle({{ {script} }})</script></html>";
    }

    private Task<Result<RemoteImport>> ReadAsync(string address)
    {
        return _service.ReadAsync(new Uri(address), _headers, CancellationToken.None);
    }

    private void AssertFound(Result<RemoteImport> result, string address)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.Message);
        Assert.That(result.Value, Is.EqualTo(new FoundSpecification(_document, new Uri(address))));
    }
}
