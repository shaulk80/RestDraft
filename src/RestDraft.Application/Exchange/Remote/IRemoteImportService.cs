using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

public interface IRemoteImportService
{
    // Takes the address of a specification, or of a Swagger UI page that leads to one. A failure says what was tried.
    Task<Result<RemoteImport>> ReadAsync(Uri address, DownloadHeaders headers, CancellationToken cancellationToken);
}
