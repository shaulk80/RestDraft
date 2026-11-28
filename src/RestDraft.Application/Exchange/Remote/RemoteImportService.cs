using RestDraft.Application.Abstractions;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

public sealed class RemoteImportService : IRemoteImportService
{
    private readonly ISpecificationDownloader _downloader;
    private readonly CollectionTextReader _textReader;

    public RemoteImportService(ISpecificationDownloader downloader, CollectionTextReader textReader)
    {
        _downloader = downloader;
        _textReader = textReader;
    }

    public async Task<Result<RemoteImport>> ReadAsync(Uri address, DownloadHeaders headers, CancellationToken cancellationToken)
    {
        var search = new SpecificationSearch(_downloader, _textReader, headers);
        var result = await SearchAsync(search, address, cancellationToken);
        // A cancelled search ends early with whatever it had, which says nothing about the address.
        return cancellationToken.IsCancellationRequested
            ? Result<RemoteImport>.Failure(DownloadErrors.Cancelled)
            : result;
    }

    private static async Task<Result<RemoteImport>> SearchAsync(SpecificationSearch search, Uri address, CancellationToken cancellationToken)
    {
        var first = await search.DownloadFirstAsync(address, cancellationToken);
        if (!first.IsSuccess)
        {
            return Result<RemoteImport>.Failure(first.Error!);
        }

        var downloaded = first.Value!;
        if (!SwaggerPageScanner.IsPage(downloaded.Text))
        {
            return search.Read(downloaded) ?? Result<RemoteImport>.Failure(ResultErrors.Validation(
                $"The address {WebAddress.ForDisplay(address)} gives neither a specification nor a Swagger UI page."));
        }

        var found = await new SwaggerPageSearch(search, downloaded).RunAsync(cancellationToken);
        return found ?? Result<RemoteImport>.Failure(search.NothingFound(address));
    }
}
