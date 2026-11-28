using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange.Remote;

public static class DownloadErrors
{
    public static Error Cancelled { get; } = new(ErrorCategory.Cancelled, "The download was cancelled.");
}
