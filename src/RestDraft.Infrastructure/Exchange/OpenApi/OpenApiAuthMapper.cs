using Microsoft.OpenApi;
using RestDraft.Domain.Auth;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Turns the security of an operation into the authentication of a request. Credentials are variables for the user to fill.
internal sealed class OpenApiAuthMapper
{
    private const string UsernameVariable = "{{username}}";
    private const string PasswordVariable = "{{password}}";
    private const string TokenVariable = "{{token}}";
    private const string ApiKeyVariable = "{{apiKey}}";

    private readonly IList<OpenApiSecurityRequirement>? _documentSecurity;
    private readonly OpenApiReferences _references;
    private readonly ImportWarningList _warnings;

    public OpenApiAuthMapper(
        IList<OpenApiSecurityRequirement>? documentSecurity,
        OpenApiReferences references,
        ImportWarningList warnings)
    {
        _documentSecurity = documentSecurity;
        _references = references;
        _warnings = warnings;
    }

    // The security of the operation replaces that of the document, also when it is an empty list. Of several
    // alternatives the first one that names a scheme is used.
    public AuthDefinition Map(OpenApiOperation operation)
    {
        var requirement = (operation.Security ?? _documentSecurity)?.FirstOrDefault(candidate => candidate is { Count: > 0 });
        if (requirement is null)
        {
            return new NoAuth();
        }

        if (requirement.Count > 1)
        {
            _warnings.AddOnce("Some requests need several authentication schemes at once. Only the first one is set.");
        }

        var reference = requirement.Keys.First();
        var scheme = _references.Follow<IOpenApiSecurityScheme>(reference);
        return scheme is null ? new NoAuth() : MapScheme(scheme, reference.Reference.Id ?? string.Empty);
    }

    private AuthDefinition MapScheme(IOpenApiSecurityScheme scheme, string name)
    {
        switch (scheme.Type)
        {
            case SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase):
                return new BasicAuth(UsernameVariable, PasswordVariable);
            case SecuritySchemeType.Http when string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase):
                return new BearerAuth(TokenVariable);
            case SecuritySchemeType.ApiKey:
                return MapApiKey(scheme);
            case SecuritySchemeType.OAuth2:
            case SecuritySchemeType.OpenIdConnect:
                _warnings.AddOnce($"OAuth 2 and OpenID Connect are set as a bearer token, {TokenVariable}.");
                return new BearerAuth(TokenVariable);
            default:
                _warnings.AddOnce($"The authentication scheme '{name}' is not supported. Its requests have no authentication.");
                return new NoAuth();
        }
    }

    private AuthDefinition MapApiKey(IOpenApiSecurityScheme scheme)
    {
        switch (scheme.In)
        {
            case ParameterLocation.Header:
                return new ApiKeyAuth(scheme.Name ?? string.Empty, ApiKeyVariable, ApiKeyLocation.Header);
            case ParameterLocation.Query:
                return new ApiKeyAuth(scheme.Name ?? string.Empty, ApiKeyVariable, ApiKeyLocation.Query);
            default:
                _warnings.AddOnce("API keys in cookies are not supported. Their requests have no authentication.");
                return new NoAuth();
        }
    }
}
