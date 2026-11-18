using FakeItEasy;
using NUnit.Framework;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Application.Tests.Collections;
using RestDraft.Application.Workspaces;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Tests.Exchange;

[TestFixture]
public class ExportServiceTests
{
    private ExchangeFixtures _data = null!;
    private IFileWriter _writer = null!;
    private ICollectionFormat _format = null!;
    private ExportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _data = new ExchangeFixtures();
        _writer = A.Fake<IFileWriter>();
        _format = A.Fake<ICollectionFormat>();
        A.CallTo(() => _writer.WriteAllTextAsync(A<string>._, A<string>._, A<CancellationToken>._)).Returns(Result.Success());
        A.CallTo(() => _format.Write(A<Collection>._)).Returns(Result<string>.Success("text"));
        var repository = CollectionRepositoryFake.Create(new CollectionStore([_data.Source]));
        _service = new ExportService(new Workspace<Collection>(repository.LoadAsync, repository.SaveAsync), _writer, [_format]);
    }

    [Test]
    public async Task ExportWritesOnlyTheTickedPart()
    {
        var ticked = new HashSet<Guid> { _data.SourceCreate.Id };

        var result = await _service.ExportAsync(_data.Source.Id, ticked, "out.yaml", CancellationToken.None);

        Assert.That(result.Value, Is.EqualTo(new ExportOutcome("out.yaml", 1, 1)));
        A.CallTo(() => _format.Write(A<Collection>.That.Matches(collection => TreeCounts.Of(collection.Root) == new TreeCounts(1, 1))))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _writer.WriteAllTextAsync("out.yaml", "text", A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExportWithNothingTickedWritesNothing()
    {
        var result = await _service.ExportAsync(_data.Source.Id, new HashSet<Guid>(), "out.yaml", CancellationToken.None);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.Validation));
        A.CallTo(() => _writer.WriteAllTextAsync(A<string>._, A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task ExportOfAMissingCollectionFails()
    {
        var result = await _service.ExportAsync(Guid.NewGuid(), _data.AllTicked, "out.yaml", CancellationToken.None);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.NotFound));
    }

    [Test]
    public async Task ExportReportsAFailedWrite()
    {
        A.CallTo(() => _writer.WriteAllTextAsync(A<string>._, A<string>._, A<CancellationToken>._))
            .Returns(Result.Failure(new Error(ErrorCategory.Storage, "No access.")));

        var result = await _service.ExportAsync(_data.Source.Id, _data.AllTicked, "out.yaml", CancellationToken.None);

        Assert.That(result.Error?.Message, Is.EqualTo("No access."));
    }
}
