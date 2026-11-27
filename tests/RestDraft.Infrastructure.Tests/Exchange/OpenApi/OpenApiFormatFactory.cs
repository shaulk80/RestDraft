using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.Infrastructure.Exchange.OpenApi;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

internal static class OpenApiFormatFactory
{
    public static OpenApiFormat Create()
    {
        var ids = A.Fake<IIdGenerator>();
        A.CallTo(() => ids.NewId()).ReturnsLazily(() => Guid.NewGuid());
        return new OpenApiFormat(ids, NullLogger<OpenApiFormat>.Instance);
    }

    public static ExchangeDocument Read(string text)
    {
        var result = Create().Read(text);
        Assert.That(result.IsSuccess, Is.True, result.Error?.Message);
        return result.Value!;
    }

    // An OpenAPI 3.0 document in JSON with the given members of "paths", and further top-level members.
    public static string Json(string paths, string more = "")
    {
        var extra = more.Length == 0 ? string.Empty : "," + more;
        return """{"openapi":"3.0.3","info":{"title":"Test API","version":"1"},"paths":""" + paths + extra + "}";
    }

    // The same for one operation, POST /items, with the given members.
    public static string Operation(string members, string more = "")
    {
        return Json("""{"/items":{"post":""" + members + "}}", more);
    }

    public static RequestDefinition SingleRequest(ExchangeDocument document)
    {
        return AllRequests(document.Collection.Root).Single();
    }

    public static IEnumerable<RequestDefinition> AllRequests(Folder folder)
    {
        return folder.Requests.Concat(folder.Folders.SelectMany(AllRequests));
    }

    public static IReadOnlyList<string> Messages(ExchangeDocument document)
    {
        return document.Warnings.Select(warning => warning.Message).ToList();
    }
}
