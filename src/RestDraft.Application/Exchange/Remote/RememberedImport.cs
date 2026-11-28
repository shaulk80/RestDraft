namespace RestDraft.Application.Exchange.Remote;

// The address of the last import from an address, and the base URL used with it. Both are empty before the first one.
public sealed record RememberedImport(string Address, string BaseUrl)
{
    // The base URL to offer again. Null for another address, or when none was used.
    public string? BaseUrlFor(Uri address)
    {
        return BaseUrl.Length > 0 && Address == WebAddress.WithoutUserInfo(address) ? BaseUrl : null;
    }
}
