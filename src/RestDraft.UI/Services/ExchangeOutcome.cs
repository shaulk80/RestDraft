namespace RestDraft.UI.Services;

// How an import or an export ended, for the main window: whether the tree must be reloaded, which collection to select,
// which requests were replaced (their open tabs reload), and the message for the message line.
public sealed record ExchangeOutcome(
    bool Changed,
    string Message,
    bool IsError,
    Guid? CollectionId,
    IReadOnlyList<Guid> OverwrittenRequestIds)
{
    // The user cancelled a dialog. There is nothing to say.
    public static ExchangeOutcome Cancelled { get; } = new(false, string.Empty, false, null, []);

    public static ExchangeOutcome Failed(string message)
    {
        return new ExchangeOutcome(false, message, true, null, []);
    }

    public static ExchangeOutcome Done(string message)
    {
        return new ExchangeOutcome(false, message, false, null, []);
    }

    public static ExchangeOutcome Imported(string message, Guid collectionId, IReadOnlyList<Guid> overwrittenRequestIds)
    {
        return new ExchangeOutcome(true, message, false, collectionId, overwrittenRequestIds);
    }
}
