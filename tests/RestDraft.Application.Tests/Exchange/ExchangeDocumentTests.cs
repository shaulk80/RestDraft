using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;

namespace RestDraft.Application.Tests.Exchange;

[TestFixture]
public class ExchangeDocumentTests
{
    [TestCase("https://api.example.com/v1")]
    [TestCase("  https://api.example.com/v1/  ")]
    public void TheBaseUrlGoesInFrontOfEveryRequestUrl(string baseUrl)
    {
        var nested = new RequestDefinition { Id = Guid.NewGuid(), Name = "Nested", Url = "/a/{{id}}" };
        var loose = new RequestDefinition { Id = Guid.NewGuid(), Name = "Loose", Url = "/" };
        var folder = new Folder(Guid.NewGuid(), "F", [], [nested]);
        var collection = new Collection(Guid.NewGuid(), "C", new Folder(Guid.NewGuid(), string.Empty, [folder], [loose]));
        var document = new ExchangeDocument(collection, [new ImportWarning("w")], "/v1");

        var addressed = document.WithBaseUrl(baseUrl);

        var root = addressed.Collection.Root;
        Assert.That(root.Folders.Single().Requests.Single(), Is.EqualTo(nested with { Url = "https://api.example.com/v1/a/{{id}}" }));
        Assert.That(root.Requests.Single().Url, Is.EqualTo("https://api.example.com/v1/"));
        Assert.That(addressed.Collection.Id, Is.EqualTo(collection.Id));
        Assert.That(addressed.BaseUrl, Is.Null);
        Assert.That(addressed.Warnings, Is.SameAs(document.Warnings));
    }
}
