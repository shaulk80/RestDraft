using Microsoft.OpenApi;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Turns a specification into a collection: one folder per tag and one request per operation. The request URLs are the
// paths; the server address is kept apart, because the user may change it before the import. One mapper maps one document.
internal sealed class OpenApiImportMapper
{
    private const string DefaultCollectionName = "Imported collection";
    private const string DescriptionsDropped = "Descriptions are not imported.";

    // How the reader words a property that it skips.
    private const string UnknownPropertyError = " is not a valid property";

    private readonly IIdGenerator _ids;
    private readonly ImportWarningList _warnings = new();
    private readonly OpenApiReferences _references;

    public OpenApiImportMapper(IIdGenerator ids)
    {
        _ids = ids;
        _references = new OpenApiReferences(_warnings);
    }

    public ExchangeDocument Map(OpenApiDocument document, IList<OpenApiError> errors)
    {
        ReportErrors(errors);
        ReportDocumentParts(document);
        var serverAddress = ReadServerAddress(document.Servers);
        var folders = MapOperations(document);
        var title = document.Info?.Title;
        var name = string.IsNullOrWhiteSpace(title) ? DefaultCollectionName : title.Trim();
        var collection = new Collection(_ids.NewId(), name, folders.Build(_ids));
        return new ExchangeDocument(collection, _warnings.ToList(), serverAddress);
    }

    // Errors in response definitions do not matter, because those are not imported. A property that the reader skipped
    // is told apart: a large specification can have thousands of them and still be imported completely.
    private void ReportErrors(IList<OpenApiError> errors)
    {
        var relevant = errors
            .Where(error => !(error.Pointer ?? string.Empty).Contains("/responses", StringComparison.Ordinal))
            .ToLookup(error => error.Message.Contains(UnknownPropertyError, StringComparison.Ordinal));
        ReportFirst(relevant[false].ToList(), "1 error", "errors", "so something may be missing");
        ReportFirst(relevant[true].ToList(), "1 property that the reader skipped", "properties that the reader skipped", "as unknown");
    }

    private void ReportFirst(List<OpenApiError> errors, string one, string many, string consequence)
    {
        if (errors.Count == 0)
        {
            return;
        }

        var count = errors.Count == 1 ? one : $"{errors.Count} {many}";
        _warnings.Add($"The specification has {count}, {consequence}. The first one: {errors[0].Message}", errors[0].Pointer);
    }

    private void ReportDocumentParts(OpenApiDocument document)
    {
        if (!string.IsNullOrEmpty(document.Info?.Description))
        {
            _warnings.AddOnce(DescriptionsDropped);
        }

        if (document.Webhooks is { Count: > 0 })
        {
            _warnings.AddOnce("Webhooks are not imported.");
        }
    }

    private string ReadServerAddress(IList<OpenApiServer>? servers)
    {
        if (servers is { Count: > 1 })
        {
            _warnings.Add("The specification has several server addresses. The first one is used.");
        }

        return OpenApiServerAddress.Read(servers);
    }

    private OpenApiTagFolders MapOperations(OpenApiDocument document)
    {
        var requests = new OpenApiRequestMapper(
            _references,
            _warnings,
            new OpenApiBodyMapper(_references, _warnings),
            new OpenApiAuthMapper(document.Security, _references, _warnings));
        var declaredTags = (document.Tags ?? new HashSet<OpenApiTag>()).Select(tag => tag.Name ?? string.Empty);
        var folders = new OpenApiTagFolders(declaredTags);
        foreach (var path in document.Paths ?? [])
        {
            var pathItem = _references.Follow(path.Value);
            foreach (var operation in pathItem?.Operations ?? [])
            {
                var source = new OpenApiOperationSource(path.Key, pathItem!, operation.Value, $"{operation.Key.Method} {path.Key}");
                MapOperation(source, operation.Key, requests, folders);
            }
        }

        return folders;
    }

    private void MapOperation(OpenApiOperationSource source, HttpMethod httpMethod, OpenApiRequestMapper requests, OpenApiTagFolders folders)
    {
        if (!Enum.TryParse<RequestMethod>(httpMethod.Method, ignoreCase: true, out var method) || !Enum.IsDefined(method))
        {
            _warnings.Add($"The method '{httpMethod.Method}' is not supported, so the request was skipped.", source.Label);
            return;
        }

        ReportOperationParts(source);
        var request = requests.Map(source, _ids.NewId(), RequestName(source), method);
        folders.Add(source.Operation.Tags?.FirstOrDefault()?.Name, request);
    }

    private void ReportOperationParts(OpenApiOperationSource source)
    {
        var operation = source.Operation;
        if (!string.IsNullOrEmpty(operation.Description))
        {
            _warnings.AddOnce(DescriptionsDropped);
        }

        if (operation.Responses is { Count: > 0 })
        {
            _warnings.AddOnce("Response definitions are not imported.");
        }

        if (operation.Callbacks is { Count: > 0 })
        {
            _warnings.AddOnce("Callbacks are not imported.");
        }

        if (operation.Servers is { Count: > 0 } || source.PathItem.Servers is { Count: > 0 })
        {
            _warnings.AddOnce("Server addresses of single paths or operations are not used.");
        }
    }

    // A summary can run over several lines, and a name cannot.
    private static string RequestName(OpenApiOperationSource source)
    {
        var summary = OneLine(source.Operation.Summary);
        if (summary.Length > 0)
        {
            return summary;
        }

        var operationId = OneLine(source.Operation.OperationId);
        return operationId.Length > 0 ? operationId : source.Label;
    }

    private static string OneLine(string? text)
    {
        return string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
