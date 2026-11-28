using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Application.Settings;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.Infrastructure.Http;

namespace RestDraft.Integration.Tests.Http;

[TestFixture]
public class HttpSpecificationDownloaderTests
{
    private TestHttpServer _server = null!;
    private TestHttpServer _otherServer = null!;
    private TestHttpsServer _secureServer = null!;
    private MutableSettingsService _settings = null!;
    private HttpSpecificationDownloader _downloader = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _server = new TestHttpServer();
        await _server.StartAsync();
        _otherServer = new TestHttpServer();
        await _otherServer.StartAsync();
        _secureServer = new TestHttpsServer();
        await _secureServer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _server.DisposeAsync();
        await _otherServer.DisposeAsync();
        await _secureServer.DisposeAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _settings = new MutableSettingsService(new AppSettings());
        _downloader = new HttpSpecificationDownloader(_settings, NullLogger<HttpSpecificationDownloader>.Instance);
    }

    [TestCase("/ok", "hello")]
    [TestCase("/token", "secret")]
    [TestCase("/marked", "ok")]
    public async Task TheTextOfTheAddressIsDownloaded(string path, string expected)
    {
        var result = await DownloadAsync(At(_server, path));

        Assert.That(result.Value?.Text, Is.EqualTo(expected), result.Error?.Message);
        Assert.That(result.Value?.Address, Is.EqualTo(At(_server, path)));
    }

    [Test]
    public async Task ARedirectToTheSameHostKeepsTheHeadersAndGivesTheNewAddress()
    {
        var result = await DownloadAsync(At(_server, "/go?to=/token"));

        Assert.That(result.Value?.Text, Is.EqualTo("secret"));
        Assert.That(result.Value?.Address, Is.EqualTo(At(_server, "/token")));
    }

    [Test]
    public async Task ARedirectToAnotherHostGetsNoHeaders()
    {
        var target = At(_otherServer, "/token");

        var result = await DownloadAsync(At(_server, $"/go?to={Uri.EscapeDataString(target.AbsoluteUri)}"));

        Assert.That(result.Value?.Address, Is.EqualTo(target));
        Assert.That(result.Value?.Text, Is.Empty);
    }

    [TestCase(false, 10, "/redirect", "302")]
    [TestCase(true, 2, "/redirect/5", "redirected more than 2 times")]
    public async Task RedirectsFollowTheSettings(bool follow, int limit, string path, string expected)
    {
        _settings.Current = new AppSettings { FollowRedirects = follow, MaxRedirects = limit };

        var result = await DownloadAsync(At(_server, path));

        Assert.That(result.Error?.Message, Does.Contain(expected));
    }

    [Test]
    public async Task AChainWithinTheRedirectLimitIsFollowed()
    {
        _settings.Current = new AppSettings { MaxRedirects = 3 };

        var result = await DownloadAsync(At(_server, "/redirect/3"));

        Assert.That(result.Value?.Text, Is.EqualTo("hello"));
    }

    [TestCase(401, "The server answered 401 (Unauthorized).")]
    [TestCase(404, "The server answered 404 (Not Found).")]
    [TestCase(500, "The server answered 500 (Internal Server Error).")]
    public async Task AStatusOtherThanSuccessIsAnError(int status, string expected)
    {
        var result = await DownloadAsync(At(_server, $"/status/{status}"));

        Assert.That(result.Error, Is.EqualTo(new Error(ErrorCategory.Network, expected)));
    }

    [TestCase("bytes")]
    [TestCase("stream")]
    public async Task ADownloadOverTheLimitIsRefused(string path)
    {
        var result = await DownloadAsync(At(_server, $"/{path}/{ExchangeLimits.MaxFileBytes + 1}"));

        Assert.That(result.Error?.Message, Is.EqualTo("The download is larger than 20 MB."));
    }

    [TestCase("bytes")]
    [TestCase("stream")]
    public async Task ADownloadAtTheLimitIsAccepted(string path)
    {
        var result = await DownloadAsync(At(_server, $"/{path}/{ExchangeLimits.MaxFileBytes}"));

        Assert.That(result.Value?.Text, Has.Length.EqualTo(ExchangeLimits.MaxFileBytes), result.Error?.Message);
    }

    [Test]
    public async Task ASlowServerTimesOut()
    {
        _settings.Current = new AppSettings { TimeoutSeconds = 1 };

        var result = await DownloadAsync(At(_server, "/slow"));

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.Timeout));
    }

    [TestCase(0)]
    [TestCase(100)]
    public async Task ACancelledDownloadSaysSo(int afterMilliseconds)
    {
        using var cancellation = new CancellationTokenSource();
        if (afterMilliseconds == 0)
        {
            await cancellation.CancelAsync();
        }
        else
        {
            cancellation.CancelAfter(afterMilliseconds);
        }

        var result = await DownloadAsync(At(_server, "/slow"), cancellation.Token);

        Assert.That(result.Error, Is.EqualTo(DownloadErrors.Cancelled));
    }

    [Test]
    public async Task ACertificateIsCheckedOnlyWhileVerificationIsOn()
    {
        var address = new Uri(_secureServer.BaseAddress, "/ok");

        var verified = await DownloadAsync(address);
        _settings.Current = new AppSettings { VerifySsl = false };
        var unverified = await DownloadAsync(address);

        Assert.That(verified.Error?.Category, Is.EqualTo(ErrorCategory.Network));
        Assert.That(verified.Error?.Details, Is.Not.Empty);
        Assert.That(unverified.Value?.Text, Is.EqualTo("secure"));
    }

    // The redirect is allowed by the settings, and still not followed to the unprotected address.
    [Test]
    public async Task ARedirectFromHttpsToHttpIsNotFollowed()
    {
        _settings.Current = new AppSettings { VerifySsl = false };
        var target = Uri.EscapeDataString(At(_server, "/ok").AbsoluteUri);

        var result = await DownloadAsync(new Uri(_secureServer.BaseAddress, $"/go?to={target}"));

        Assert.That(result.Error?.Message, Is.EqualTo("The server answered 302 (Found)."));
    }

    [Test]
    public async Task TheProxyOfTheSettingsIsUsed()
    {
        _settings.Current = new AppSettings { ProxyUrl = "http://127.0.0.1:1" };

        var result = await DownloadAsync(At(_server, "/ok"));

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.Network));
    }

    private static Uri At(TestHttpServer server, string path)
    {
        return new Uri(server.BaseAddress, path);
    }

    // The token is allowed for the first server only.
    private Task<Result<DownloadedText>> DownloadAsync(Uri address, CancellationToken cancellationToken = default)
    {
        var allowed = address.Scheme == Uri.UriSchemeHttps ? _secureServer.BaseAddress : _server.BaseAddress;
        var headers = new DownloadHeaders([new KeyValueEntry("X-Token", "secret", true)], allowed);
        return _downloader.DownloadAsync(address, headers, cancellationToken);
    }
}
