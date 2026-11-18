using NUnit.Framework;
using RestDraft.Domain.Auth;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.InsomniaFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange;

[TestFixture]
public class InsomniaV5FormatReadTests
{
    private static readonly string[] SortedNames = ["First", "Second", "Third", "NoKey"];

    [Test]
    public void ReadsTheCollectionNameAndSortsSiblingsBySortKey()
    {
        var document = Read(Yaml("""
              - name: Third
                url: http://x/3
                method: GET
                meta: { sortKey: 5 }
              - name: NoKey
                url: http://x/4
                method: GET
              - name: Second
                url: http://x/2
                method: GET
                meta: { sortKey: -2.5 }
              - name: First
                url: http://x/1
                method: GET
                meta: { sortKey: -1754299537461 }
            """));

        Assert.That(document.Collection.Name, Is.EqualTo("Scratch Pad"));
        Assert.That(document.Collection.Root.Requests.Select(request => request.Name), Is.EqualTo(SortedNames));
    }

    [Test]
    public void FoldersNestAndHoldRequests()
    {
        var document = Read(Yaml("""
              - name: Outer
                children:
                  - name: Inner
                    children:
                      - name: Ping
                        url: http://x
                        method: HEAD
              - name: Loose
                url: http://x
                method: delete
            """));

        var root = document.Collection.Root;
        var inner = root.Folders.Single().Folders.Single();
        Assert.That(root.Name, Is.Empty);
        Assert.That(inner.Name, Is.EqualTo("Inner"));
        Assert.That(inner.Requests.Single().Method, Is.EqualTo(RequestMethod.Head));
        Assert.That(root.Requests.Single().Method, Is.EqualTo(RequestMethod.Delete));
    }

    [Test]
    public void ParametersAndHeadersKeepTheirDisabledFlag()
    {
        var request = ReadRequest("""
                method: GET
                url: http://x
                parameters:
                  - { name: page, value: 2 }
                  - { name: debug, value: "1", disabled: true }
                headers:
                  - { name: Accept, value: text/plain }
                  - { name: X-Off, value: "", disabled: true }
            """);

        Assert.That(request.QueryParams, Is.EqualTo(new[]
        {
            new KeyValueEntry("page", "2", true),
            new KeyValueEntry("debug", "1", false),
        }));
        Assert.That(request.Headers.Select(header => header.IsEnabled), Is.EqualTo(new[] { true, false }));
    }

    [TestCase("application/json", "{\"a\":1}", "application/json")]
    [TestCase("application/xml", "<a/>", "application/xml")]
    [TestCase("text/plain", "hello", "text/plain")]
    [TestCase("application/graphql", "{\"query\":\"{ a }\"}", "application/json")]
    public void RawBodiesKeepTheirTypeAndText(string mimeType, string text, string expectedContentType)
    {
        var request = ReadRequest($"""
                method: POST
                body:
                  mimeType: {mimeType}
                  text: '{text}'
            """);

        Assert.That(request.Body, Is.EqualTo(new RawBody(expectedContentType, text)));
    }

    [Test]
    public void MultilineBodyTextKeepsItsLineBreaks()
    {
        var request = ReadRequest("""
                method: POST
                body:
                  mimeType: application/json
                  text: >-
                    {
                    	"a": 1
                    }
            """);

        Assert.That(((RawBody)request.Body).Text, Does.Contain("\n").And.Contain("\"a\": 1"));
    }

    [Test]
    public void FormBodyBecomesFormFields()
    {
        var request = ReadRequest("""
                method: POST
                body:
                  mimeType: application/x-www-form-urlencoded
                  params:
                    - { name: a, value: b }
                    - { name: c, value: d, disabled: true }
            """);

        Assert.That(((FormUrlEncodedBody)request.Body).Fields, Is.EqualTo(new[]
        {
            new KeyValueEntry("a", "b", true),
            new KeyValueEntry("c", "d", false),
        }));
    }

    [Test]
    public void MultipartBodyKeepsTextAndFileParts()
    {
        var document = Read(Yaml("""
              - name: Upload
                method: POST
                body:
                  mimeType: multipart/form-data
                  params:
                    - { name: title, value: Report }
                    - { name: doc, type: file, fileName: 'C:\data\report.pdf' }
                    - { name: off, value: x, disabled: true }
            """));

        var body = (MultipartBody)document.Collection.Root.Requests.Single().Body;
        Assert.That(body.Parts, Is.EqualTo(new[]
        {
            new MultipartPart("title", "Report", null),
            new MultipartPart("doc", null, @"C:\data\report.pdf"),
        }));
        Assert.That(document.Warnings.Select(warning => warning.Message), Has.One.Contain("Disabled multipart"));
    }

    [Test]
    public void AFileBodyIsDroppedWithAWarning()
    {
        var document = Read(Yaml("""
              - name: Binary
                method: POST
                body:
                  mimeType: application/octet-stream
                  fileName: 'C:\data\x.bin'
            """));

        Assert.That(document.Collection.Root.Requests.Single().Body, Is.TypeOf<NoBody>());
        Assert.That(document.Warnings.Select(warning => warning.Message), Has.One.Contain("file is not supported"));
    }

    [TestCase("type: basic\n      username: u\n      password: p", typeof(BasicAuth))]
    [TestCase("type: bearer\n      token: t", typeof(BearerAuth))]
    [TestCase("type: apikey\n      key: k\n      value: v\n      addTo: header", typeof(ApiKeyAuth))]
    [TestCase("type: bearer\n      token: t\n      disabled: true", typeof(NoAuth))]
    [TestCase("type: none", typeof(NoAuth))]
    public void AuthenticationMapsToTheMatchingKind(string authentication, Type expected)
    {
        var request = ReadRequest($"    method: GET\n    authentication:\n      {authentication}");

        Assert.That(request.Auth, Is.TypeOf(expected));
    }

    [TestCase("header", ApiKeyLocation.Header)]
    [TestCase("queryParams", ApiKeyLocation.Query)]
    public void ApiKeyLocationFollowsAddTo(string addTo, ApiKeyLocation expected)
    {
        var request = ReadRequest($"    method: GET\n    authentication:\n      type: apikey\n      key: k\n      value: v\n      addTo: {addTo}");

        Assert.That(request.Auth, Is.EqualTo(new ApiKeyAuth("k", "v", expected)));
    }

    [Test]
    public void AuthenticationOfAnotherTypeIsDroppedWithAWarning()
    {
        var document = Read(Yaml("""
              - name: Secure
                method: GET
                authentication:
                  type: oauth2
            """));

        Assert.That(document.Collection.Root.Requests.Single().Auth, Is.TypeOf<NoAuth>());
        Assert.That(document.Warnings.Single().Message, Does.Contain("oauth2"));
        Assert.That(document.Warnings.Single().Path, Is.EqualTo("Secure"));
    }

    [Test]
    public void ARequestWithAnUnsupportedMethodIsSkippedWithAWarning()
    {
        var document = Read(Yaml("""
              - name: Trace it
                method: TRACE
              - name: Fine
                method: GET
            """));

        Assert.That(document.Collection.Root.Requests.Select(request => request.Name), Is.EqualTo(new[] { "Fine" }));
        Assert.That(document.Warnings.Single().Message, Does.Contain("TRACE"));
    }

    [Test]
    public void VariablesAreConvertedInEveryPlace()
    {
        var request = ReadRequest("""
                method: POST
                url: '{{ _.base }}/items/{{_.id}}'
                parameters:
                  - { name: '{{ _.key }}', value: '{{ _.value }}' }
                headers:
                  - { name: X-Token, value: 'Bearer {{ _.token }}' }
                body:
                  mimeType: application/json
                  text: '{"id": "{{ _.id }}"}'
                authentication:
                  type: basic
                  username: '{{ _.user }}'
                  password: '{{ _.pass }}'
            """);

        Assert.That(request.Url, Is.EqualTo("{{base}}/items/{{id}}"));
        Assert.That(request.QueryParams.Single(), Is.EqualTo(new KeyValueEntry("{{key}}", "{{value}}", true)));
        Assert.That(request.Headers.Single().Value, Is.EqualTo("Bearer {{token}}"));
        Assert.That(((RawBody)request.Body).Text, Is.EqualTo("{\"id\": \"{{id}}\"}"));
        Assert.That(request.Auth, Is.EqualTo(new BasicAuth("{{user}}", "{{pass}}")));
    }

    [TestCase("'{{ _.a.b }}'")]
    [TestCase("'{% uuid \"v4\" %}'")]
    [TestCase("'{{ now | date }}'")]
    public void TagsThatCannotBeConvertedAreKeptAsTextWithOneWarning(string url)
    {
        var document = Read(Yaml($"  - name: Tagged\n    method: GET\n    url: {url}\n"));

        Assert.That(document.Collection.Root.Requests.Single().Url, Does.Contain("{"));
        Assert.That(document.Warnings.Single().Message, Does.Contain("cannot be converted"));
        Assert.That(document.Warnings.Single().Path, Is.EqualTo("Tagged"));
    }

    [Test]
    public void DuplicateAndEmptyNamesAreMadeUniqueWithAWarning()
    {
        var document = Read(Yaml("""
              - { name: Same, method: GET }
              - { name: same, method: GET }
              - { name: Same, children: [] }
              - { method: GET }
            """));

        var root = document.Collection.Root;
        var names = root.Folders.Select(folder => folder.Name).Concat(root.Requests.Select(request => request.Name));
        Assert.That(names, Is.EquivalentTo(new[] { "Same", "same 2", "Same 3", "Untitled" }));
        Assert.That(document.Warnings, Has.Count.EqualTo(2));
    }

    [Test]
    public void FolderHeadersAndAuthenticationGiveOneWarningForTheFile()
    {
        var document = Read(Yaml("""
              - name: One
                headers: [{ name: A, value: b }]
                children: []
              - name: Two
                authentication: { type: bearer, token: t }
                children: []
            """));

        Assert.That(document.Warnings.Select(warning => warning.Message), Has.One.Contain("Some folders have headers"));
    }

    [Test]
    public void DroppedThingsAreSummarisedOncePerKind()
    {
        var document = Read("""
            type: collection.insomnia.rest/5.0
            name: X
            collection:
              - { name: A, method: GET, settings: { encodeUrl: false } }
              - { name: B, method: GET, settings: { encodeUrl: false } }
              - { name: C, method: GET, settings: { encodeUrl: true, followRedirects: global, cookies: { send: true, store: true } } }
              - { name: D, method: GET, scripts: { preRequest: "x" } }
            environments: { name: Base }
            """);

        var messages = document.Warnings.Select(warning => warning.Message).ToList();
        Assert.That(messages, Has.One.Contain("2 requests have settings"));
        Assert.That(messages, Has.One.Contain("1 item has scripts"));
        Assert.That(messages, Has.One.Contain("environments"));
    }

    [Test]
    public void UnknownKeysAndNewerMinorVersionsAreAccepted()
    {
        var format = Create();
        var text = Yaml("  - { name: A, method: GET, somethingNew: 1 }\n", "collection.insomnia.rest/5.9") + "future: [1, 2]\n";

        var canRead = format.CanRead(text);
        var read = format.Read(text);

        Assert.That(canRead.Value, Is.True);
        Assert.That(read.Value!.Collection.Root.Requests, Has.Count.EqualTo(1));
    }

    [TestCase("type: collection.insomnia.rest/4.0\nname: x")]
    [TestCase("{ \"_type\": \"export\", \"__export_format\": 4 }")]
    [TestCase("name: no type")]
    [TestCase("just text")]
    [TestCase("a: [unclosed")]
    [TestCase("")]
    public void OtherFilesAreNotRecognised(string text)
    {
        Assert.That(Create().CanRead(text).Value, Is.False);
    }

    [TestCase("a: [unclosed", "not valid YAML")]
    [TestCase("base: &a { x: 1 }\ncopy: *a", "aliases")]
    public void ReadRefusesBrokenOrRiskyYaml(string text, string expectedInMessage)
    {
        var result = Create().Read(Yaml("  - { name: A }\n") + text + "\n");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error?.Message, Does.Contain(expectedInMessage));
    }

    [Test]
    public void ReadRefusesFoldersNestedTooDeep()
    {
        var items = string.Concat(Enumerable.Range(0, 80).Select(level => $"{new string(' ', level * 4 + 2)}- name: F{level}\n{new string(' ', level * 4 + 4)}children:\n"));

        var result = Create().Read(Yaml(items));

        Assert.That(result.Error?.Message, Does.Contain("nested deeper"));
    }

    [Test]
    public void AnEmptyCollectionListGivesAnEmptyCollection()
    {
        var document = Read("type: collection.insomnia.rest/5.0\nname: Empty\ncollection: []\n");

        Assert.That(document.Collection.Root.Folders, Is.Empty);
        Assert.That(document.Collection.Root.Requests, Is.Empty);
    }

    [Test]
    public void ANotYamlCollectionFileFails()
    {
        var result = Create().Read("type: collection.insomnia.rest/5.0\ncollection: not-a-list\n");

        Assert.That(result.IsSuccess, Is.False);
    }

    private static RequestDefinition ReadRequest(string requestBody)
    {
        var document = Read(Yaml($"  - name: Req\n{requestBody.TrimStart('\n')}\n"));
        return document.Collection.Root.Requests.Single();
    }
}
