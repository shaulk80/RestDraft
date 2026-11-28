namespace RestDraft.Application.Exchange.Remote;

// A document that was read. Address is the one it was downloaded from, which a relative server address is resolved against.
public sealed record FoundSpecification(ExchangeDocument Document, Uri Address) : RemoteImport;
