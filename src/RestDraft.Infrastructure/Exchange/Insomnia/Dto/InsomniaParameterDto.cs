namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

// A header, a query parameter or a form field. A multipart field of type file has a file name.
internal sealed class InsomniaParameterDto
{
    public string? Name { get; set; }

    public string? Value { get; set; }

    public string? Type { get; set; }

    public string? FileName { get; set; }

    public bool? Disabled { get; set; }
}
