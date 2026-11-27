using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using RestDraft.Domain.Collections;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Turns one operation into a request: the URL and the parameters here, the body and the authentication by their mappers.
internal sealed partial class OpenApiRequestMapper
{
    private readonly OpenApiReferences _references;
    private readonly ImportWarningList _warnings;
    private readonly OpenApiBodyMapper _bodies;
    private readonly OpenApiAuthMapper _authentication;
    private readonly OpenApiParameterValue _values;

    public OpenApiRequestMapper(
        OpenApiReferences references,
        ImportWarningList warnings,
        OpenApiBodyMapper bodies,
        OpenApiAuthMapper authentication)
    {
        _references = references;
        _warnings = warnings;
        _bodies = bodies;
        _authentication = authentication;
        _values = new OpenApiParameterValue(references);
    }

    public RequestDefinition Map(OpenApiOperationSource source, Guid id, string name, RequestMethod method)
    {
        var parameters = ReadParameters(source);
        return new RequestDefinition
        {
            Id = id,
            Name = name,
            Method = method,
            Url = BuildUrl(source.Path, parameters),
            QueryParams = parameters.Where(parameter => parameter.In == ParameterLocation.Query).SelectMany(QueryEntries).ToList(),
            Headers = parameters.Where(parameter => parameter.In == ParameterLocation.Header).Select(HeaderEntry).ToList(),
            Body = _bodies.Map(source.Operation.RequestBody, source.Label),
            Auth = _authentication.Map(source.Operation),
        };
    }

    // The parameters of the path and of the operation. One of the operation replaces one of the path with the same name
    // in the same place.
    private List<IOpenApiParameter> ReadParameters(OpenApiOperationSource source)
    {
        var parameters = (source.PathItem.Parameters ?? [])
            .Concat(source.Operation.Parameters ?? [])
            .Select(_references.Follow)
            .OfType<IOpenApiParameter>()
            .Where(parameter => !string.IsNullOrEmpty(parameter.Name))
            .GroupBy(parameter => new { parameter.In, parameter.Name })
            .Select(sameParameter => sameParameter.Last())
            .ToList();
        ReportDropped(parameters);
        return parameters;
    }

    private void ReportDropped(List<IOpenApiParameter> parameters)
    {
        if (parameters.Any(parameter => parameter.In == ParameterLocation.Cookie))
        {
            _warnings.AddOnce("Cookie parameters are not imported.");
        }

        if (parameters.Any(parameter => parameter.In == ParameterLocation.QueryString))
        {
            _warnings.AddOnce("Parameters that describe the whole query string are not imported.");
        }

        if (parameters.Any(parameter => !string.IsNullOrEmpty(parameter.Description)))
        {
            _warnings.AddOnce("Descriptions are not imported.");
        }
    }

    private string BuildUrl(string path, List<IOpenApiParameter> parameters)
    {
        var url = PathVariable().Replace(path, match => PathValue(match.Groups[1].Value, parameters));
        return url.StartsWith('/') ? url : "/" + url;
    }

    private string PathValue(string name, List<IOpenApiParameter> parameters)
    {
        var parameter = parameters.FirstOrDefault(candidate => candidate.In == ParameterLocation.Path && candidate.Name == name);
        var value = parameter is null ? string.Empty : string.Join(',', _values.Read(parameter));
        return value.Length > 0 ? Uri.EscapeDataString(value) : "{{" + name + "}}";
    }

    // Required parameters are switched on. A list is sent as one entry per item when the specification says so.
    private IEnumerable<KeyValueEntry> QueryEntries(IOpenApiParameter parameter)
    {
        var values = _values.Read(parameter);
        if (values.Count > 1 && parameter.Explode)
        {
            return values.Select(value => new KeyValueEntry(parameter.Name!, value, parameter.Required));
        }

        var delimiter = parameter.Style switch
        {
            ParameterStyle.SpaceDelimited => " ",
            ParameterStyle.PipeDelimited => "|",
            _ => ",",
        };
        return [new KeyValueEntry(parameter.Name!, string.Join(delimiter, values), parameter.Required)];
    }

    private KeyValueEntry HeaderEntry(IOpenApiParameter parameter)
    {
        return new KeyValueEntry(parameter.Name!, string.Join(',', _values.Read(parameter)), parameter.Required);
    }

    [GeneratedRegex(@"\{([^{}/]+)\}")]
    private static partial Regex PathVariable();
}
