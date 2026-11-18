using RestDraft.Application.Exchange;
using RestDraft.Domain.Results;

namespace RestDraft.UI.Services;

// The file dialogs of the import and the export. Success with a null value means the user cancelled.
public interface IExchangeFilePicker
{
    Task<Result<string?>> PickFileToImportAsync(IReadOnlyList<ICollectionFormat> formats);

    Task<Result<string?>> PickFileToExportAsync(string suggestedName, ICollectionFormat format);
}
