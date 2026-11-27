using NUnit.Framework;
using RestDraft.Application.Exchange;

namespace RestDraft.Application.Tests.Exchange;

[TestFixture]
public class BaseUrlResolverTests
{
    // A file has no download address.
    [TestCase("https://api.example.com/v1", "https://api.example.com/v1")]
    [TestCase(" http://h:81/api/ ", "http://h:81/api")]
    [TestCase("https://api.example.com/", "https://api.example.com")]
    [TestCase("https://{{host}}/v1", "https://{{host}}/v1")]
    [TestCase("{{base}}/v2/", "{{base}}/v2")]
    [TestCase("//h.example/api", "https://h.example/api")]
    [TestCase("", "{{base_url}}")]
    [TestCase("/", "{{base_url}}")]
    [TestCase("/api/v3", "{{base_url}}/api/v3")]
    [TestCase("v1", "{{base_url}}/v1")]
    [TestCase("./v1/", "{{base_url}}/v1")]
    [TestCase("https:///api", "{{base_url}}/api")]
    [TestCase("https://", "{{base_url}}")]
    public void ForAFile(string declared, string expected)
    {
        Assert.That(BaseUrlResolver.Resolve(declared, null), Is.EqualTo(expected));
    }

    [TestCase("https://api.example.com/v1", "https://api.example.com/v1")]
    [TestCase("", "http://docs.example:8080")]
    [TestCase("/", "http://docs.example:8080")]
    [TestCase("/api", "http://docs.example:8080/api")]
    [TestCase("v1", "http://docs.example:8080/swagger/v1/v1")]
    [TestCase("../api/", "http://docs.example:8080/swagger/api")]
    [TestCase("//other.example/api", "http://other.example/api")]
    [TestCase("https:///api", "http://docs.example:8080/api")]
    public void ForADownload(string declared, string expected)
    {
        var downloaded = new Uri("http://user:secret@docs.example:8080/swagger/v1/swagger.json?x=1");

        Assert.That(BaseUrlResolver.Resolve(declared, downloaded), Is.EqualTo(expected));
    }
}
