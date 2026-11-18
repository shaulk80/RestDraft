using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestDraft.Domain.Results;
using RestDraft.Infrastructure.Services;

namespace RestDraft.Infrastructure.Tests.Services;

[TestFixture]
public class LocalFileWriterTests
{
    private string _folder = string.Empty;
    private LocalFileWriter _writer = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "RestDraftTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _writer = new LocalFileWriter(NullLogger<LocalFileWriter>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Test]
    public async Task WriteCreatesTheFileWithoutAByteOrderMarkAndLeavesNoTemporaryFile()
    {
        var path = Path.Combine(_folder, "out.yaml");

        var result = await _writer.WriteAllTextAsync(path, "héllo", CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("héllo")));
        Assert.That(Directory.GetFiles(_folder), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task WriteReplacesAnExistingFile()
    {
        var path = Path.Combine(_folder, "out.yaml");
        await File.WriteAllTextAsync(path, "old");

        await _writer.WriteAllTextAsync(path, "new", CancellationToken.None);

        Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo("new"));
    }

    [Test]
    public async Task WriteIntoAMissingFolderFailsAsStorageError()
    {
        var path = Path.Combine(_folder, "missing", "out.yaml");

        var result = await _writer.WriteAllTextAsync(path, "x", CancellationToken.None);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.Storage));
    }
}
