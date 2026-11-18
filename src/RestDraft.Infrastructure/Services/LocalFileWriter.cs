using System.Text;
using Microsoft.Extensions.Logging;
using RestDraft.Application.Abstractions;
using RestDraft.Domain.Results;

namespace RestDraft.Infrastructure.Services;

// Writes to a temporary file in the same folder and then moves it over the target, so a failure leaves the old file whole.
public sealed class LocalFileWriter : IFileWriter
{
    private static readonly Action<ILogger, string, Exception?> WriteFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(12, nameof(WriteFailed)),
        "A file could not be written: {Reason}");

    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    private readonly ILogger<LocalFileWriter> _logger;

    public LocalFileWriter(ILogger<LocalFileWriter> logger)
    {
        _logger = logger;
    }

    public async Task<Result> WriteAllTextAsync(string path, string text, CancellationToken cancellationToken)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, text, Utf8WithoutBom, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            DeleteQuietly(temporaryPath);
            return Result.Failure(new Error(ErrorCategory.Cancelled, "Writing the file was cancelled."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            WriteFailed(_logger, exception.Message, exception);
            DeleteQuietly(temporaryPath);
            return Result.Failure(new Error(ErrorCategory.Storage, "The file could not be written.", exception.Message));
        }
    }

    private void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            WriteFailed(_logger, exception.Message, exception);
        }
    }
}
