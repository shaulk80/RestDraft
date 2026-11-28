using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Abstractions;

// Gets the text behind a web address. The download follows the settings of the app, sends the headers only to the host
// that may get them, and stops at the import size limit.
public interface ISpecificationDownloader
{
    Task<Result<DownloadedText>> DownloadAsync(Uri address, DownloadHeaders headers, CancellationToken cancellationToken);
}
