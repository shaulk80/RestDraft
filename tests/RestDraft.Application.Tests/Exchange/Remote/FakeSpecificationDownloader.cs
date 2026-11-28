using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Tests.Exchange.Remote;

// A web of fixed addresses. An address that was not set up answers 404.
internal sealed class FakeSpecificationDownloader : ISpecificationDownloader
{
    private readonly Dictionary<string, Result<DownloadedText>> _answers = [];

    public List<string> Asked { get; } = [];

    public List<DownloadHeaders> Headers { get; } = [];

    // Runs before an answer is given, for a test that acts in the middle of a search.
    public Action<Uri> OnDownload { get; set; } = _ => { };

    public void Serve(string address, string text)
    {
        Redirect(address, address, text);
    }

    public void Redirect(string address, string finalAddress, string text)
    {
        _answers[new Uri(address).AbsoluteUri] = Result<DownloadedText>.Success(new DownloadedText(new Uri(finalAddress), text));
    }

    public void Fail(string address, string message)
    {
        _answers[new Uri(address).AbsoluteUri] = Result<DownloadedText>.Failure(new Error(ErrorCategory.Network, message));
    }

    public Task<Result<DownloadedText>> DownloadAsync(Uri address, DownloadHeaders headers, CancellationToken cancellationToken)
    {
        Asked.Add(address.AbsoluteUri);
        Headers.Add(headers);
        OnDownload(address);
        var answer = _answers.TryGetValue(address.AbsoluteUri, out var known)
            ? known
            : Result<DownloadedText>.Failure(new Error(ErrorCategory.Network, "The server answered 404 (Not Found)."));
        return Task.FromResult(answer);
    }
}
