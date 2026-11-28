using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RestDraft.Application.Exchange.Remote;

// Finds where the specification of a Swagger UI or Redoc page is. It reads the page, a script of it or a configuration
// file, and gives the links as they are written; it loads nothing.
public static partial class SwaggerPageScanner
{
    private const char ByteOrderMark = '\uFEFF';
    private const string CommentStart = "<!--";
    private const string CommentEnd = "-->";
    private const string ScriptEndTag = "</script";
    private const string ConfigurationKey = "configUrl";
    private const string ScriptExtension = ".js";
    private const string MinifiedExtension = ".min.js";
    private const string DefaultScript = "index.js";

    private static readonly string[] ScriptNameParts = ["init", "config"];

    // Markup rather than a specification, a script or a configuration file.
    public static bool IsPage(string text)
    {
        return text.AsSpan().TrimStart(ByteOrderMark).TrimStart().StartsWith("<");
    }

    public static SwaggerPageLinks Scan(string text)
    {
        return IsPage(text) ? ScanPage(text) : ScanScript(text);
    }

    private static SwaggerPageLinks ScanPage(string text)
    {
        var page = WithoutMarkupComments(text);
        var links = new SwaggerPageLinks
        {
            Direct = SpecUrlAttribute().Matches(page).Select(match => new PageLink(AttributeValue(match))).ToList(),
        };
        var scriptEnd = 0;
        foreach (Match tag in ScriptTag().Matches(page))
        {
            if (tag.Index < scriptEnd)
            {
                continue;
            }

            var source = SourceAttribute().Match(tag.Groups["attributes"].Value);
            if (source.Success && IsWorthReading(AttributeValue(source)))
            {
                links = links.With(new SwaggerPageLinks { Scripts = [AttributeValue(source)] });
            }

            var contentStart = tag.Index + tag.Length;
            var closing = page.IndexOf(ScriptEndTag, contentStart, StringComparison.OrdinalIgnoreCase);
            scriptEnd = closing < 0 ? page.Length : closing;
            links = links.With(ScanScript(page[contentStart..scriptEnd]));
        }

        return links;
    }

    private static SwaggerPageLinks ScanScript(string script)
    {
        // The embedded specifications and the lists go first: the links inside them are not links on their own.
        var embedded = ScriptText.TakeBlocks(ScriptText.WithoutCommentLines(script), EmbeddedStart());
        var lists = ScriptText.TakeBlocks(embedded.Rest, ListStart());
        var properties = LinkProperty().Matches(lists.Rest).Where(match => Value(match).Length > 0).ToList();
        return new SwaggerPageLinks
        {
            Embedded = embedded.Blocks,
            Listed = lists.Blocks.SelectMany(ReadList).ToList(),
            Direct = properties.Where(match => !IsConfiguration(match)).Select(match => new PageLink(Value(match))).ToList(),
            ByHost = HostPair().Matches(lists.Rest).Select(ToHostLink).ToList(),
            Configurations = properties.Where(IsConfiguration).Select(Value).ToList(),
        };
    }

    // The entries of a list such as [{"url": "v1/swagger.json", "name": "My API V1"}].
    private static IEnumerable<PageLink> ReadList(string list)
    {
        foreach (var entry in ScriptText.TakeBlocks(list[1..], ObjectStart()).Blocks)
        {
            var link = LinkProperty().Match(entry);
            if (link.Success && Value(link).Length > 0)
            {
                var name = NameProperty().Match(entry);
                yield return new PageLink(Value(link), name.Success ? Value(name) : string.Empty);
            }
        }
    }

    private static HostLink ToHostLink(Match pair)
    {
        return new HostLink(pair.Groups["host"].Value, pair.Groups["address"].Value);
    }

    // The scripts of the tools themselves are large and hold no address of the specification.
    private static bool IsWorthReading(string source)
    {
        var path = source.Split('?', '#')[0];
        var name = path[(path.LastIndexOf('/') + 1)..];
        if (!name.EndsWith(ScriptExtension, StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(MinifiedExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return name.Equals(DefaultScript, StringComparison.OrdinalIgnoreCase)
            || ScriptNameParts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string WithoutMarkupComments(string page)
    {
        var kept = new StringBuilder();
        var from = 0;
        while (from < page.Length)
        {
            var start = page.IndexOf(CommentStart, from, StringComparison.Ordinal);
            if (start < 0)
            {
                kept.Append(page, from, page.Length - from);
                break;
            }

            kept.Append(page, from, start - from);
            var end = page.IndexOf(CommentEnd, start + CommentStart.Length, StringComparison.Ordinal);
            from = end < 0 ? page.Length : end + CommentEnd.Length;
        }

        return kept.ToString();
    }

    private static bool IsConfiguration(Match property)
    {
        return property.Groups["key"].Value == ConfigurationKey;
    }

    private static string Value(Match property)
    {
        return ScriptText.Unescape(property.Groups["value"].Value);
    }

    private static string AttributeValue(Match attribute)
    {
        return WebUtility.HtmlDecode(attribute.Groups["value"].Value).Trim();
    }

    // No part of a tag crosses an angle bracket, so a broken page cannot make the search slow.
    [GeneratedRegex(@"<script\b(?<attributes>[^<>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTag();

    [GeneratedRegex(@"(?<![\w-])src\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s""'>]+))", RegexOptions.IgnoreCase)]
    private static partial Regex SourceAttribute();

    [GeneratedRegex(@"(?<![\w-])spec-url\s*=\s*(?:""(?<value>[^""<>]+)""|'(?<value>[^'<>]+)')", RegexOptions.IgnoreCase)]
    private static partial Regex SpecUrlAttribute();

    // A specification as an object: "swaggerDoc" of swagger-ui-express, or the "spec" option of Swagger UI.
    [GeneratedRegex(@"(?<![\w$.\-])[""']?(?:swaggerDoc|spec)[""']?\s*:\s*\{")]
    private static partial Regex EmbeddedStart();

    [GeneratedRegex(@"(?<![\w$.\-])[""']?(?:urls|swaggerUrls)[""']?\s*:\s*\[")]
    private static partial Regex ListStart();

    [GeneratedRegex(@"\{")]
    private static partial Regex ObjectStart();

    // A property with a text as its whole value; a text that something is added to is only a part of an address.
    [GeneratedRegex(@"(?<![\w$.\-])[""']?(?<key>url|swaggerUrl|configUrl)[""']?\s*:\s*(?:""(?<value>(?:[^""\\\r\n]|\\.)*)""|'(?<value>(?:[^'\\\r\n]|\\.)*)')(?!\s*\+)")]
    private static partial Regex LinkProperty();

    // One entry of a text such as "a.example=https://a.example/spec.json,b.example=https://b.example/spec.json".
    [GeneratedRegex(@"(?<=[`'"",])(?<host>[A-Za-z0-9.-]+)=(?<address>https?://[^,`'""\s\\]+)")]
    private static partial Regex HostPair();

    [GeneratedRegex(@"(?<![\w$.\-])[""']?name[""']?\s*:\s*(?:""(?<value>(?:[^""\\\r\n]|\\.)*)""|'(?<value>(?:[^'\\\r\n]|\\.)*)')")]
    private static partial Regex NameProperty();
}
