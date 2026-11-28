using RestDraft.Application.Settings;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

// Keeps the address of the last import from an address in the settings. A user name and a password are not kept, and
// the headers of a download are never given to it.
public sealed class ImportAddressMemory
{
    private readonly ISettingsService _settings;

    public ImportAddressMemory(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<Result<RememberedImport>> RecallAsync(CancellationToken cancellationToken)
    {
        var loaded = await _settings.GetAsync(cancellationToken);
        return loaded.IsSuccess
            ? Result<RememberedImport>.Success(new RememberedImport(loaded.Value!.LastImportUrl, loaded.Value.LastImportBaseUrl))
            : Result<RememberedImport>.Failure(loaded.Error!);
    }

    // baseUrl is null for a document whose request URLs are complete.
    public async Task<Result> RememberAsync(Uri address, string? baseUrl, CancellationToken cancellationToken)
    {
        var current = await _settings.GetAsync(cancellationToken);
        if (!current.IsSuccess)
        {
            return Result.Failure(current.Error!);
        }

        var updated = current.Value! with
        {
            LastImportUrl = WebAddress.WithoutUserInfo(address),
            LastImportBaseUrl = baseUrl ?? string.Empty,
        };
        return await _settings.SaveAsync(updated, cancellationToken);
    }
}
