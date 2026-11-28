namespace RestDraft.Application.Exchange.Remote;

// A link to a specification as a page writes it, which may be relative. Name is empty when the page gives none.
public sealed record PageLink(string Address, string Name = "");
