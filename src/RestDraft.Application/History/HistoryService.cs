using RestDraft.Application.Abstractions;
using RestDraft.Application.Requests;
using RestDraft.Application.Search;
using RestDraft.Domain.Collections;
using RestDraft.Domain.History;
using RestDraft.Domain.Results;

namespace RestDraft.Application.History;

// Records every send and serializes the writes, so two sends finishing together cannot interleave
// their inserts and retention passes.
public sealed class HistoryService : IHistoryService, IDisposable
{
    private readonly IHistoryRepository _repository;
    private readonly HistoryRetention _retention;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public HistoryService(
        IHistoryRepository repository,
        HistoryRetention retention,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _repository = repository;
        _retention = retention;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result> RecordAsync(RequestAttempt attempt, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var added = await _repository.AddAsync(ToEntry(attempt), cancellationToken);
            if (!added.IsSuccess)
            {
                return added;
            }

            return await _retention.ApplyAsync(_clock.UtcNow, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    // The repository finds URLs. A body is not a column, so a search in bodies reads the entries of the method
    // and looks at the stored requests here.
    public async Task<Result<IReadOnlyList<HistoryEntry>>> SearchAsync(HistoryFilter filter, CancellationToken cancellationToken)
    {
        if (!filter.InBodies || filter.UrlText.Length == 0)
        {
            return await _repository.SearchAsync(filter, cancellationToken);
        }

        var entries = await _repository.SearchAsync(filter with { UrlText = string.Empty, InBodies = false }, cancellationToken);
        if (!entries.IsSuccess)
        {
            return entries;
        }

        var found = entries.Value!
            .Where(entry => RequestMatcher.Contains(entry.Url, filter.UrlText) || RequestMatcher.BodyContains(entry.RequestSnapshot.Body, filter.UrlText))
            .ToList();
        return Result<IReadOnlyList<HistoryEntry>>.Success(found);
    }

    // Returns an unsaved definition. It gets a new identifier, so it cannot be mistaken for the saved request it came from.
    // Nothing is written to a collection here. The caller decides where the request is saved.
    public async Task<Result<RequestDefinition>> RestoreAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetAsync(entryId, cancellationToken);
        if (!entry.IsSuccess)
        {
            return Result<RequestDefinition>.Failure(entry.Error!);
        }

        var restored = entry.Value!.RequestSnapshot with { Id = _idGenerator.NewId() };
        return Result<RequestDefinition>.Success(restored);
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private HistoryEntry ToEntry(RequestAttempt attempt)
    {
        var outcome = attempt.Outcome;
        return new HistoryEntry(
            _idGenerator.NewId(),
            attempt.SentAt,
            attempt.Request.Method,
            attempt.Url,
            attempt.Request,
            outcome.Value?.StatusCode,
            attempt.DurationMs,
            ErrorText(outcome.Error),
            outcome.Value?.BodyFilePath);
    }

    private static string? ErrorText(Error? error)
    {
        if (error is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(error.Details)
            ? error.Message
            : $"{error.Message} ({error.Details})";
    }
}
