using RestDraft.Application.Abstractions;
using RestDraft.Application.Collections;
using RestDraft.Application.Workspaces;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

public sealed class ExportService : IExportService
{
    private readonly Workspace<Collection> _workspace;
    private readonly IFileWriter _writer;

    public ExportService(Workspace<Collection> workspace, IFileWriter writer, IWritableCollectionFormat format)
    {
        _workspace = workspace;
        _writer = writer;
        Format = format;
    }

    public IWritableCollectionFormat Format { get; }

    public async Task<Result<ExportOutcome>> ExportAsync(
        Guid collectionId,
        IReadOnlySet<Guid> ticked,
        string path,
        CancellationToken cancellationToken)
    {
        var loaded = await _workspace.GetAllAsync(cancellationToken);
        if (!loaded.IsSuccess)
        {
            return Result<ExportOutcome>.Failure(loaded.Error!);
        }

        var collection = CollectionSet.Find(loaded.Value!, collectionId);
        if (collection is null)
        {
            return Result<ExportOutcome>.Failure(ResultErrors.NotFound("The collection"));
        }

        var selected = SelectionPruner.Prune(collection, ticked);
        var counts = TreeCounts.Of(selected.Root);
        if (counts.IsEmpty)
        {
            return Result<ExportOutcome>.Failure(ResultErrors.Validation("Nothing is selected to export."));
        }

        var text = Format.Write(selected);
        if (!text.IsSuccess)
        {
            return Result<ExportOutcome>.Failure(text.Error!);
        }

        var written = await _writer.WriteAllTextAsync(path, text.Value!, cancellationToken);
        return written.IsSuccess
            ? Result<ExportOutcome>.Success(new ExportOutcome(path, counts.Folders, counts.Requests))
            : Result<ExportOutcome>.Failure(written.Error!);
    }
}
