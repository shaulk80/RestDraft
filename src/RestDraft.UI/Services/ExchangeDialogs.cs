using Avalonia.Controls;
using RestDraft.Application.Collections;
using RestDraft.Application.Exchange;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Services;

public sealed class ExchangeDialogs : IExchangeDialogs
{
    private readonly IImportService _import;
    private readonly IRemoteImportService _remoteImport;
    private readonly ImportAddressMemory _addresses;
    private readonly IExportService _export;
    private readonly ICollectionService _collections;
    private readonly IExchangeFilePicker _filePicker;
    private readonly IConfirmationDialogs _confirmations;
    private readonly TabStripViewModel _tabs;

    public ExchangeDialogs(
        IImportService import,
        IRemoteImportService remoteImport,
        ImportAddressMemory addresses,
        IExportService export,
        ICollectionService collections,
        IExchangeFilePicker filePicker,
        IConfirmationDialogs confirmations,
        TabStripViewModel tabs)
    {
        _import = import;
        _remoteImport = remoteImport;
        _addresses = addresses;
        _export = export;
        _collections = collections;
        _filePicker = filePicker;
        _confirmations = confirmations;
        _tabs = tabs;
    }

    public async Task<ExchangeOutcome> ImportAsync(Guid? selectedCollectionId, CancellationToken cancellationToken)
    {
        var picked = await _filePicker.PickFileToImportAsync(_import.Formats);
        if (!picked.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(picked.Error!));
        }

        if (picked.Value is not { } path)
        {
            return ExchangeOutcome.Cancelled;
        }

        var read = await Task.Run(() => _import.ReadAsync(path, cancellationToken), cancellationToken);
        if (!read.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(read.Error!));
        }

        var confirmed = await ConfirmImportAsync(read.Value!, selectedCollectionId, null, null, cancellationToken);
        if (!confirmed.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(confirmed.Error!));
        }

        return confirmed.Value is { } request
            ? await ApplyAsync(read.Value!, request, cancellationToken)
            : ExchangeOutcome.Cancelled;
    }

    public async Task<ExchangeOutcome> ImportFromUrlAsync(Guid? selectedCollectionId, CancellationToken cancellationToken)
    {
        var remembered = await _addresses.RecallAsync(cancellationToken);
        if (!remembered.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(remembered.Error!));
        }

        if (MainWindowLocator.Find() is not { } owner)
        {
            return ExchangeOutcome.Cancelled;
        }

        var found = await ShowUrlDialogAsync(owner, remembered.Value!.Address);
        if (found is null)
        {
            return ExchangeOutcome.Cancelled;
        }

        var confirmed = await ConfirmImportAsync(
            found.Document,
            selectedCollectionId,
            found.DownloadAddress,
            remembered.Value.BaseUrlFor(found.TypedAddress),
            cancellationToken);
        if (!confirmed.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(confirmed.Error!));
        }

        if (confirmed.Value is not { } request)
        {
            return ExchangeOutcome.Cancelled;
        }

        var outcome = await ApplyAsync(found.Document, request, cancellationToken);
        return outcome.Changed
            ? await RememberAddressAsync(outcome, found.TypedAddress, request.BaseUrl, cancellationToken)
            : outcome;
    }

    public async Task<ExchangeOutcome> ExportAsync(Guid? selectedCollectionId, CancellationToken cancellationToken)
    {
        var listed = await _collections.ListAsync(cancellationToken);
        if (!listed.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(listed.Error!));
        }

        if (listed.Value!.Count == 0)
        {
            return ExchangeOutcome.Failed("There is no collection to export.");
        }

        var selection = await ShowSelectionAsync(listed.Value!, selectedCollectionId);
        if (selection is null)
        {
            return ExchangeOutcome.Cancelled;
        }

        return await WriteAsync(selection, cancellationToken);
    }

    private async Task<ImportUrlResult?> ShowUrlDialogAsync(Window owner, string address)
    {
        var viewModel = new ImportUrlViewModel(_remoteImport, address);
        return await new ImportUrlWindow { DataContext = viewModel }.ShowDialog<ImportUrlResult?>(owner);
    }

    // The preview and the second question about open tabs. Null when the user cancelled.
    private async Task<Result<ImportRequest?>> ConfirmImportAsync(
        ExchangeDocument document,
        Guid? selectedCollectionId,
        Uri? downloadAddress,
        string? rememberedBaseUrl,
        CancellationToken cancellationToken)
    {
        var existing = await _collections.ListAsync(cancellationToken);
        if (!existing.IsSuccess)
        {
            return Result<ImportRequest?>.Failure(existing.Error!);
        }

        if (MainWindowLocator.Find() is not { } owner)
        {
            return Result<ImportRequest?>.Success(null);
        }

        var viewModel = new ImportPreviewViewModel(
            document,
            existing.Value!,
            selectedCollectionId,
            _tabs.CountLinkedTabsWithEdits,
            downloadAddress,
            rememberedBaseUrl);
        var request = await new ImportPreviewWindow { DataContext = viewModel }.ShowDialog<ImportRequest?>(owner);
        if (request is null || !await ConfirmReplacedEditsAsync(request, cancellationToken))
        {
            return Result<ImportRequest?>.Success(null);
        }

        return Result<ImportRequest?>.Success(request);
    }

    // The import is done by now, so a failure here only adds to its message.
    private async Task<ExchangeOutcome> RememberAddressAsync(ExchangeOutcome outcome, Uri address, string? baseUrl, CancellationToken cancellationToken)
    {
        var remembered = await _addresses.RememberAsync(address, baseUrl, cancellationToken);
        return remembered.IsSuccess
            ? outcome
            : outcome with { Message = $"{outcome.Message} The address was not remembered: {ErrorMessageFormatter.Format(remembered.Error!)}" };
    }

    private static async Task<ExportSelection?> ShowSelectionAsync(IReadOnlyList<Collection> collections, Guid? selectedCollectionId)
    {
        if (MainWindowLocator.Find() is not { } owner)
        {
            return null;
        }

        var viewModel = new ExportSelectionViewModel(collections, selectedCollectionId);
        return await new ExportSelectionWindow { DataContext = viewModel }.ShowDialog<ExportSelection?>(owner);
    }

    // The preview has said that open tabs with unsaved edits are replaced. Importing goes on only if the user agrees again.
    private async Task<bool> ConfirmReplacedEditsAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        if (request.OpenTabsWithEdits == 0)
        {
            return true;
        }

        var tabs = request.OpenTabsWithEdits == 1 ? "1 open tab has" : $"{request.OpenTabsWithEdits} open tabs have";
        return await _confirmations.ConfirmAsync($"{tabs} unsaved edits that the import replaces. Import anyway?", cancellationToken);
    }

    private async Task<ExchangeOutcome> ApplyAsync(ExchangeDocument document, ImportRequest request, CancellationToken cancellationToken)
    {
        var addressed = request.BaseUrl is null ? document : document.WithBaseUrl(request.BaseUrl);
        var applied = await _import.ApplyAsync(addressed, request.Target, request.Overwrite, request.TickedIds, cancellationToken);
        if (!applied.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(applied.Error!));
        }

        var outcome = applied.Value!;
        return ExchangeOutcome.Imported(ExchangeMessages.Imported(outcome), outcome.CollectionId, outcome.OverwrittenRequestIds);
    }

    private async Task<ExchangeOutcome> WriteAsync(ExportSelection selection, CancellationToken cancellationToken)
    {
        var suggested = ExchangeMessages.SuggestedFileName(selection.CollectionName);
        var picked = await _filePicker.PickFileToExportAsync(suggested, _export.Format);
        if (!picked.IsSuccess)
        {
            return ExchangeOutcome.Failed(ErrorMessageFormatter.Format(picked.Error!));
        }

        if (picked.Value is not { } path)
        {
            return ExchangeOutcome.Cancelled;
        }

        var exported = await Task.Run(() => _export.ExportAsync(selection.CollectionId, selection.TickedIds, path, cancellationToken), cancellationToken);
        return exported.IsSuccess
            ? ExchangeOutcome.Done(ExchangeMessages.Exported(exported.Value!))
            : ExchangeOutcome.Failed(ErrorMessageFormatter.Format(exported.Error!));
    }
}
