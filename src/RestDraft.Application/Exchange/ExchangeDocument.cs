using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

// What a file holds: one collection, and what was left out or changed while reading it. BaseUrl is null when the request
// URLs are complete. A format whose request URLs are only paths sets it to the server address that the file declares,
// which is empty when the file declares none.
public sealed record ExchangeDocument(Collection Collection, IReadOnlyList<ImportWarning> Warnings, string? BaseUrl = null)
{
    // The same document with the address in front of every request URL. The ids stay, so ticked items stay ticked.
    public ExchangeDocument WithBaseUrl(string baseUrl)
    {
        var prefix = baseUrl.Trim().TrimEnd('/');
        var root = Prefix(Collection.Root, prefix);
        return this with { Collection = Collection with { Root = root }, BaseUrl = null };
    }

    private static Folder Prefix(Folder folder, string prefix)
    {
        return folder with
        {
            Folders = folder.Folders.Select(child => Prefix(child, prefix)).ToList(),
            Requests = folder.Requests.Select(request => request with { Url = prefix + request.Url }).ToList(),
        };
    }
}
