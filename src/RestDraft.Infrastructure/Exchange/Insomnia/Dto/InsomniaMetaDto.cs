namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

internal sealed class InsomniaMetaDto
{
    public string? Id { get; set; }

    public long? Created { get; set; }

    public long? Modified { get; set; }

    public bool? IsPrivate { get; set; }

    public string? Description { get; set; }

    public double? SortKey { get; set; }
}
