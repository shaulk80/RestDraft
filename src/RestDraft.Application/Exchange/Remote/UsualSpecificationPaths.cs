namespace RestDraft.Application.Exchange.Remote;

// Where the common tools serve a specification, tried when a page does not lead to one.
internal static class UsualSpecificationPaths
{
    private static readonly string[] Paths =
    [
        "swagger/v1/swagger.json",
        "swagger.json",
        "openapi.json",
        "openapi/v1.json",
        "v3/api-docs",
    ];

    // Each path under the folder of the page and under the root of its host.
    public static IEnumerable<Uri> For(Uri page)
    {
        var folder = new Uri(page, "./");
        var root = new Uri(page, "/");
        return Paths.SelectMany(path => new[] { new Uri(folder, path), new Uri(root, path) });
    }
}
