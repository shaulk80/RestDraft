using RestDraft.Domain.Auth;
using RestDraft.Domain.Collections;

namespace RestDraft.Application.Exchange;

public static class AuthSecrets
{
    // True when a request in the folder or below it has a password, token or key that an export would write out.
    public static bool HasCredentials(Folder folder)
    {
        return folder.Requests.Any(request => HasCredentials(request.Auth))
            || folder.Folders.Any(HasCredentials);
    }

    private static bool HasCredentials(AuthDefinition auth)
    {
        return auth switch
        {
            BasicAuth basic => basic.Password.Length > 0,
            BearerAuth bearer => bearer.Token.Length > 0,
            ApiKeyAuth apiKey => apiKey.Value.Length > 0,
            _ => false,
        };
    }
}
