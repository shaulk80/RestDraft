using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

// A format that a collection can also be exported to.
public interface IWritableCollectionFormat : ICollectionFormat
{
    Result<string> Write(Collection collection);
}
