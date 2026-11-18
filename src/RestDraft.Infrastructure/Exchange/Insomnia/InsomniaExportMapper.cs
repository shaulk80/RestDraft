using RestDraft.Application.Abstractions;
using RestDraft.Domain.Auth;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;
using RestDraft.Infrastructure.Exchange.Insomnia.Dto;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Turns a RestDraft collection into an Insomnia v5 one. What Insomnia needs and RestDraft has no value for gets the
// default that Insomnia itself writes. Folders come before requests, and the sort keys keep that order.
internal sealed class InsomniaExportMapper
{
    public const string FileType = "collection.insomnia.rest/5.0";
    public const string SchemaVersion = "5.1";

    private const string DefaultRawContentType = "text/plain";
    private const string FormMimeType = "application/x-www-form-urlencoded";
    private const string MultipartMimeType = "multipart/form-data";
    private const string FileParameterType = "file";

    private readonly IIdGenerator _ids;
    private readonly long _now;
    private long _nextSortKey;

    public InsomniaExportMapper(IIdGenerator ids, IClock clock)
    {
        _ids = ids;
        _now = clock.UtcNow.ToUnixTimeMilliseconds();
        _nextSortKey = -_now;
    }

    public InsomniaCollectionDto Map(Collection collection)
    {
        return new InsomniaCollectionDto
        {
            Type = FileType,
            SchemaVersion = SchemaVersion,
            Name = collection.Name,
            Meta = new InsomniaMetaDto { Id = NewId("wrk"), Created = _now, Modified = _now, Description = string.Empty },
            Collection = MapChildren(collection.Root),
        };
    }

    private List<InsomniaItemDto> MapChildren(Folder folder)
    {
        return
        [
            .. folder.Folders.Select(MapFolder),
            .. folder.Requests.Select(MapRequest),
        ];
    }

    private InsomniaItemDto MapFolder(Folder folder)
    {
        return new InsomniaItemDto
        {
            Name = folder.Name,
            Meta = NewMeta("fld", withPrivateFlag: false),
            Children = MapChildren(folder),
        };
    }

    private InsomniaItemDto MapRequest(RequestDefinition request)
    {
        return new InsomniaItemDto
        {
            Url = Text(request.Url),
            Name = request.Name,
            Meta = NewMeta("req", withPrivateFlag: true),
            Method = request.Method.ToString().ToUpperInvariant(),
            Body = MapBody(request.Body),
            Parameters = MapEntries(request.QueryParams),
            Headers = MapEntries(request.Headers),
            Authentication = MapAuth(request.Auth),
            Settings = InsomniaSettingsDto.Default(),
        };
    }

    private InsomniaMetaDto NewMeta(string idPrefix, bool withPrivateFlag)
    {
        return new InsomniaMetaDto
        {
            Id = NewId(idPrefix),
            Created = _now,
            Modified = _now,
            IsPrivate = withPrivateFlag ? false : null,
            Description = string.Empty,
            SortKey = _nextSortKey++,
        };
    }

    // Insomnia ids are a prefix and 32 hex digits.
    private string NewId(string prefix)
    {
        return $"{prefix}_{_ids.NewId():N}";
    }

    private static string Text(string text)
    {
        return InsomniaVariableSyntax.ToInsomnia(text);
    }

    // Insomnia leaves an empty list out of the file, so an empty one is not written.
    private static List<InsomniaParameterDto>? MapEntries(IReadOnlyList<KeyValueEntry> entries)
    {
        if (entries.Count == 0)
        {
            return null;
        }

        return entries
            .Select(entry => new InsomniaParameterDto
            {
                Name = Text(entry.Key),
                Value = Text(entry.Value),
                Disabled = entry.IsEnabled ? null : true,
            })
            .ToList();
    }

    private static InsomniaBodyDto? MapBody(BodyDefinition body)
    {
        return body switch
        {
            RawBody raw => new InsomniaBodyDto
            {
                MimeType = string.IsNullOrWhiteSpace(raw.ContentType) ? DefaultRawContentType : raw.ContentType,
                Text = Text(raw.Text),
            },
            FormUrlEncodedBody form => new InsomniaBodyDto { MimeType = FormMimeType, Params = MapEntries(form.Fields) ?? [] },
            MultipartBody multipart => new InsomniaBodyDto
            {
                MimeType = MultipartMimeType,
                Params = multipart.Parts.Select(MapPart).ToList(),
            },
            _ => null,
        };
    }

    private static InsomniaParameterDto MapPart(MultipartPart part)
    {
        if (part.FilePath is not null)
        {
            return new InsomniaParameterDto { Name = Text(part.Name), Type = FileParameterType, FileName = part.FilePath };
        }

        return new InsomniaParameterDto { Name = Text(part.Name), Value = Text(part.Value ?? string.Empty) };
    }

    private static InsomniaAuthDto? MapAuth(AuthDefinition auth)
    {
        return auth switch
        {
            BasicAuth basic => new InsomniaAuthDto { Type = "basic", Username = Text(basic.Username), Password = Text(basic.Password) },
            BearerAuth bearer => new InsomniaAuthDto { Type = "bearer", Token = Text(bearer.Token) },
            ApiKeyAuth apiKey => new InsomniaAuthDto
            {
                Type = "apikey",
                Key = Text(apiKey.Key),
                Value = Text(apiKey.Value),
                AddTo = apiKey.Location == ApiKeyLocation.Query ? "queryParams" : "header",
            },
            _ => null,
        };
    }
}
