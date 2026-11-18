using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

public interface IExportService
{
    // The format that files are written in.
    ICollectionFormat Format { get; }

    Task<Result<ExportOutcome>> ExportAsync(
        Guid collectionId,
        IReadOnlySet<Guid> ticked,
        string path,
        CancellationToken cancellationToken);
}
