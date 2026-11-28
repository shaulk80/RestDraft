using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

// Goes from a page to its specification: through what the page and its scripts say, then through the usual paths. Links
// are taken as a browser takes them, relative to the address of the page, also when a script holds them.
internal sealed class SwaggerPageSearch
{
    private readonly SpecificationSearch _search;
    private readonly DownloadedText _page;

    public SwaggerPageSearch(SpecificationSearch search, DownloadedText page)
    {
        _search = search;
        _page = page;
    }

    // Null when nothing led to a specification.
    public async Task<Result<RemoteImport>?> RunAsync(CancellationToken cancellationToken)
    {
        var links = await ReadLinksAsync(cancellationToken);
        var embedded = links.Embedded
            .Select(text => _search.Read(_page with { Text = text }))
            .FirstOrDefault(read => read is not null);
        if (embedded is not null)
        {
            return embedded;
        }

        var candidates = await ReadCandidatesAsync(links, cancellationToken);
        if (candidates.Count > 1)
        {
            return Result<RemoteImport>.Success(new SpecificationChoices(candidates));
        }

        foreach (var address in candidates.Select(link => link.Address).Concat(UsualSpecificationPaths.For(_page.Address)))
        {
            var found = await _search.TryAsync(address, cancellationToken);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    // The links of the page and of its scripts. A script of another host is not read.
    private async Task<SwaggerPageLinks> ReadLinksAsync(CancellationToken cancellationToken)
    {
        var links = SwaggerPageScanner.Scan(_page.Text);
        var scripts = links.Scripts.Select(Resolve).OfType<Uri>().Where(IsOfThePageHost).ToList();
        foreach (var script in scripts)
        {
            var downloaded = await _search.DownloadAsync(script, cancellationToken);
            if (downloaded is not null && IsOfThePageHost(downloaded.Address))
            {
                links = links.With(SwaggerPageScanner.Scan(downloaded.Text));
            }
        }

        return links;
    }

    // A configuration file, when the page names one, decides what the page offers.
    private async Task<IReadOnlyList<SpecificationLink>> ReadCandidatesAsync(SwaggerPageLinks links, CancellationToken cancellationToken)
    {
        if (links.Configurations.Count == 0)
        {
            return ToCandidates(links.SpecificationsFor(_page.Address.Host));
        }

        var configured = SwaggerPageLinks.None;
        foreach (var address in links.Configurations.Select(Resolve).OfType<Uri>())
        {
            var downloaded = await _search.DownloadAsync(address, cancellationToken);
            if (downloaded is not null)
            {
                configured = configured.With(SwaggerPageScanner.Scan(downloaded.Text));
            }
        }

        var candidates = ToCandidates(configured.Specifications);
        if (candidates.Count > 0)
        {
            return candidates;
        }

        // Beside a configuration file, a link of the page to another host is usually the sample that Swagger UI ships with.
        return ToCandidates(links.Specifications).Where(link => IsOfThePageHost(link.Address)).ToList();
    }

    private List<SpecificationLink> ToCandidates(IReadOnlyList<PageLink> links)
    {
        var candidates = new List<SpecificationLink>();
        foreach (var link in links)
        {
            if (Resolve(link.Address) is { } address && candidates.All(candidate => candidate.Address != address))
            {
                var name = link.Name.Length > 0 ? link.Name : WebAddress.ForDisplay(address);
                candidates.Add(new SpecificationLink(name, address));
            }
        }

        return candidates;
    }

    // Null for a link that does not lead to a web address.
    private Uri? Resolve(string link)
    {
        return Uri.TryCreate(_page.Address, link, out var address) && WebAddress.IsWeb(address) ? address : null;
    }

    private bool IsOfThePageHost(Uri address)
    {
        return WebAddress.IsSameHost(address, _page.Address);
    }
}
