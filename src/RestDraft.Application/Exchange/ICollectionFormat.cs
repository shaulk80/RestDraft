using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

// A file format that holds one collection and can be imported. A new format is one more implementation; the dialogs and
// the merge do not change.
public interface ICollectionFormat
{
    string Id { get; }

    string DisplayName { get; }

    // File name extensions with the dot, such as ".yaml". The first one is the default for a saved file.
    IReadOnlyList<string> FileExtensions { get; }

    // False when the text is of another format. A failure means the text could not even be looked at.
    Result<bool> CanRead(string text);

    Result<ExchangeDocument> Read(string text);
}
