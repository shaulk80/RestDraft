using YamlDotNet.Serialization;

namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

// The top level of an Insomnia v5 collection file. Keys that RestDraft does not use are read only to tell that they exist.
internal sealed class InsomniaCollectionDto
{
    public string? Type { get; set; }

    [YamlMember(Alias = "schema_version", ApplyNamingConventions = false)]
    public string? SchemaVersion { get; set; }

    public string? Name { get; set; }

    public InsomniaMetaDto? Meta { get; set; }

    public List<InsomniaItemDto>? Collection { get; set; }

    public object? Environments { get; set; }

    public object? CookieJar { get; set; }
}
