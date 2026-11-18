namespace RestDraft.Infrastructure.Exchange.Insomnia.Dto;

internal sealed class InsomniaSettingsDto
{
    private const string DefaultFollowRedirects = "global";

    public bool? RenderRequestBody { get; set; }

    public bool? EncodeUrl { get; set; }

    public string? FollowRedirects { get; set; }

    public InsomniaCookieSettingsDto? Cookies { get; set; }

    public bool? RebuildPath { get; set; }

    // The settings of a request that nobody changed. RestDraft writes them, and a file with only these loses nothing.
    public static InsomniaSettingsDto Default()
    {
        return new InsomniaSettingsDto
        {
            RenderRequestBody = true,
            EncodeUrl = true,
            FollowRedirects = DefaultFollowRedirects,
            Cookies = new InsomniaCookieSettingsDto { Send = true, Store = true },
            RebuildPath = true,
        };
    }

    public bool IsDefault()
    {
        return RenderRequestBody is null or true
            && EncodeUrl is null or true
            && FollowRedirects is null or DefaultFollowRedirects
            && Cookies?.Send is null or true
            && Cookies?.Store is null or true
            && RebuildPath is null or true;
    }
}
