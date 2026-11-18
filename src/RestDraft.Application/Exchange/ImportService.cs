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

    public ImportService(Workspace<Collection> workspace, IFileReader reader, IEnumerable<ICollectionFormat> formats)
    {
        _workspace = workspace;
        _reader = reader;
        Formats = formats.ToList();
    }

    public IReadOnlyList<ICollectionFormat> Formats { get; }

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

        var text = new UTF8Encoding(false).GetString(bytes.Value).TrimStart('﻿');
        return ReadText(text);
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

    private Result<ExchangeDocument> ReadText(string text)
    {
        foreach (var format in Formats)
        {
            var canRead = format.CanRead(text);
            if (!canRead.IsSuccess)
            {
                return Result<ExchangeDocument>.Failure(canRead.Error!);
            }

            if (canRead.Value)
            {
                return format.Read(text);
            }
        }

        return Result<ExchangeDocument>.Failure(ResultErrors.Validation(NotSupportedMessage(text)));
    }

    private string NotSupportedMessage(string text)
    {
        var names = string.Join(", ", Formats.Select(format => format.DisplayName));
        var message = $"The file is not in a supported format. Supported: {names}.";
        return text.TrimStart().StartsWith('{')
            ? message + " Insomnia v4 JSON is not supported yet."
            : message;
    }
}
