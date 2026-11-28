using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Application.Settings;
using RestDraft.Domain.Results;

namespace RestDraft.Infrastructure.Http;

// Downloads a specification or a page that leads to one, with the timeout, redirect, proxy and SSL settings of the app.
// Redirects are followed here and not by the HTTP handler, so the typed headers are decided again for every address.
public sealed class HttpSpecificationDownloader : ISpecificationDownloader
{
    private const int ChunkSizeBytes = 80 * 1024;

    // No header value and no query of an address goes into the log.
    private static readonly Action<ILogger, string, string, Exception?> DownloadFailed = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(40, nameof(DownloadFailed)),
        "A download from {Address} failed: {Reason}");

    private static readonly Action<ILogger, string, string, Exception?> DownloadFailedUnexpectedly = LoggerMessage.Define<string, string>(
        LogLevel.Error,
        new EventId(41, nameof(DownloadFailedUnexpectedly)),
        "A download from {Address} failed unexpectedly: {Reason}");

    private readonly ISettingsService _settings;
    private readonly ILogger<HttpSpecificationDownloader> _logger;

    public HttpSpecificationDownloader(ISettingsService settings, ILogger<HttpSpecificationDownloader> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<DownloadedText>> DownloadAsync(Uri address, DownloadHeaders headers, CancellationToken cancellationToken)
    {
        // The settings are cached after the start. Without the token, a cancellation ends in the catch below like any other.
        var loaded = await _settings.GetAsync(CancellationToken.None);
        if (!loaded.IsSuccess)
        {
            return Result<DownloadedText>.Failure(loaded.Error!);
        }

        var settings = loaded.Value!;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(settings.Timeout);

        var transport = HttpTransportOptions.From(settings) with { FollowRedirects = false };
        using var client = RequestHttpClientFactory.Create(transport, new ConnectTimingRecorder());
        try
        {
            return await FollowAsync(client, address, headers, settings, deadline.Token);
        }
        catch (OperationCanceledException exception)
        {
            return cancellationToken.IsCancellationRequested
                ? Result<DownloadedText>.Failure(DownloadErrors.Cancelled)
                : Fail(address, new Error(ErrorCategory.Timeout, $"The download timed out after {settings.TimeoutSeconds} seconds."), exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            var details = exception.InnerException?.Message ?? exception.Message;
            return Fail(address, new Error(ErrorCategory.Network, "The server could not be reached.", details), exception);
        }
        catch (Exception exception)
        {
            return Fail(address, new Error(ErrorCategory.Unexpected, "The download failed unexpectedly.", exception.Message), exception);
        }
    }

    private async Task<Result<DownloadedText>> FollowAsync(
        HttpClient client,
        Uri address,
        DownloadHeaders headers,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var current = address;
        for (var redirects = 0; ; redirects++)
        {
            using var request = CreateRequest(current, headers);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var target = settings.FollowRedirects ? RedirectTarget(response, current) : null;
            if (target is null)
            {
                return response.IsSuccessStatusCode
                    ? await ReadAsync(current, response, cancellationToken)
                    : Fail(address, StatusError(response), null);
            }

            if (redirects >= settings.MaxRedirects)
            {
                var message = $"The server redirected more than {settings.MaxRedirects} times.";
                return Fail(address, new Error(ErrorCategory.Network, message), null);
            }

            current = target;
        }
    }

    private static HttpRequestMessage CreateRequest(Uri address, DownloadHeaders headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, address);
        foreach (var header in headers.For(address))
        {
            request.Headers.TryAddWithoutValidation(header.Key.Trim(), header.Value);
        }

        return request;
    }

    // Null when the response is not a redirect that can be followed. As in the HTTP handler of .NET, an HTTPS address
    // never leads to an HTTP one.
    private static Uri? RedirectTarget(HttpResponseMessage response, Uri current)
    {
        var isRedirect = response.StatusCode is HttpStatusCode.MultipleChoices or HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
        if (!isRedirect || response.Headers.Location is not { } location)
        {
            return null;
        }

        if (!Uri.TryCreate(current, location, out var target) || !WebAddress.IsWeb(target))
        {
            return null;
        }

        var isDowngrade = current.Scheme == Uri.UriSchemeHttps && target.Scheme == Uri.UriSchemeHttp;
        return isDowngrade ? null : target;
    }

    private async Task<Result<DownloadedText>> ReadAsync(Uri address, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > ExchangeLimits.MaxFileBytes)
        {
            return Fail(address, TooLarge(), null);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[ChunkSizeBytes];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > ExchangeLimits.MaxFileBytes)
            {
                return Fail(address, TooLarge(), null);
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        using var text = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return Result<DownloadedText>.Success(new DownloadedText(address, await text.ReadToEndAsync(cancellationToken)));
    }

    private static Error StatusError(HttpResponseMessage response)
    {
        var message = $"The server answered {(int)response.StatusCode} ({response.ReasonPhrase}).";
        return new Error(ErrorCategory.Network, message);
    }

    private static Error TooLarge()
    {
        return ResultErrors.Validation($"The download is larger than {ExchangeLimits.MaxFileBytes / (1024 * 1024)} MB.");
    }

    private Result<DownloadedText> Fail(Uri address, Error error, Exception? exception)
    {
        var log = error.Category == ErrorCategory.Unexpected ? DownloadFailedUnexpectedly : DownloadFailed;
        log(_logger, WebAddress.ForDisplay(address), error.Message, exception);
        return Result<DownloadedText>.Failure(error);
    }
}
