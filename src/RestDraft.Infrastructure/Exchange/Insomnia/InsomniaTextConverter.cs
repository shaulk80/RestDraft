namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Converts the text of one request and remembers whether something in it could not be converted.
internal sealed class InsomniaTextConverter
{
    public bool HasUnconvertible { get; private set; }

    public string Convert(string? text)
    {
        var converted = InsomniaVariableSyntax.ToRestDraft(text ?? string.Empty);
        if (InsomniaVariableSyntax.HasUnconvertible(converted))
        {
            HasUnconvertible = true;
        }

        return converted;
    }
}
