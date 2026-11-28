using RestDraft.Application.Exchange;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.UI.Tests.ViewModels;

// A download that a test finishes when it wants to, with the result it wants.
internal sealed class FakeRemoteImport : IRemoteImportService
{
    private TaskCompletionSource<Result<RemoteImport>> _pending = NewPending();

    public List<Uri> Addresses { get; } = [];

    public DownloadHeaders? Headers { get; private set; }

    public CancellationToken Token { get; private set; }

    // Completes when the download has been asked for.
    public TaskCompletionSource Started { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Result<RemoteImport> Found(string address)
    {
        var root = new Folder(Guid.NewGuid(), string.Empty, [], [new RequestDefinition { Id = Guid.NewGuid(), Name = "List" }]);
        var document = new ExchangeDocument(new Collection(Guid.NewGuid(), "Shop", root), [], "/api");
        return Result<RemoteImport>.Success(new FoundSpecification(document, new Uri(address)));
    }

    public static Result<RemoteImport> Offering(params string[] names)
    {
        var links = names.Select(name => new SpecificationLink(name, new Uri($"http://api.test/{name}.json"))).ToList();
        return Result<RemoteImport>.Success(new SpecificationChoices(links));
    }

    public void Finish(Result<RemoteImport> result)
    {
        var pending = _pending;
        _pending = NewPending();
        Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pending.SetResult(result);
    }

    public Task<Result<RemoteImport>> ReadAsync(Uri address, DownloadHeaders headers, CancellationToken cancellationToken)
    {
        Addresses.Add(address);
        Headers = headers;
        Token = cancellationToken;
        var answer = _pending.Task;
        Started.TrySetResult();
        return answer;
    }

    private static TaskCompletionSource<Result<RemoteImport>> NewPending()
    {
        return new TaskCompletionSource<Result<RemoteImport>>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
