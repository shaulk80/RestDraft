using RestDraft.Domain.Auth;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;
using RestDraft.Infrastructure.Exchange.Insomnia.Dto;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Turns one Insomnia request into a RestDraft request. What cannot be kept is left out and reported in the log.
internal sealed class InsomniaRequestReader
{
    private const string FormMimeType = "application/x-www-form-urlencoded";
    private const string MultipartMimeType = "multipart/form-data";
    private const string FileMimeType = "application/octet-stream";
    private const string GraphQlMimeType = "application/graphql";
    private const string JsonMimeType = "application/json";
    private const string FileParameterType = "file";
    private const string QueryAddTo = "queryParams";

    private readonly ImportWarningLog _log;

    public InsomniaRequestReader(ImportWarningLog log)
    {
        _log = log;
    }

    // Null when the request cannot be kept at all, and the log says why. The id and the name are the caller's.
    public RequestDefinition? Read(InsomniaItemDto item, Guid id, string name, string path)
    {
        if (!TryParseMethod(item.Method, out var method))
        {
            _log.Add($"The method '{item.Method}' is not supported, so the request was skipped.", path);
            return null;
        }

        CountDropped(item);
        var text = new InsomniaTextConverter();
        var request = new RequestDefinition
        {
            Id = id,
            Name = name,
            Method = method,
            Url = text.Convert(item.Url),
            QueryParams = ReadEntries(item.Parameters, text),
            Headers = ReadEntries(item.Headers, text),
            Body = ReadBody(item.Body, text, path),
            Auth = ReadAuth(item.Authentication, text, path),
        };
        if (text.HasUnconvertible)
        {
            _log.Add("Insomnia tags or expressions cannot be converted. They are kept as text.", path);
        }

        return request;
    }

    private static bool TryParseMethod(string? value, out RequestMethod method)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            method = RequestMethod.Get;
            return true;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out method) && Enum.IsDefined(method);
    }

    private void CountDropped(InsomniaItemDto item)
    {
        if (item.Settings is { } settings && !settings.IsDefault())
        {
            _log.RequestsWithSettings++;
        }

        if (item.Scripts is not null)
        {
            _log.ItemsWithScripts++;
        }

        if (!string.IsNullOrEmpty(item.Meta?.Description))
        {
            _log.ItemsWithDescription++;
        }
    }

    private static List<KeyValueEntry> ReadEntries(List<InsomniaParameterDto>? parameters, InsomniaTextConverter text)
    {
        return (parameters ?? [])
            .Select(parameter => new KeyValueEntry(
                text.Convert(parameter.Name),
                text.Convert(parameter.Value),
                parameter.Disabled != true))
            .ToList();
    }

    private BodyDefinition ReadBody(InsomniaBodyDto? body, InsomniaTextConverter text, string path)
    {
        if (body is null)
        {
            return new NoBody();
        }

        var mimeType = body.MimeType?.Trim() ?? string.Empty;
        if (string.Equals(mimeType, FormMimeType, StringComparison.OrdinalIgnoreCase))
        {
            return new FormUrlEncodedBody(ReadEntries(body.Params, text));
        }

        if (string.Equals(mimeType, MultipartMimeType, StringComparison.OrdinalIgnoreCase))
        {
            return ReadMultipart(body, text, path);
        }

        return ReadRaw(body, mimeType, text, path);
    }

    private MultipartBody ReadMultipart(InsomniaBodyDto body, InsomniaTextConverter text, string path)
    {
        var parameters = body.Params ?? [];
        if (parameters.Any(parameter => parameter.Disabled == true))
        {
            _log.Add("Disabled multipart fields cannot be kept and were left out.", path);
        }

        var parts = parameters
            .Where(parameter => parameter.Disabled != true)
            .Select(parameter => ReadPart(parameter, text))
            .ToList();
        return new MultipartBody(parts);
    }

    private static MultipartPart ReadPart(InsomniaParameterDto parameter, InsomniaTextConverter text)
    {
        var name = text.Convert(parameter.Name);
        var isFile = string.Equals(parameter.Type, FileParameterType, StringComparison.OrdinalIgnoreCase);
        return isFile
            ? new MultipartPart(name, null, parameter.FileName)
            : new MultipartPart(name, text.Convert(parameter.Value), null);
    }

    private BodyDefinition ReadRaw(InsomniaBodyDto body, string mimeType, InsomniaTextConverter text, string path)
    {
        if (string.Equals(mimeType, FileMimeType, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(body.Text))
        {
            if (!string.IsNullOrEmpty(body.FileName))
            {
                _log.Add("A body that is a file is not supported. The request has no body.", path);
            }

            return new NoBody();
        }

        if (string.IsNullOrEmpty(body.Text))
        {
            return new NoBody();
        }

        var contentType = string.Equals(mimeType, GraphQlMimeType, StringComparison.OrdinalIgnoreCase) ? JsonMimeType : mimeType;
        return new RawBody(contentType, text.Convert(body.Text));
    }

    private AuthDefinition ReadAuth(InsomniaAuthDto? auth, InsomniaTextConverter text, string path)
    {
        if (auth is null || auth.Disabled == true || string.IsNullOrWhiteSpace(auth.Type))
        {
            return new NoAuth();
        }

        switch (auth.Type.Trim().ToLowerInvariant())
        {
            case "none":
                return new NoAuth();
            case "basic":
                return new BasicAuth(text.Convert(auth.Username), text.Convert(auth.Password));
            case "bearer":
                return new BearerAuth(text.Convert(auth.Token));
            case "apikey":
                var location = string.Equals(auth.AddTo, QueryAddTo, StringComparison.OrdinalIgnoreCase)
                    ? ApiKeyLocation.Query
                    : ApiKeyLocation.Header;
                return new ApiKeyAuth(text.Convert(auth.Key), text.Convert(auth.Value), location);
            default:
                _log.Add($"The authentication '{auth.Type}' is not supported. The request has no authentication.", path);
                return new NoAuth();
        }
    }
}
