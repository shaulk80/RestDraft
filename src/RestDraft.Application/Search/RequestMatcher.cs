using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;

namespace RestDraft.Application.Search;

// The rules shared by the collections tree and the history: how a request, a name or a body matches a filter.
public static class RequestMatcher
{
    public static bool Matches(RequestDefinition request, RequestFilter filter, bool textMatchedByParent)
    {
        return MethodMatches(request, filter) && (textMatchedByParent || TextMatches(request, filter));
    }

    public static bool NameMatches(string name, RequestFilter filter)
    {
        return filter.Text.Length > 0 && Contains(name, filter.Text);
    }

    public static bool Contains(string? value, string text)
    {
        return value is not null && value.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    public static bool BodyContains(BodyDefinition body, string text)
    {
        return body switch
        {
            RawBody raw => Contains(raw.Text, text),
            FormUrlEncodedBody form => form.Fields.Any(field => Contains(field.Key, text) || Contains(field.Value, text)),
            MultipartBody multipart => multipart.Parts.Any(part => Contains(part.Name, text) || Contains(part.Value, text)),
            _ => false,
        };
    }

    private static bool MethodMatches(RequestDefinition request, RequestFilter filter)
    {
        return filter.Method is null || filter.Method == request.Method;
    }

    private static bool TextMatches(RequestDefinition request, RequestFilter filter)
    {
        if (filter.Text.Length == 0)
        {
            return true;
        }

        return Contains(request.Name, filter.Text)
            || Contains(request.Url, filter.Text)
            || (filter.InBodies && BodyContains(request.Body, filter.Text));
    }
}
