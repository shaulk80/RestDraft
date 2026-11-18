namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

internal sealed class InsomniaBodyDto
{
    public string? MimeType { get; set; }

    public string? Text { get; set; }

    public string? FileName { get; set; }

    public List<InsomniaParameterDto>? Params { get; set; }
}
