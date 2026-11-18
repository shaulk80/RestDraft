using FakeItEasy;
using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Infrastructure.Exchange.Insomnia;

namespace RestDraft.Infrastructure.Tests.Exchange;

internal static class InsomniaFormatFactory
{
    public static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    public static InsomniaV5Format Create()
    {
        var ids = A.Fake<IIdGenerator>();
        A.CallTo(() => ids.NewId()).ReturnsLazily(() => Guid.NewGuid());
        var clock = A.Fake<IClock>();
        A.CallTo(() => clock.UtcNow).Returns(Now);
        return new InsomniaV5Format(ids, clock, NullLogger<InsomniaV5Format>.Instance);
    }

    // A file with the given items under "collection:". The items are written with the two spaces of the list.
    public static string Yaml(string items, string type = "collection.insomnia.rest/5.0")
    {
        return $"type: {type}\nschema_version: \"5.1\"\nname: Scratch Pad\nmeta:\n  id: wrk_1\ncollection:\n{items}";
    }

    public static ExchangeDocument Read(string text)
    {
        var result = Create().Read(text);
        Assert.That(result.IsSuccess, Is.True, result.Error?.Message);
        return result.Value!;
    }
}
