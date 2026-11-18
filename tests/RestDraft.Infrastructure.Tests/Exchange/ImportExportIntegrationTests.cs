using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Application.Workspaces;
using RestDraft.Domain.Collections;
using RestDraft.Infrastructure.Persistence;
using RestDraft.Infrastructure.Services;
using RestDraft.Infrastructure.Tests.Collections;
using static RestDraft.Infrastructure.Tests.Exchange.InsomniaFormatFactory;

namespace RestDraft.Infrastructure.Tests.Exchange;

// Import and export through the real services, files and SQLite repository.
[TestFixture]
public partial class ImportExportIntegrationTests
{
    private const string FixtureYaml = """
        type: collection.insomnia.rest/5.0
        schema_version: "5.1"
        name: Fixture
        collection:
          - name: API
            meta: { sortKey: 1 }
            children:
              - name: List
                meta: { sortKey: 1 }
                method: GET
                url: '{{ _.base }}/items'
                parameters:
                  - { name: page, value: "2" }
                headers:
                  - { name: Accept, value: application/json }
              - name: Create
                meta: { sortKey: 2 }
                method: POST
                url: '{{ _.base }}/items'
                body:
                  mimeType: application/json
                  text: |-
                    {
                      "name": "x"
                    }
                authentication: { type: bearer, token: '{{ _.token }}' }
          - name: Ping
            meta: { sortKey: 2 }
            method: HEAD
            url: http://localhost/ping
        """;

    private string _folder = string.Empty;
    private string _connectionString = string.Empty;
    private SqliteConnection? _keepAlive;
    private SqliteCollectionRepository _repository = null!;
    private Workspace<Collection> _workspace = null!;
    private ImportService _import = null!;
    private ExportService _export = null!;
    private LocalFileWriter _writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "RestDraftTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"ImportExport{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ConnectionString;
        _keepAlive = new SqliteConnection(_connectionString);
        await _keepAlive.OpenAsync();
        await MigrateAsync();

        _repository = new SqliteCollectionRepository(new SqliteConnectionFactory(_connectionString), NullLogger<SqliteCollectionRepository>.Instance);
        _workspace = new Workspace<Collection>(_repository.LoadAsync, _repository.SaveAsync);
        var format = Create();
        _writer = new LocalFileWriter(NullLogger<LocalFileWriter>.Instance);
        _import = new ImportService(_workspace, new LocalFileReader(NullLogger<LocalFileReader>.Instance), [format]);
        _export = new ExportService(_workspace, _writer, [format]);
    }

    [TearDown]
    public void TearDown()
    {
        _workspace.Dispose();
        _keepAlive?.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Test]
    public async Task ImportedCollectionIsStoredAndComesBackAfterAReload()
    {
        var document = await ReadFixtureAsync();

        var outcome = await _import.ApplyAsync(document, ImportTarget.NewCollection("Imported"), false, AllTicked(document), CancellationToken.None);

        var reloaded = (await NewRepository().LoadAsync(CancellationToken.None)).Value!;
        var collection = reloaded.Single();
        Assert.That(outcome.IsSuccess, Is.True, outcome.Error?.Message);
        Assert.That(collection.Name, Is.EqualTo("Imported"));
        Assert.That(collection.Root.Folders.Single().Requests.Select(request => request.Name), Is.EqualTo(new[] { "List", "Create" }));
        Assert.That(collection.Root.Requests.Single().Method, Is.EqualTo(RequestMethod.Head));
        Assert.That(collection.Root.Folders.Single().Requests[0].Url, Is.EqualTo("{{base}}/items"));
    }

    [Test]
    public async Task ExportedFileReadsBackAsTheSameCollection()
    {
        var document = await ReadFixtureAsync();
        await _import.ApplyAsync(document, ImportTarget.NewCollection("Imported"), false, AllTicked(document), CancellationToken.None);
        var stored = (await _workspace.GetAllAsync(CancellationToken.None)).Value!.Single();
        var path = Path.Combine(_folder, "out.yaml");

        var exported = await _export.ExportAsync(stored.Id, new HashSet<Guid> { stored.Id }, path, CancellationToken.None);
        var again = await _import.ReadAsync(path, CancellationToken.None);

        Assert.That(exported.Value, Is.EqualTo(new ExportOutcome(path, 1, 3)));
        Assert.That(WithoutIds(again.Value!.Collection), Is.EqualTo(WithoutIds(stored with { Name = again.Value.Collection.Name })));
        Assert.That(again.Value.Warnings, Is.Empty);
    }

    [Test]
    public async Task ImportingTheSameFileAgainWithOverwriteKeepsTheIdsOfTheRequests()
    {
        var first = await ReadFixtureAsync();
        await _import.ApplyAsync(first, ImportTarget.NewCollection("Imported"), false, AllTicked(first), CancellationToken.None);
        var stored = (await _workspace.GetAllAsync(CancellationToken.None)).Value!.Single();
        var idsBefore = stored.Root.Folders.Single().Requests.Select(request => request.Id).ToList();
        var second = await ReadFixtureAsync();

        var outcome = await _import.ApplyAsync(second, ImportTarget.Existing(stored.Id), true, AllTicked(second), CancellationToken.None);

        var after = (await NewRepository().LoadAsync(CancellationToken.None)).Value!.Single();
        Assert.That(outcome.Value!.Counts, Is.EqualTo(new ImportCounts(0, 0, 3, 0)));
        Assert.That(outcome.Value.OverwrittenRequestIds, Has.Count.EqualTo(3));
        Assert.That(after.Root.Folders.Single().Requests.Select(request => request.Id), Is.EqualTo(idsBefore));
    }

    [Test]
    public async Task ImportingTheSameFileAgainWithoutOverwriteChangesNothing()
    {
        var first = await ReadFixtureAsync();
        await _import.ApplyAsync(first, ImportTarget.NewCollection("Imported"), false, AllTicked(first), CancellationToken.None);
        var stored = (await _workspace.GetAllAsync(CancellationToken.None)).Value!.Single();
        var second = await ReadFixtureAsync();

        var outcome = await _import.ApplyAsync(second, ImportTarget.Existing(stored.Id), false, AllTicked(second), CancellationToken.None);

        Assert.That(outcome.Value!.Counts, Is.EqualTo(new ImportCounts(0, 0, 0, 3)));
        Assert.That(WithoutIds((await NewRepository().LoadAsync(CancellationToken.None)).Value!.Single()), Is.EqualTo(WithoutIds(stored)));
    }

    private async Task<ExchangeDocument> ReadFixtureAsync()
    {
        var path = Path.Combine(_folder, "fixture.yaml");
        await File.WriteAllTextAsync(path, FixtureYaml);
        var read = await _import.ReadAsync(path, CancellationToken.None);
        Assert.That(read.IsSuccess, Is.True, read.Error?.Message);
        return read.Value!;
    }

    private static HashSet<Guid> AllTicked(ExchangeDocument document)
    {
        return [document.Collection.Id];
    }

    private SqliteCollectionRepository NewRepository()
    {
        return new SqliteCollectionRepository(new SqliteConnectionFactory(_connectionString), NullLogger<SqliteCollectionRepository>.Instance);
    }

    private static IReadOnlyList<string> WithoutIds(Collection collection)
    {
        return CollectionDescriber.Describe([collection]).Select(line => IdPattern().Replace(line, string.Empty)).ToList();
    }

    private async Task MigrateAsync()
    {
        var scripts = new SqlScriptMigrationSource(Path.Combine(AppContext.BaseDirectory, "MigrationScripts"), NullLogger<SqlScriptMigrationSource>.Instance);
        var migrations = new MigrationSetBuilder([scripts], NullLogger<MigrationSetBuilder>.Instance);
        var migrator = new SqliteMigrator(new SqliteConnectionFactory(_connectionString), migrations, NullLogger<SqliteMigrator>.Instance);
        var migrated = await migrator.MigrateAsync(CancellationToken.None);
        Assert.That(migrated.IsSuccess, Is.True);
    }

    [GeneratedRegex(@"id=[0-9a-f-]+ ")]
    private static partial Regex IdPattern();
}
