using NUnit.Framework;
using RestDraft.Application.Search;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;

namespace RestDraft.Application.Tests.Search;

[TestFixture]
public class RequestMatcherTests
{
    private static readonly RequestDefinition Login = new()
    {
        Id = Guid.NewGuid(),
        Name = "Login",
        Url = "https://example.com/auth/token",
        Method = RequestMethod.Post,
        Body = new RawBody("application/json", "{\"user\": \"Alice\"}"),
    };

    [TestCase("login", false, true)]
    [TestCase("AUTH", false, true)]
    [TestCase("alice", false, false)]
    [TestCase("alice", true, true)]
    [TestCase("nothing", true, false)]
    [TestCase("", false, true)]
    public void TextMatchesNameUrlAndOnlyIfAskedTheBody(string text, bool inBodies, bool expected)
    {
        var filter = new RequestFilter(text, null, inBodies);

        Assert.That(RequestMatcher.Matches(Login, filter, textMatchedByParent: false), Is.EqualTo(expected));
    }

    [TestCase(RequestMethod.Post, true)]
    [TestCase(RequestMethod.Get, false)]
    public void MethodMustBeEqual(RequestMethod method, bool expected)
    {
        var filter = new RequestFilter(string.Empty, method, false);

        Assert.That(RequestMatcher.Matches(Login, filter, textMatchedByParent: false), Is.EqualTo(expected));
    }

    [Test]
    public void TextMatchedByTheParentStillNeedsTheMethod()
    {
        var filter = new RequestFilter("unrelated", RequestMethod.Get, false);

        Assert.That(RequestMatcher.Matches(Login, filter, textMatchedByParent: true), Is.False);
        Assert.That(RequestMatcher.Matches(Login with { Method = RequestMethod.Get }, filter, textMatchedByParent: true), Is.True);
    }

    [Test]
    public void BodyContainsLooksAtEveryKindOfBody()
    {
        var form = new FormUrlEncodedBody([new KeyValueEntry("color", "red", false)]);
        var multipart = new MultipartBody([new MultipartPart("avatar", "blue", null), new MultipartPart("file", null, "C:\\green.png")]);

        Assert.That(RequestMatcher.BodyContains(form, "COLOR"), Is.True);
        Assert.That(RequestMatcher.BodyContains(form, "red"), Is.True);
        Assert.That(RequestMatcher.BodyContains(multipart, "avatar"), Is.True);
        Assert.That(RequestMatcher.BodyContains(multipart, "blue"), Is.True);
        Assert.That(RequestMatcher.BodyContains(multipart, "green"), Is.False, "a file path is not body text");
        Assert.That(RequestMatcher.BodyContains(new NoBody(), "red"), Is.False);
    }

    [TestCase("", false)]
    [TestCase("shop", true)]
    [TestCase("SHOP", true)]
    [TestCase("other", false)]
    public void NameNeverMatchesWithoutText(string text, bool expected)
    {
        Assert.That(RequestMatcher.NameMatches("Shop", new RequestFilter(text, null, false)), Is.EqualTo(expected));
    }
}
