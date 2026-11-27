using Avalonia.Platform.Storage;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Results;

namespace RestDraft.UI.Services;

public sealed class ExchangeFilePicker : IExchangeFilePicker
{
    public async Task<Result<string?>> PickFileToImportAsync(IReadOnlyList<ICollectionFormat> formats)
    {
        if (MainWindowLocator.Find() is not { } owner)
        {
            return NoWindow();
        }

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import a collection",
            AllowMultiple = false,
            FileTypeFilter = [AllSupported(formats), .. formats.Select(FileTypeOf), FilePickerFileTypes.All],
        });
        return files.Count == 0 ? Result<string?>.Success(null) : LocalPath(files[0]);
    }

    public async Task<Result<string?>> PickFileToExportAsync(string suggestedName, ICollectionFormat format)
    {
        if (MainWindowLocator.Find() is not { } owner)
        {
            return NoWindow();
        }

        var extension = format.FileExtensions[0];
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export a collection",
            SuggestedFileName = suggestedName + extension,
            DefaultExtension = extension.TrimStart('.'),
            FileTypeChoices = [FileTypeOf(format)],
            ShowOverwritePrompt = true,
        });
        return file is null ? Result<string?>.Success(null) : LocalPath(file);
    }

    // The first filter is the one that a dialog starts with, so it shows every file that can be imported.
    private static FilePickerFileType AllSupported(IReadOnlyList<ICollectionFormat> formats)
    {
        return FileTypeFor("All supported files", formats.SelectMany(format => format.FileExtensions).Distinct());
    }

    private static FilePickerFileType FileTypeOf(ICollectionFormat format)
    {
        return FileTypeFor(format.DisplayName, format.FileExtensions);
    }

    private static FilePickerFileType FileTypeFor(string name, IEnumerable<string> extensions)
    {
        return new FilePickerFileType(name)
        {
            Patterns = [.. extensions.Select(extension => "*" + extension)],
        };
    }

    private static Result<string?> LocalPath(IStorageItem item)
    {
        var path = item.TryGetLocalPath();
        return path is null
            ? Result<string?>.Failure(ResultErrors.Validation("The selected file has no local path."))
            : Result<string?>.Success(path);
    }

    private static Result<string?> NoWindow()
    {
        return Result<string?>.Failure(new Error(ErrorCategory.Unexpected, "The file dialog has no window to attach to."));
    }
}
