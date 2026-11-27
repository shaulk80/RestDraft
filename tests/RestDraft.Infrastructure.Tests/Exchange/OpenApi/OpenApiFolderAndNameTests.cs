using NUnit.Framework;
using RestDraft.Domain.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

[TestFixture]
public class OpenApiFolderAndNameTests
{
    private static readonly string[] DeclaredThenUsedFolders = ["zoo", "admin", "extra"];
    private static readonly string[] NamesInTheFolder = ["Same", "same 2", "Same 3"];
    private static readonly string[] NamesAtTheRoot = ["Same 2", "Same 3"];
    private static readonly string[] UntaggedUrls = ["/d", "/e"];
    private static readonly int[] OneRequestEach = [1, 1, 1];

    [TestCase("""{"summary":"  List\n all  ","operationId":"op"}""", "List all")]
    [TestCase("""{"summary":"","operationId":"listItems"}""", "listItems")]
    [TestCase("{}", "POST /items")]
    public void TheNameIsTheSummaryOrTheOperationIdOrTheMethodAndPath(string operation, string expected)
    {
        var request = SingleRequest(Read(Operation(operation)));

        Assert.That(request.Name, Is.EqualTo(expected));
    }

    [Test]
    public void FoldersFollowTheDeclaredTagsThenTheOrderOfUse()
    {
        var document = Read(Json(
            """
            {
              "/a": { "get": { "tags": ["admin", "zoo"] } },
              "/b": { "get": { "tags": ["extra"] } },
              "/c": { "get": { "tags": ["zoo"] } },
              "/d": { "get": { } },
              "/e": { "get": { "tags": [""] } }
            }
            """,
            """ "tags": [{ "name": "zoo" }, { "name": "admin" }, { "name": "unused" }] """));

        var root = document.Collection.Root;
        Assert.That(root.Folders.Select(folder => folder.Name), Is.EqualTo(DeclaredThenUsedFolders));
        Assert.That(root.Folders.Select(folder => folder.Requests.Count), Is.EqualTo(OneRequestEach));
        Assert.That(root.Requests.Select(request => request.Url), Is.EqualTo(UntaggedUrls));
    }

    [Test]
    public void NamesThatAreTakenGetANumberAlsoAgainstAFolder()
    {
        var document = Read(Json("""
            {
              "/a": { "get": { "tags": ["Same"], "summary": "Same" }, "post": { "tags": ["same"], "summary": "same" } },
              "/b": { "get": { "tags": ["Same"], "summary": "Same" } },
              "/c": { "get": { "summary": "Same" }, "post": { "summary": "Same" } }
            }
            """));

        var folder = document.Collection.Root.Folders.Single();
        Assert.That(folder.Requests.Select(request => request.Name), Is.EqualTo(NamesInTheFolder));
        Assert.That(document.Collection.Root.Requests.Select(request => request.Name), Is.EqualTo(NamesAtTheRoot));
    }

    [TestCase("get", RequestMethod.Get)]
    [TestCase("put", RequestMethod.Put)]
    [TestCase("post", RequestMethod.Post)]
    [TestCase("delete", RequestMethod.Delete)]
    [TestCase("options", RequestMethod.Options)]
    [TestCase("head", RequestMethod.Head)]
    [TestCase("patch", RequestMethod.Patch)]
    public void EveryMethodThatRestDraftHasIsImported(string method, RequestMethod expected)
    {
        var request = SingleRequest(Read(Json("{\"/a\":{\"" + method + "\":{}}}")));

        Assert.That(request.Method, Is.EqualTo(expected));
    }

    [Test]
    public void AMethodThatRestDraftDoesNotHaveIsSkippedWithAWarning()
    {
        var document = Read(Json("""{ "/a": { "trace": {}, "get": {} } }"""));

        var warning = document.Warnings.Single(candidate => candidate.Message.Contains("TRACE", StringComparison.Ordinal));
        Assert.That(SingleRequest(document).Method, Is.EqualTo(RequestMethod.Get));
        Assert.That(warning.Path, Is.EqualTo("TRACE /a"));
    }

    [TestCase("""{"openapi":"3.0.3","info":{"title":"  My API ","version":"1"},"paths":{}}""", "My API")]
    [TestCase("""{"openapi":"3.0.3","info":{"title":"","version":"1"},"paths":{}}""", "Imported collection")]
    public void TheCollectionIsNamedAfterTheTitle(string specification, string expected)
    {
        Assert.That(Read(specification).Collection.Name, Is.EqualTo(expected));
    }
}
