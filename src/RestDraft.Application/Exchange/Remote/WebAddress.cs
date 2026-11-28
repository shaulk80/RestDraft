using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

public static class WebAddress
{
    private const string SchemeSeparator = "://";

    // An address that the user typed. It has to say http or https itself.
    public static Result<Uri> Parse(string text)
    {
        var typed = text.Trim();
        var namesScheme = typed.StartsWith(Uri.UriSchemeHttp + SchemeSeparator, StringComparison.OrdinalIgnoreCase)
            || typed.StartsWith(Uri.UriSchemeHttps + SchemeSeparator, StringComparison.OrdinalIgnoreCase);
        if (namesScheme && Uri.TryCreate(typed, UriKind.Absolute, out var address))
        {
            return Result<Uri>.Success(address);
        }

        return Result<Uri>.Failure(ResultErrors.Validation("Enter an address that starts with http:// or https://."));
    }

    public static bool IsWeb(Uri address)
    {
        return address.IsAbsoluteUri && (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps);
    }

    // The same scheme, host and port.
    public static bool IsSameHost(Uri first, Uri second)
    {
        return Uri.Compare(first, second, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0;
    }

    // For messages and logs: without a user name, a password and a query, which may hold secrets.
    public static string ForDisplay(Uri address)
    {
        return address.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
    }

    public static string WithoutUserInfo(Uri address)
    {
        return address.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
    }
}
