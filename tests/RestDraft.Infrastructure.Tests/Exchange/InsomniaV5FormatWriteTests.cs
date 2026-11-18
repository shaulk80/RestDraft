using NUnit.Framework;
using RestDraft.Domain.Auth;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.InsomniaFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange;

[TestFixture]
public class InsomniaV5FormatWriteTests
{
    private static readonly string[] SecondThenFirstSortKeysAscending = ["Folder", "Folder request", "Top request"];

    [Test]
    public void WritesTheHeaderThatInsomniaExpects()
    {
        var text = Write(Collection("Shop", Folder(string.Empty)));

        Assert.That(text, Does.StartWith("type: collection.insomnia.rest/5.0\nschema_version: \"5.1\"\nname: Shop\n").Or.StartWith("type: collection.insomnia.rest/5.0\r\nschema_version: \"5.1\"\r\nname: Shop\r\n"));
        Assert.That(text, Does.Contain("id: wrk_"));
        Assert.That(text, Does.Contain("collection: []"));
    }

    [Test]
    public void WritesIdsInInsomniaStyleAndTheExportTime()
    {
        var folder = Folder("Dir", Request("Ping"));
        var text = Write(Collection("Shop", Folder(string.Empty, [folder], [])));

        var created = Now.ToUnixTimeMilliseconds();
        Assert.That(text, Does.Match(@"id: fld_[0-9a-f]{32}"));
        Assert.That(text, Does.Match(@"id: req_[0-9a-f]{32}"));
        Assert.That(text, Does.Contain($"created: {created}"));
        Assert.That(text, Does.Contain($"modified: {created}"));
    }

    [Test]
    public void WritesSettingsAndNoUserAgentHeader()
    {
        var text = Write(Collection("Shop", Folder(string.Empty, [], [Request("Ping")])));

        Assert.That(text, Does.Contain("encodeUrl: true"));
        Assert.That(text, Does.Contain("followRedirects: global"));
        Assert.That(text, Does.Not.Contain("User-Agent"));
    }

    [Test]
    public void MultilineBodyIsWrittenAsABlock()
    {
        var request = Request("Post") with
        {
            Method = RequestMethod.Post,
            Body = new RawBody("application/json", "{\n  \"a\": 1\n}"),
        };

        var text = Write(Collection("Shop", Folder(string.Empty, [], [request])));

        Assert.That(text, Does.Match(@"text: \|[-+]?\r?\n"));
    }

    [Test]
    public void SortKeysKeepTheTreeOrderFoldersFirst()
    {
        var root = Folder(string.Empty, [Folder("Folder", Request("Folder request"))], [Request("Top request")]);
        var read = Read(Write(Collection("Shop", root)));

        Assert.That(
            read.Collection.Root.Folders.Select(folder => folder.Name)
                .Concat(read.Collection.Root.Folders.SelectMany(folder => folder.Requests).Select(request => request.Name))
                .Concat(read.Collection.Root.Requests.Select(request => request.Name)),
            Is.EqualTo(SecondThenFirstSortKeysAscending));
    }

    [Test]
    public void ARequestOfEveryKindSurvivesAWriteAndARead()
    {
        var requests = new[]
        {
            Request("Plain") with { Url = "{{base}}/a?x=1", Headers = [new KeyValueEntry("Accept", "*/*", true), new KeyValueEntry("X-Off", "1", false)] },
            Request("Query") with { Method = RequestMethod.Options, QueryParams = [new KeyValueEntry("q", "{{term}}", true)] },
            Request("Raw") with { Method = RequestMethod.Put, Body = new RawBody("application/xml", "<a>\n  <b/>\n</a>") },
            Request("Form") with { Method = RequestMethod.Post, Body = new FormUrlEncodedBody([new KeyValueEntry("a", "b", true), new KeyValueEntry("c", "d", false)]) },
            Request("Multipart") with { Method = RequestMethod.Patch, Body = new MultipartBody([new MultipartPart("t", "v", null), new MultipartPart("f", null, "data/x.bin")]) },
            Request("Basic") with { Auth = new BasicAuth("user", "pass") },
            Request("Bearer") with { Auth = new BearerAuth("{{token}}") },
            Request("Key header") with { Auth = new ApiKeyAuth("X-Key", "k", ApiKeyLocation.Header) },
            Request("Key query") with { Auth = new ApiKeyAuth("key", "k", ApiKeyLocation.Query) },
            Request("Number like") with { Url = "true", Headers = [new KeyValueEntry("12", "null", true)] },
        };
        var original = Collection("Shop", Folder(string.Empty, [Folder("Dir", requests[..5]), Folder("Empty")], requests[5..]));

        var read = Read(Write(original)).Collection;

        Assert.That(read.Name, Is.EqualTo("Shop"));
        Assert.That(Shape(read.Root), Is.EqualTo(Shape(original.Root)));
    }

    [Test]
    public void ExportedFileIsRecognisedAsInsomniaV5()
    {
        var format = Create();

        var text = Write(Collection("Shop", Folder(string.Empty, [], [Request("Ping")])));

        Assert.That(format.CanRead(text).Value, Is.True);
    }

    // The part of a tree that a round trip keeps: names and content, without the ids that are new on every read.
    private static string Shape(Folder folder)
    {
        var folders = folder.Folders.Select(Shape);
        var requests = folder.Requests.Select(request => string.Join(
            "|",
            request.Name,
            request.Method,
            request.Url,
            string.Join(";", request.QueryParams),
            string.Join(";", request.Headers),
            Body(request.Body),
            request.Auth));
        return $"{folder.Name}[{string.Join(",", folders.Concat(requests))}]";
    }

    private static string Body(BodyDefinition body)
    {
        return body switch
        {
            FormUrlEncodedBody form => "form:" + string.Join(";", form.Fields),
            MultipartBody multipart => "multipart:" + string.Join(";", multipart.Parts),
            _ => body.ToString() ?? string.Empty,
        };
    }

    private static string Write(Collection collection)
    {
        var result = Create().Write(collection);
        Assert.That(result.IsSuccess, Is.True, result.Error?.Message);
        return result.Value!;
    }

    private static Collection Collection(string name, Folder root)
    {
        return new Collection(Guid.NewGuid(), name, root);
    }

    private static Folder Folder(string name, IReadOnlyList<Folder> folders, IReadOnlyList<RequestDefinition> requests)
    {
        return new Folder(Guid.NewGuid(), name, folders, requests);
    }

    private static Folder Folder(string name, params RequestDefinition[] requests)
    {
        return Folder(name, [], requests);
    }

    private static RequestDefinition Request(string name)
    {
        return new RequestDefinition { Id = Guid.NewGuid(), Name = name };
    }
}
