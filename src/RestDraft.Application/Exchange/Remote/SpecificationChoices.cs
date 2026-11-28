namespace RestDraft.Application.Exchange.Remote;

// A page that offers several specifications. The user picks one, and it is downloaded like a typed address.
public sealed record SpecificationChoices(IReadOnlyList<SpecificationLink> Links) : RemoteImport;
