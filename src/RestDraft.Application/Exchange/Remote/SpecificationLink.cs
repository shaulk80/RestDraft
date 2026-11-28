namespace RestDraft.Application.Exchange.Remote;

// One of the specifications that a page offers.
public sealed record SpecificationLink(string Name, Uri Address)
{
    // A list reads an item to a screen reader by this text. The address may hold a password, so it stays out.
    public override string ToString()
    {
        return Name;
    }
}
