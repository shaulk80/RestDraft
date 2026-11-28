using NUnit.Framework;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Domain.Collections;

namespace RestDraft.Application.Tests.Exchange.Remote;

[TestFixture]
public class WebAddressTests
{
    [TestCase("http://localhost:5001/swagger", "http://localhost:5001/swagger")]
    [TestCase("  HTTPS://Api.Test/a.json  ", "https://api.test/a.json")]
    public void AnAddressThatNamesHttpOrHttpsIsAccepted(string typed, string expected)
    {
        var parsed = WebAddress.Parse(typed);

        Assert.That(parsed.Value?.AbsoluteUri, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("localhost:5001/swagger")]
    [TestCase("/swagger/v1/swagger.json")]
    [TestCase("//api.test/a")]
    [TestCase("ftp://api.test/a")]
    [TestCase("file:///c:/a.json")]
    [TestCase("http://")]
    [TestCase("http:/api.test")]
    [TestCase("http://a b")]
    public void AnyOtherTextIsRefused(string typed)
    {
        var parsed = WebAddress.Parse(typed);

        Assert.That(parsed.Error?.Message, Is.EqualTo("Enter an address that starts with http:// or https://."));
    }

    [TestCase("http://api.test/a?key=1#x", "http://api.test/a")]
    [TestCase("https://user:secret@api.test:8443/a b", "https://api.test:8443/a%20b")]
    public void AnAddressInAMessageHasNoSecrets(string address, string expected)
    {
        Assert.That(WebAddress.ForDisplay(new Uri(address)), Is.EqualTo(expected));
    }

    [TestCase("http://user:secret@api.test/a?g=1", "http://api.test/a?g=1")]
    [TestCase("https://user@api.test:8443/a#/pet", "https://api.test:8443/a#/pet")]
    [TestCase("https://API.test:443/a", "https://api.test/a")]
    public void AUserNameAndPasswordCanBeTakenOut(string address, string expected)
    {
        Assert.That(WebAddress.WithoutUserInfo(new Uri(address)), Is.EqualTo(expected));
    }

    [TestCase("http://api.test/other?x=1", true)]
    [TestCase("http://API.TEST:80/a", true)]
    [TestCase("http://user:secret@api.test/a", true)]
    [TestCase("https://api.test/a", false)]
    [TestCase("http://api.test:81/a", false)]
    [TestCase("http://other.test/a", false)]
    [TestCase("http://api.test.other.test/a", false)]
    [TestCase("http://sub.api.test/a", false)]
    public void HeadersGoOnlyToTheHostOfTheTypedAddress(string address, bool getsThem)
    {
        var token = new KeyValueEntry("Authorization", "Bearer t", true);
        var headers = new DownloadHeaders([token], new Uri("http://api.test/swagger"));

        var sent = headers.For(new Uri(address));

        Assert.That(sent, getsThem ? Is.EqualTo(new[] { token }) : Is.Empty);
    }

    [TestCase("X-Token", false)]
    [TestCase("", true)]
    [TestCase("   ", true)]
    public void AHeaderThatIsOffOrHasNoNameIsNotSent(string name, bool isEnabled)
    {
        var address = new Uri("http://api.test/");
        var headers = new DownloadHeaders([new KeyValueEntry(name, "value", isEnabled)], address);

        Assert.That(headers.For(address), Is.Empty);
    }
}
