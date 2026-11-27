using System.Text;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Workspaces;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

public sealed class ImportService : IImportService
{
    private readonly Workspace<Collection> _workspace;
    private readonly IFileReader _reader;
    private readonly CollectionTextReader _textReader;

    public ImportService(Workspace<Collection> workspace, IFileReader reader, CollectionTextReader textReader)
    {
        _workspace = workspace;
        _reader = reader;
        _textReader = textReader;
    }

    public IReadOnlyList<ICollectionFormat> Formats => _textReader.Formats;

    public async Task<Result<ExchangeDocument>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var bytes = await _reader.ReadAllBytesAsync(path, cancellationToken);
        if (!bytes.IsSuccess)
        {
            return Result<ExchangeDocument>.Failure(bytes.Error!);
        }

        if (bytes.Value!.Length > ExchangeLimits.MaxFileBytes)
        {
            return Result<ExchangeDocument>.Failure(ResultErrors.Validation(
                $"The file is larger than {ExchangeLimits.MaxFileBytes / (1024 * 1024)} MB."));
        }

        return _textReader.Read(new UTF8Encoding(false).GetString(bytes.Value));
    }

    public async Task<Result<ImportOutcome>> ApplyAsync(
        ExchangeDocument document,
        ImportTarget target,
        bool overwrite,
        IReadOnlySet<Guid> ticked,
        CancellationToken cancellationToken)
    {
        ImportPlan? applied = null;
        var saved = await _workspace.UpdateAsync(
            collections =>
            {
                var planned = ImportPlanner.Plan(collections, document.Collection, target, overwrite, ticked);
                if (!planned.IsSuccess)
                {
                    return Result<IReadOnlyList<Collection>>.Failure(planned.Error!);
                }

                applied = planned.Value;
                return Result<IReadOnlyList<Collection>>.Success(planned.Value!.Collections);
            },
            cancellationToken);
        if (!saved.IsSuccess)
        {
            return Result<ImportOutcome>.Failure(saved.Error!);
        }

        return Result<ImportOutcome>.Success(new ImportOutcome(
            applied!.TargetCollectionId,
            applied.TargetCollectionName,
            applied.Counts,
            applied.OverwrittenRequestIds,
            document.Warnings.Count));
    }
}
