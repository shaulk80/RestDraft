using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Application.Requests;
using RestDraft.Application.Settings;
using RestDraft.Application.Workspaces;
using RestDraft.Domain.Collections;
using RestDraft.Infrastructure;
using RestDraft.Infrastructure.Exchange.OpenApi;
using RestDraft.Infrastructure.Http;
using RestDraft.Infrastructure.Persistence;
using RestDraft.Infrastructure.Services;

namespace RestDraft.Integration.Tests.Exchange;

// A specification file is imported into the SQLite database, and a request of it is sent as it comes back from there.
[TestFixture]
public class ImportSpecificationAndSendTests
{
    private const string Specification = """
        openapi: 3.0.3
        info: { title: Shop, version: 1.0 }
        servers:
          - url: /echo/api
        paths:
          /items/{itemId}:
            post:
              tags: [items]
              summary: Update an item
              security:
                - login: []
              parameters:
                - { name: itemId, in: path, required: true, example: 42, schema: { type: integer } }
                - { name: notify, in: query, required: true, schema: { type: boolean, default: true } }
                - { name: trace, in: query, schema: { type: string } }
              requestBody:
                content:
                  application/json:
                    schema:
                      type: object
                      properties:
                        name: { type: string, example: Lamp }
                        price: { type: number, example: 1.50 }
        components:
          securitySchemes:
            login: { type: http, scheme: basic }
        """;

    private TestHttpServer _server = null!;
    private string _folder = string.Empty;
    private string _connectionString = string.Empty;
    private SqliteConnection? _keepAlive;
    private Workspace<Collection> _workspace = null!;

    [SetUp]
    public async Task SetUp()
    {
        _server = new TestHttpServer();
        await _server.StartAsync();
        _folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"Specification{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ConnectionString;
        _keepAlive = new SqliteConnection(_connectionString);
        await _keepAlive.OpenAsync();
        await MigrateAsync();
        var repository = NewRepository();
        _workspace = new Workspace<Collection>(repository.LoadAsync, repository.SaveAsync);
    }

    [TearDown]
    public async Task TearDown()
    {
        _workspace.Dispose();
        _keepAlive?.Dispose();
        Directory.Delete(_folder, recursive: true);
        await _server.DisposeAsync();
    }

    [Test]
    public async Task AnImportedRequestReachesTheServer()
    {
        var path = Path.Combine(_folder, "shop.yaml");
        await File.WriteAllTextAsync(path, Specification);
        var format = new OpenApiFormat(new IdGenerator(), NullLogger<OpenApiFormat>.Instance);
        var files = new LocalFileReader(NullLogger<LocalFileReader>.Instance);
        var import = new ImportService(_workspace, files, new CollectionTextReader([format]));
        var read = await import.ReadAsync(path, CancellationToken.None);
        Assert.That(read.IsSuccess, Is.True, read.Error?.Message);
        var proposed = BaseUrlResolver.Resolve(read.Value!.BaseUrl!, null);
        var baseUrl = proposed.Replace(BaseUrlResolver.BaseUrlVariable, _server.BaseAddress.ToString().TrimEnd('/'), StringComparison.Ordinal);
        var document = read.Value.WithBaseUrl(baseUrl);

        var imported = await import.ApplyAsync(
            document,
            ImportTarget.NewCollection("Shop"),
            false,
            new HashSet<Guid> { document.Collection.Id },
            CancellationToken.None);

        Assert.That(imported.IsSuccess, Is.True, imported.Error?.Message);
        var stored = (await NewRepository().LoadAsync(CancellationToken.None)).Value!.Single();
        var echo = await SendAsync(stored.Root.Folders.Single().Requests.Single());
        Assert.That(echo.Split('\n', 4), Is.EqualTo(new[]
        {
            "POST /echo/api/items/42?notify=true",
            "Basic " + Convert.ToBase64String("ann:secret"u8),
            "application/json",
            "{\n  \"name\": \"Lamp\",\n  \"price\": 1.50\n}",
        }));
    }

    private async Task<string> SendAsync(RequestDefinition definition)
    {
        var variables = new Dictionary<string, string> { ["username"] = "ann", ["password"] = "secret" };
        var settings = new MutableSettingsService(new AppSettings());
        var files = new LocalFileReader(NullLogger<LocalFileReader>.Instance);
        var bodies = new RequestBodyResolver(new MultipartBodyEncoder(files), new IdGenerator());
        var built = await new RequestBuilder(settings, bodies).BuildAsync(definition, variables, CancellationToken.None);
        Assert.That(built.IsSuccess, Is.True, built.Error?.Message);

        var bodyReader = new ResponseBodyReader(
            new InfrastructureOptions(_folder),
            new IdGenerator(),
            NullLogger<ResponseBodyReader>.Instance);
        var executor = new HttpRequestExecutor(bodyReader, settings, NullLogger<HttpRequestExecutor>.Instance);
        var sent = await executor.SendAsync(built.Value!, CancellationToken.None);
        Assert.That(sent.IsSuccess, Is.True, sent.Error?.Message);
        return sent.Value!.BodyPreview!;
    }

    private SqliteCollectionRepository NewRepository()
    {
        return new SqliteCollectionRepository(new SqliteConnectionFactory(_connectionString), NullLogger<SqliteCollectionRepository>.Instance);
    }

    private async Task MigrateAsync()
    {
        var scripts = new SqlScriptMigrationSource(Path.Combine(AppContext.BaseDirectory, "MigrationScripts"), NullLogger<SqlScriptMigrationSource>.Instance);
        var migrations = new MigrationSetBuilder([scripts], NullLogger<MigrationSetBuilder>.Instance);
        var migrator = new SqliteMigrator(new SqliteConnectionFactory(_connectionString), migrations, NullLogger<SqliteMigrator>.Instance);
        var migrated = await migrator.MigrateAsync(CancellationToken.None);
        Assert.That(migrated.IsSuccess, Is.True);
    }
}
