using System.Text;
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
public class ImportServiceTests
{
    private ExchangeFixtures _data = null!;
    private CollectionStore _store = null!;
    private ICollectionRepository _repository = null!;
    private IFileReader _reader = null!;
    private ICollectionFormat _format = null!;
    private ImportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _data = new ExchangeFixtures();
        _store = new CollectionStore([_data.Target]);
        _repository = CollectionRepositoryFake.Create(_store);
        _reader = A.Fake<IFileReader>();
        _format = A.Fake<ICollectionFormat>();
        A.CallTo(() => _format.DisplayName).Returns("Fake format");
        var workspace = new Workspace<Collection>(_repository.LoadAsync, _repository.SaveAsync);
        _service = new ImportService(workspace, _reader, new CollectionTextReader([_format]));
    }

    [Test]
    public async Task ApplySavesOnceAndReportsTheOutcome()
    {
        var document = new ExchangeDocument(_data.Source, [new ImportWarning("a"), new ImportWarning("b")]);

        var result = await _service.ApplyAsync(document, ImportTarget.Existing(_data.Target.Id), true, _data.AllTicked, CancellationToken.None);

        A.CallTo(() => _repository.SaveAsync(A<IReadOnlyList<Collection>>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        Guid[] overwritten = [_data.TargetList.Id];
        Assert.That(result.Value!.Counts, Is.EqualTo(new ImportCounts(0, 2, 1, 0)));
        Assert.That(result.Value.OverwrittenRequestIds, Is.EqualTo(overwritten));
        Assert.That(result.Value.WarningCount, Is.EqualTo(2));
        Assert.That(_store.Collections.Single().Root.Requests.Single().Name, Is.EqualTo("Ping"));
    }

    [Test]
    public async Task ApplySavesNothingWhenTheTargetIsGone()
    {
        var document = new ExchangeDocument(_data.Source, []);

        var result = await _service.ApplyAsync(document, ImportTarget.Existing(Guid.NewGuid()), false, _data.AllTicked, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        A.CallTo(() => _repository.SaveAsync(A<IReadOnlyList<Collection>>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task ReadUsesTheFirstFormatThatRecognisesTheFile()
    {
        var document = new ExchangeDocument(_data.Source, []);
        A.CallTo(() => _reader.ReadAllBytesAsync("file.yaml", A<CancellationToken>._))
            .Returns(Result<byte[]>.Success([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("type: x")]));
        A.CallTo(() => _format.CanRead("type: x")).Returns(Result<bool>.Success(true));
        A.CallTo(() => _format.Read("type: x")).Returns(Result<ExchangeDocument>.Success(document));

        var result = await _service.ReadAsync("file.yaml", CancellationToken.None);

        Assert.That(result.Value, Is.SameAs(document));
    }

    [TestCase("not: a collection", "Fake format")]
    [TestCase("{ \"_type\": \"export\" }", "Insomnia v4 JSON")]
    public async Task ReadNamesTheProblemWhenNoFormatFits(string text, string expectedInMessage)
    {
        A.CallTo(() => _reader.ReadAllBytesAsync("file", A<CancellationToken>._))
            .Returns(Result<byte[]>.Success(Encoding.UTF8.GetBytes(text)));
        A.CallTo(() => _format.CanRead(A<string>._)).Returns(Result<bool>.Success(false));

        var result = await _service.ReadAsync("file", CancellationToken.None);

        Assert.That(result.Error?.Message, Does.Contain(expectedInMessage));
    }

    [Test]
    public async Task ReadRefusesAFileOverTheLimit()
    {
        A.CallTo(() => _reader.ReadAllBytesAsync("big", A<CancellationToken>._))
            .Returns(Result<byte[]>.Success(new byte[ExchangeLimits.MaxFileBytes + 1]));

        var result = await _service.ReadAsync("big", CancellationToken.None);

        Assert.That(result.Error?.Message, Does.Contain("larger than 20 MB"));
        A.CallTo(() => _format.CanRead(A<string>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task ReadPassesOnAFailedRead()
    {
        A.CallTo(() => _reader.ReadAllBytesAsync("gone", A<CancellationToken>._))
            .Returns(Result<byte[]>.Failure(ResultErrors.NotFound("The file")));

        var result = await _service.ReadAsync("gone", CancellationToken.None);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.NotFound));
    }
}
