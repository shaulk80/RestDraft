namespace RestDraft.Application.Exchange;

// Where an import goes: a collection that is created with this name, or one that exists.
public sealed record ImportTarget
{
    private ImportTarget()
    {
    }

    public string NewCollectionName { get; private init; } = string.Empty;

    public Guid? ExistingCollectionId { get; private init; }

    public bool IsNewCollection => ExistingCollectionId is null;

    public static ImportTarget NewCollection(string name)
    {
        return new ImportTarget { NewCollectionName = name };
    }

    public static ImportTarget Existing(Guid collectionId)
    {
        return new ImportTarget { ExistingCollectionId = collectionId };
    }
}
