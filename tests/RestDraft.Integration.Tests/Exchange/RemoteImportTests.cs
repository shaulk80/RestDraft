using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Application.Settings;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.Infrastructure.Exchange.OpenApi;
using RestDraft.Infrastructure.Http;
using RestDraft.Infrastructure.Services;

namespace RestDraft.Integration.Tests.Exchange;

// From the address of a Swagger UI page to the document, over real connections.
[TestFixture]
public class RemoteImportTests
{
    private TestSwaggerServer _server = null!;
    private RemoteImportService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _server = new TestSwaggerServer();
        await _server.StartAsync();
        var downloader = new HttpSpecificationDownloader(
            new MutableSettingsService(new AppSettings()),
            NullLogger<HttpSpecificationDownloader>.Instance);
        var format = new OpenApiFormat(new IdGenerator(), NullLogger<OpenApiFormat>.Instance);
        _service = new RemoteImportService(downloader, new CollectionTextReader([format]));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _server.DisposeAsync();
    }

    [TestCase("/swagger")]
    [TestCase("/swagger/index.html")]
    [TestCase("/swagger/v1/swagger.json")]
    public async Task TheSpecificationIsFoundFromTheAddress(string path)
    {
        var result = await ReadAsync(path, TestSwaggerServer.Token);

        var found = result.Value as FoundSpecification;
        Assert.That(found?.Address, Is.EqualTo(new Uri(_server.BaseAddress, "/swagger/v1/swagger.json")), result.Error?.Message);
        Assert.That(found?.Document.Collection.Name, Is.EqualTo("Shop"));
        Assert.That(
            BaseUrlResolver.Resolve(found!.Document.BaseUrl!, found.Address),
            Is.EqualTo(_server.BaseAddress.GetLeftPart(UriPartial.Authority)));
    }

    [Test]
    public async Task AWrongTokenIsRefusedByTheServer()
    {
        var result = await ReadAsync("/swagger", "wrong");

        Assert.That(result.Error?.Message, Is.EqualTo("The server answered 401 (Unauthorized)."));
    }

    private Task<Result<RemoteImport>> ReadAsync(string path, string token)
    {
        var address = new Uri(_server.BaseAddress, path);
        var headers = new DownloadHeaders([new KeyValueEntry(TestSwaggerServer.TokenHeader, token, true)], address);
        return _service.ReadAsync(address, headers, CancellationToken.None);
    }
}
