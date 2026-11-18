using RestDraft.Domain.Collections;

namespace RestDraft.Application.Tests.Exchange;

// A file with one folder and a request at the top, and a collection that already has some of the same names.
internal sealed class ExchangeFixtures
{
    public ExchangeFixtures()
    {
        SourceList = Request("List", "http://file/list");
        SourceCreate = Request("Create", "http://file/create");
        SourcePing = Request("Ping", "http://file/ping");
        SourceApi = Folder("API", [SourceList, SourceCreate]);
        Source = new Collection(Guid.NewGuid(), "Shop file", Folder(string.Empty, [SourcePing], [SourceApi]));

        TargetList = Request("list", "http://mine/list");
        TargetOther = Request("Other", "http://mine/other");
        TargetApi = Folder("API", [TargetList, TargetOther]);
        Target = new Collection(Guid.NewGuid(), "Shop", Folder(string.Empty, [], [TargetApi]));
    }

    public Collection Source { get; }

    public Collection Target { get; }

    public RequestDefinition SourceList { get; }

    public RequestDefinition SourceCreate { get; }

    public RequestDefinition SourcePing { get; }

    public Folder SourceApi { get; }

    public RequestDefinition TargetList { get; }

    public RequestDefinition TargetOther { get; }

    public Folder TargetApi { get; }

    public HashSet<Guid> AllTicked => [Source.Id];

    // The ids of a folder and everything in it, in order. Records compare their lists by reference, so trees are compared
    // through this.
    public static string Describe(Folder folder)
    {
        var folders = folder.Folders.Select(Describe);
        var requests = folder.Requests.Select(request => request.Id.ToString());
        return $"{folder.Id}[{string.Join(",", folders.Concat(requests))}]";
    }

    public static RequestDefinition Request(string name, string url)
    {
        return new RequestDefinition { Id = Guid.NewGuid(), Name = name, Url = url };
    }

    public static Folder Folder(string name, IReadOnlyList<RequestDefinition> requests, IReadOnlyList<Folder>? folders = null)
    {
        return new Folder(Guid.NewGuid(), name, folders ?? [], requests);
    }
}
