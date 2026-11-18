using YamlDotNet.Serialization;

namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

// A folder (it has children) or a request. Insomnia mixes both in one list. The order of the properties is the order in
// the written file.
internal sealed class InsomniaItemDto
{
    public string? Url { get; set; }

    public string? Name { get; set; }

    public InsomniaMetaDto? Meta { get; set; }

    public string? Method { get; set; }

    public InsomniaBodyDto? Body { get; set; }

    public List<InsomniaParameterDto>? Parameters { get; set; }

    public List<InsomniaParameterDto>? Headers { get; set; }

    public InsomniaAuthDto? Authentication { get; set; }

    public InsomniaSettingsDto? Settings { get; set; }

    public object? Scripts { get; set; }

    public List<InsomniaItemDto>? Children { get; set; }

    [YamlIgnore]
    public bool IsFolder => Children is not null;
}
