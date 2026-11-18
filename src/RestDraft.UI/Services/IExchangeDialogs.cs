namespace RestDraft.UI.Services;

// The whole import and export, from the file dialog to the result. The view model of the main window uses it, and tests
// replace it.
public interface IExchangeDialogs
{
    // The collection selected in the tree, if any, is preselected as the target.
    Task<ExchangeOutcome> ImportAsync(Guid? selectedCollectionId, CancellationToken cancellationToken);

    Task<ExchangeOutcome> ExportAsync(Guid? selectedCollectionId, CancellationToken cancellationToken);
}
