using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

public interface IImportService
{
    IReadOnlyList<ICollectionFormat> Formats { get; }

    Task<Result<ExchangeDocument>> ReadAsync(string path, CancellationToken cancellationToken);

    // Applies the ticked part of the document in one save. Nothing is saved when it fails.
    Task<Result<ImportOutcome>> ApplyAsync(
        ExchangeDocument document,
        ImportTarget target,
        bool overwrite,
        IReadOnlySet<Guid> ticked,
        CancellationToken cancellationToken);
}
