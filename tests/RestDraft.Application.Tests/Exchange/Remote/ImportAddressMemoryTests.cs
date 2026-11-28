using NUnit.Framework;
using RestDraft.Application.Exchange.Remote;
using RestDraft.Application.Settings;
using RestDraft.Application.Tests.Secrets;

namespace RestDraft.Application.Tests.Exchange.Remote;

[TestFixture]
public class ImportAddressMemoryTests
{
    private InMemorySettingsStore _store = null!;
    private SettingsService _settings = null!;
    private ImportAddressMemory _memory = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemorySettingsStore();
        _settings = new SettingsService(new AppSettingsStorage(_store));
        _memory = new ImportAddressMemory(_settings);
    }

    [TearDown]
    public void TearDown()
    {
        _settings.Dispose();
    }

    [Test]
    public async Task NothingIsRememberedAtFirst()
    {
        var recalled = await _memory.RecallAsync(CancellationToken.None);

        Assert.That(recalled.Value, Is.EqualTo(new RememberedImport(string.Empty, string.Empty)));
    }

    [TestCase("{{base_url}}/v1", "{{base_url}}/v1")]
    [TestCase(null, "")]
    public async Task TheAddressIsRememberedWithoutAUserNameOrPassword(string? baseUrl, string expectedBaseUrl)
    {
        var address = new Uri("https://ann:secret@api.test/swagger?group=a");

        var remembered = await _memory.RememberAsync(address, baseUrl, CancellationToken.None);
        var recalled = await _memory.RecallAsync(CancellationToken.None);

        Assert.That(remembered.IsSuccess, Is.True);
        Assert.That(recalled.Value, Is.EqualTo(new RememberedImport("https://api.test/swagger?group=a", expectedBaseUrl)));
        Assert.That(_store.Values.Values, Has.None.Contains("secret"));
    }

    [Test]
    public async Task OtherSettingsAreKept()
    {
        await _settings.SaveAsync(new AppSettings { TimeoutSeconds = 7 }, CancellationToken.None);

        await _memory.RememberAsync(new Uri("http://api.test/"), "http://api.test", CancellationToken.None);

        var settings = await _settings.GetAsync(CancellationToken.None);
        Assert.That(settings.Value?.TimeoutSeconds, Is.EqualTo(7));
    }

    [TestCase("http://api.test/swagger", "http://h/v1", "http://h/v1")]
    [TestCase("http://ann:secret@API.test:80/swagger", "http://h/v1", "http://h/v1")]
    [TestCase("http://api.test/Swagger", "http://h/v1", null)]
    [TestCase("http://api.test/swagger/", "http://h/v1", null)]
    [TestCase("https://api.test/swagger", "http://h/v1", null)]
    [TestCase("http://api.test/swagger", "", null)]
    public void TheBaseUrlIsOfferedAgainOnlyForTheSameAddress(string address, string baseUrl, string? expected)
    {
        var remembered = new RememberedImport("http://api.test/swagger", baseUrl);

        Assert.That(remembered.BaseUrlFor(new Uri(address)), Is.EqualTo(expected));
    }
}
