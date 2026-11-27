using RestDraft.Application.Abstractions;
using RestDraft.Domain.Collections;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Sorts requests into one folder per tag; a request without a tag goes to the root. The folders are in the order in which
// the specification declares the tags, then in the order of first use. A name that is already taken gets a number.
internal sealed class OpenApiTagFolders
{
    private readonly List<string> _tags = [];
    private readonly Dictionary<string, List<RequestDefinition>> _tagged = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RequestDefinition> _untagged = [];

    public OpenApiTagFolders(IEnumerable<string> declaredTags)
    {
        foreach (var tag in declaredTags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0))
        {
            RequestsOf(tag);
        }
    }

    public void Add(string? tag, RequestDefinition request)
    {
        var name = tag?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            _untagged.Add(request);
            return;
        }

        var requests = RequestsOf(name);
        requests.Add(WithFreeName(request, requests.Select(existing => existing.Name)));
    }

    // The root folder. A declared tag that no operation uses gives no folder.
    public Folder Build(IIdGenerator ids)
    {
        var folders = _tags
            .Where(tag => _tagged[tag].Count > 0)
            .Select(tag => new Folder(ids.NewId(), tag, [], _tagged[tag]))
            .ToList();
        var requests = new List<RequestDefinition>();
        foreach (var request in _untagged)
        {
            var taken = folders.Select(folder => folder.Name).Concat(requests.Select(existing => existing.Name));
            requests.Add(WithFreeName(request, taken));
        }

        return new Folder(ids.NewId(), string.Empty, folders, requests);
    }

    private List<RequestDefinition> RequestsOf(string tag)
    {
        if (!_tagged.TryGetValue(tag, out var requests))
        {
            requests = [];
            _tagged.Add(tag, requests);
            _tags.Add(tag);
        }

        return requests;
    }

    private static RequestDefinition WithFreeName(RequestDefinition request, IEnumerable<string> takenNames)
    {
        return request with { Name = ChildNameRules.NextAvailableName(request.Name, takenNames.ToList()) };
    }
}
