using System.Text.RegularExpressions;
using NUnit.Framework;
using RestDraft.Infrastructure.Tests.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.OpenApi.OpenApiFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

// Every version and notation of the same API gives the same collection.
[TestFixture]
public partial class OpenApiFixtureTests
{
    private const string PetBody = "{\n  \"name\": \"Rex\",\n  \"tags\": [\n    \"string\"\n  ],\n  \"born\": \"2026-01-01\",\n"
        + "  \"owner\": {\n    \"email\": \"user@example.com\",\n    \"pets\": []\n  }\n}";

    private const string KeyAuth = "apikey(X-Api-Key|{{apiKey}}|Header)";

    private static readonly string[] Expected =
    [
        "folder:collection:Pet API",
        "request:collection:Pet API/Health method=Get url=/health query=[] headers=[] body=none auth=none",
        "folder:collection:Pet API/pets",
        "request:collection:Pet API/pets/List pets method=Get url=/pets query=[limit=10:False;status=:True] "
            + $"headers=[X-Trace=abc:False] body=none auth={KeyAuth}",
        "request:collection:Pet API/pets/Add a pet method=Post url=/pets query=[] headers=[] "
            + $"body=raw(application/json|{PetBody}) auth=basic({{{{username}}}}|{{{{password}}}})",
        $"request:collection:Pet API/pets/getPet method=Get url=/pets/{{{{petId}}}} query=[] headers=[] body=none auth={KeyAuth}",
        "request:collection:Pet API/pets/Upload a photo method=Post url=/pets/{{petId}}/photo query=[] headers=[] "
            + $"body=multipart(photo=@;caption=A photo@) auth={KeyAuth}",
    ];

    private static readonly string[] ExpectedWarnings = ["Response definitions are not imported."];

    [TestCase(OpenApiFixtures.Swagger2Yaml, TestName = "Swagger 2.0, YAML")]
    [TestCase(OpenApiFixtures.Swagger2Json, TestName = "Swagger 2.0, JSON")]
    [TestCase(OpenApiFixtures.OpenApi30Yaml, TestName = "OpenAPI 3.0, YAML")]
    [TestCase(OpenApiFixtures.OpenApi30Json, TestName = "OpenAPI 3.0, JSON")]
    [TestCase(OpenApiFixtures.OpenApi31Yaml, TestName = "OpenAPI 3.1, YAML")]
    [TestCase(OpenApiFixtures.OpenApi31Json, TestName = "OpenAPI 3.1, JSON")]
    public void ReadsTheSameCollectionFromEveryVersionAndNotation(string specification)
    {
        var format = Create();

        var recognised = format.CanRead(specification);
        var document = Read(specification);

        Assert.That(recognised.Value, Is.True);
        var described = CollectionDescriber.Describe([document.Collection]).Select(line => IdPattern().Replace(line, string.Empty));
        Assert.That(described, Is.EqualTo(Expected));
        Assert.That(document.BaseUrl, Is.EqualTo("https://api.example.com/v1"));
        Assert.That(Messages(document), Is.EqualTo(ExpectedWarnings));
    }

    [GeneratedRegex(@"id=[0-9a-f-]+ ")]
    private static partial Regex IdPattern();
}
