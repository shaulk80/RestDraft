using RestDraft.Application.Exchange;

namespace RestDraft.Infrastructure.Exchange;

// The warnings of one read, in the order they were found.
internal sealed class ImportWarningList
{
    private readonly List<ImportWarning> _warnings = [];
    private readonly HashSet<string> _reportedOnce = new(StringComparer.Ordinal);

    public void Add(string message, string? path = null)
    {
        _warnings.Add(new ImportWarning(message, path));
    }

    // For something that many items can have: the message is reported the first time only.
    public void AddOnce(string message)
    {
        if (_reportedOnce.Add(message))
        {
            Add(message);
        }
    }

    public IReadOnlyList<ImportWarning> ToList()
    {
        return [.. _warnings];
    }
}
