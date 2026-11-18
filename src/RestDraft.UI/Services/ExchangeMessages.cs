using RestDraft.Application.Exchange;

namespace RestDraft.UI.Services;

public static class ExchangeMessages
{
    public static string Imported(ImportOutcome outcome)
    {
        var counts = outcome.Counts;
        var requests = $"{counts.NewRequests} new";
        if (counts.OverwrittenRequests > 0)
        {
            requests += $" and {counts.OverwrittenRequests} overwritten";
        }

        var message = $"Imported {requests} {(counts.NewRequests + counts.OverwrittenRequests == 1 ? "request" : "requests")} into '{outcome.CollectionName}'";
        if (outcome.WarningCount > 0)
        {
            message += $"; {outcome.WarningCount} {(outcome.WarningCount == 1 ? "warning" : "warnings")}";
        }

        return message + ".";
    }

    public static string Exported(ExportOutcome outcome)
    {
        return $"Exported {outcome.Requests} {(outcome.Requests == 1 ? "request" : "requests")} to {outcome.Path}.";
    }

    // The suggested file name for an export: the collection name without the characters a file name cannot hold.
    public static string SuggestedFileName(string collectionName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string([.. collectionName.Select(character => invalid.Contains(character) ? '_' : character)]).Trim();
        return cleaned.Length == 0 ? "collection" : cleaned;
    }
}
