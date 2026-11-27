namespace RestDraft.Application.Exchange;

// Turns the server address that a specification declares into the base URL that the import preview offers. The download
// address is the one the specification came from, or null for a file.
public static class BaseUrlResolver
{
    public const string BaseUrlVariable = "{{base_url}}";

    private const string SchemeSeparator = "://";
    private const string NetworkPathPrefix = "//";
    private const string CurrentFolderPrefix = "./";

    public static string Resolve(string declared, Uri? downloadAddress)
    {
        var address = declared.Trim();
        if (address.StartsWith("{{", StringComparison.Ordinal))
        {
            return WithoutEndSlash(address);
        }

        var separator = address.IndexOf(SchemeSeparator, StringComparison.Ordinal);
        if (separator >= 0)
        {
            var afterScheme = address[(separator + SchemeSeparator.Length)..];
            // A Swagger 2.0 file with schemes and no host gives "https:///path": only the path says something.
            return afterScheme.StartsWith('/') || afterScheme.Length == 0
                ? ResolveRelative(afterScheme, downloadAddress)
                : WithoutEndSlash(address);
        }

        if (address.StartsWith(NetworkPathPrefix, StringComparison.Ordinal))
        {
            var scheme = downloadAddress?.Scheme ?? Uri.UriSchemeHttps;
            return WithoutEndSlash($"{scheme}:{address}");
        }

        return ResolveRelative(address, downloadAddress);
    }

    private static string ResolveRelative(string path, Uri? downloadAddress)
    {
        if (downloadAddress is null)
        {
            var relative = path.StartsWith(CurrentFolderPrefix, StringComparison.Ordinal) ? path[CurrentFolderPrefix.Length..] : path;
            var trimmed = relative.Trim('/');
            return trimmed.Length == 0 ? BaseUrlVariable : $"{BaseUrlVariable}/{trimmed}";
        }

        // A user name or password in the download address does not belong in the requests.
        var origin = $"{downloadAddress.Scheme}{SchemeSeparator}{downloadAddress.Authority}";
        if (path.Length == 0)
        {
            return origin;
        }

        var document = new Uri(origin + downloadAddress.PathAndQuery);
        return Uri.TryCreate(document, path, out var resolved)
            ? WithoutEndSlash(resolved.ToString())
            : origin;
    }

    private static string WithoutEndSlash(string address)
    {
        return address.TrimEnd('/');
    }
}
