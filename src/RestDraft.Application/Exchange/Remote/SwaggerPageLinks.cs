namespace RestDraft.Application.Exchange.Remote;

// What a page, a script or a configuration file says about where its specification is.
public sealed record SwaggerPageLinks
{
    public static SwaggerPageLinks None { get; } = new();

    // The entries of a list of specifications.
    public IReadOnlyList<PageLink> Listed { get; init; } = [];

    // Links that stand on their own.
    public IReadOnlyList<PageLink> Direct { get; init; } = [];

    // The entries of a list of host=address pairs, as the public sites of Swagger have it.
    public IReadOnlyList<HostLink> ByHost { get; init; } = [];

    // Files that hold the links, such as the swagger-config of springdoc.
    public IReadOnlyList<string> Configurations { get; init; } = [];

    // Scripts of a page that may hold the links.
    public IReadOnlyList<string> Scripts { get; init; } = [];

    // Specifications written into the page or the script itself.
    public IReadOnlyList<string> Embedded { get; init; } = [];

    // A list of specifications wins over a single link, as in Swagger UI.
    public IReadOnlyList<PageLink> Specifications => Listed.Count > 0 ? Listed : Direct;

    // The links for a page on the host. A list by host counts only when there is no link that holds on every host.
    public IReadOnlyList<PageLink> SpecificationsFor(string host)
    {
        if (Specifications.Count > 0)
        {
            return Specifications;
        }

        return ByHost
            .Where(link => link.Host.Equals(host, StringComparison.OrdinalIgnoreCase))
            .Select(link => new PageLink(link.Address))
            .ToList();
    }

    public SwaggerPageLinks With(SwaggerPageLinks other)
    {
        return new SwaggerPageLinks
        {
            Listed = [.. Listed, .. other.Listed],
            Direct = [.. Direct, .. other.Direct],
            ByHost = [.. ByHost, .. other.ByHost],
            Configurations = [.. Configurations, .. other.Configurations],
            Scripts = [.. Scripts, .. other.Scripts],
            Embedded = [.. Embedded, .. other.Embedded],
        };
    }
}
