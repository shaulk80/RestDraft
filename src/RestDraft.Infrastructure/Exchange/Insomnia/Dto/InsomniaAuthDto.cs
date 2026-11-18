namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

internal sealed class InsomniaAuthDto
{
    public string? Type { get; set; }

    public bool? Disabled { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Token { get; set; }

    public string? Prefix { get; set; }

    public string? Key { get; set; }

    public string? Value { get; set; }

    public string? AddTo { get; set; }
}
