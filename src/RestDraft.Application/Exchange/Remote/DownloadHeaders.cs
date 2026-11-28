using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange.Remote;

// The headers typed for a download. Only the host of the typed address (same scheme, host and port) gets them, so a
// link or a redirect to another server never receives a token. The values are sent as they are.
public sealed record DownloadHeaders(IReadOnlyList<KeyValueEntry> Entries, Uri AllowedAddress)
{
    // The headers to send with a request to the address.
    public IReadOnlyList<KeyValueEntry> For(Uri address)
    {
        if (!WebAddress.IsSameHost(address, AllowedAddress))
        {
            return [];
        }

        return Entries.Where(entry => entry.IsEnabled && !string.IsNullOrWhiteSpace(entry.Key)).ToList();
    }
}
