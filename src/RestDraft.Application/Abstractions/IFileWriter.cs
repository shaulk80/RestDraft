using RestDraft.Domain.Results;

namespace RestDraft.Application.Abstractions;

public interface IFileWriter
{
    // Writes the whole file or leaves the old one as it was.
    Task<Result> WriteAllTextAsync(string path, string text, CancellationToken cancellationToken);
}
