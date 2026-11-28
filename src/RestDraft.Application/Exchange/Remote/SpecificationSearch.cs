using RestDraft.Application.Abstractions;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

// One attempt to find a specification: it downloads, never the same address twice and no more than the limit, and it
// remembers why an address led nowhere.
internal sealed class SpecificationSearch
{
    public const int MaxDownloads = 20;

    private readonly ISpecificationDownloader _downloader;
    private readonly CollectionTextReader _textReader;
    private readonly DownloadHeaders _headers;
    private readonly HashSet<string> _asked = new(StringComparer.Ordinal);
    private readonly List<string> _tried = [];
    private int _downloads;
    private bool _isLimitReached;

    public SpecificationSearch(ISpecificationDownloader downloader, CollectionTextReader textReader, DownloadHeaders headers)
    {
        _downloader = downloader;
        _textReader = textReader;
        _headers = headers;
    }

    // The first download of an attempt, whose failure is the failure of the attempt.
    public Task<Result<DownloadedText>> DownloadFirstAsync(Uri address, CancellationToken cancellationToken)
    {
        _asked.Add(address.AbsoluteUri);
        return CountedDownloadAsync(address, cancellationToken);
    }

    // Null when the address was asked before, the limit is reached, the attempt is cancelled or the download failed.
    public async Task<DownloadedText?> DownloadAsync(Uri address, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || !_asked.Add(address.AbsoluteUri))
        {
            return null;
        }

        if (_downloads >= MaxDownloads)
        {
            _isLimitReached = true;
            return null;
        }

        var downloaded = await CountedDownloadAsync(address, cancellationToken);
        if (!downloaded.IsSuccess)
        {
            Note(address, downloaded.Error!.Message);
            return null;
        }

        return downloaded.Value;
    }

    // The document, or why it cannot be read although a format knows it. Null when no format knows the text.
    public Result<RemoteImport>? Read(DownloadedText downloaded)
    {
        if (SwaggerPageScanner.IsPage(downloaded.Text))
        {
            return null;
        }

        var read = _textReader.TryRead(downloaded.Text);
        if (read is null)
        {
            return null;
        }

        return read.IsSuccess
            ? Result<RemoteImport>.Success(new FoundSpecification(read.Value!, downloaded.Address))
            : Result<RemoteImport>.Failure(read.Error!);
    }

    // Downloads an address that should hold a specification. Null means that the search goes on.
    public async Task<Result<RemoteImport>?> TryAsync(Uri address, CancellationToken cancellationToken)
    {
        var downloaded = await DownloadAsync(address, cancellationToken);
        if (downloaded is null)
        {
            return null;
        }

        var read = Read(downloaded);
        if (read is null)
        {
            Note(address, "Not a specification.");
        }

        return read;
    }

    public Error NothingFound(Uri address)
    {
        var lines = new List<string> { $"No specification was found at {WebAddress.ForDisplay(address)}." };
        if (_isLimitReached)
        {
            lines.Add($"The search stopped after {MaxDownloads} downloads.");
        }

        if (_tried.Count > 0)
        {
            lines.Add("Tried:");
            lines.AddRange(_tried);
        }

        return ResultErrors.Validation(string.Join('\n', lines));
    }

    private void Note(Uri address, string reason)
    {
        _tried.Add($"{WebAddress.ForDisplay(address)}: {reason}");
    }

    private Task<Result<DownloadedText>> CountedDownloadAsync(Uri address, CancellationToken cancellationToken)
    {
        _downloads++;
        return _downloader.DownloadAsync(address, _headers, cancellationToken);
    }
}
