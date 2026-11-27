using RestDraft.Application.Exchange;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Collects what is left out while a file is read. Things that are the same for many items are counted and reported once.
internal sealed class ImportWarningLog
{
    private readonly ImportWarningList _warnings = new();

    public int RequestsWithSettings { get; set; }

    public int ItemsWithScripts { get; set; }

    public int ItemsWithDescription { get; set; }

    public bool FoldersHaveHeadersOrAuth { get; set; }

    public void Add(string message, string? path = null)
    {
        _warnings.Add(message, path);
    }

    public IReadOnlyList<ImportWarning> ToList()
    {
        var all = new List<ImportWarning>(_warnings.ToList());
        AddIf(all, RequestsWithSettings > 0, $"{Count(RequestsWithSettings, "request has", "requests have")} settings that RestDraft does not keep.");
        AddIf(all, ItemsWithScripts > 0, $"{Count(ItemsWithScripts, "item has", "items have")} scripts that RestDraft does not keep.");
        AddIf(all, ItemsWithDescription > 0, $"{Count(ItemsWithDescription, "item has", "items have")} a description that RestDraft does not keep.");
        AddIf(all, FoldersHaveHeadersOrAuth, "Some folders have headers or authentication that RestDraft does not keep.");
        return all;
    }

    private static void AddIf(List<ImportWarning> all, bool condition, string message)
    {
        if (condition)
        {
            all.Add(new ImportWarning(message));
        }
    }

    private static string Count(int count, string singular, string plural)
    {
        return $"{count} {(count == 1 ? singular : plural)}";
    }
}
