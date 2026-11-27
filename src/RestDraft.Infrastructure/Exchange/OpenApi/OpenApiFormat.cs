using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Reader;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Results;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// A Swagger 2.0 or OpenAPI 3 specification, in JSON or YAML. References to other files are never loaded.
public sealed class OpenApiFormat : ICollectionFormat
{
    // Nested objects and lists. A specification with inline schemas goes far deeper than a collection file.
    internal const int MaxDepth = 128;

    private static readonly string[] RootKeys = ["openapi", "swagger"];

    // The reader wants to know where a document came from. Nothing is loaded from there.
    private static readonly Uri DocumentAddress = new("https://restdraft.invalid/");

    private static readonly Action<ILogger, string, Exception?> ReadFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(12, nameof(ReadFailed)),
        "A specification could not be read: {Reason}");

    private readonly IIdGenerator _ids;
    private readonly ILogger<OpenApiFormat> _logger;

    public OpenApiFormat(IIdGenerator ids, ILogger<OpenApiFormat> logger)
    {
        _ids = ids;
        _logger = logger;
    }

    public string Id => "openapi";

    public string DisplayName => "OpenAPI or Swagger specification (JSON, YAML)";

    public IReadOnlyList<string> FileExtensions { get; } = [".json", ".yaml", ".yml"];

    public Result<bool> CanRead(string text)
    {
        var found = IsJson(text) ? HasRootProperty(text) : YamlRootValueReader.Find(text, RootKeys) is not null;
        return Result<bool>.Success(found);
    }

    public Result<ExchangeDocument> Read(string text)
    {
        var json = IsJson(text) ? Result<string>.Success(text) : ConvertYaml(text);
        if (!json.IsSuccess)
        {
            return Result<ExchangeDocument>.Failure(json.Error!);
        }

        try
        {
            return Map(Parse(json.Value!));
        }
        catch (JsonException exception)
        {
            return Failed($"The file is not valid JSON: {exception.Message}", exception);
        }
        // The reader library meets whatever a file holds, and it throws more than its own exceptions (a null reference or
        // an index out of range among them). Each means that this file cannot be read, not that the app should stop.
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Failed($"The specification could not be read: {exception.Message}", exception);
        }
    }

    private static bool IsJson(string text)
    {
        return text.AsSpan().TrimStart().StartsWith("{");
    }

    private static bool HasRootProperty(string json)
    {
        var options = new JsonReaderOptions { MaxDepth = int.MaxValue, CommentHandling = JsonCommentHandling.Skip };
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), options);
        try
        {
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                foreach (var key in RootKeys)
                {
                    if (reader.ValueTextEquals(key))
                    {
                        return true;
                    }
                }

                reader.Skip();
            }
        }
        catch (JsonException)
        {
            // Not being JSON only means that this is not a specification.
            return false;
        }

        return false;
    }

    private static Result<string> ConvertYaml(string text)
    {
        var safe = YamlSafetyCheck.Validate(text, MaxDepth, "The file is nested too deeply.");
        return safe.IsSuccess
            ? YamlToJsonConverter.Convert(text)
            : Result<string>.Failure(safe.Error!);
    }

    private static ReadResult Parse(string json)
    {
        var options = new JsonDocumentOptions
        {
            MaxDepth = MaxDepth,
            AllowDuplicateProperties = false,
            CommentHandling = JsonCommentHandling.Skip,
        };
        var root = JsonNode.Parse(json, documentOptions: options)!;
        return new OpenApiJsonReader().Read(root, DocumentAddress, new OpenApiReaderSettings { LoadExternalRefs = false });
    }

    private Result<ExchangeDocument> Map(ReadResult read)
    {
        var errors = read.Diagnostic?.Errors ?? [];
        if (read.Document is null)
        {
            var reason = errors.Count > 0 ? errors[0].Message : "it holds no document.";
            return Failed($"The specification could not be read: {reason}", null);
        }

        return Result<ExchangeDocument>.Success(new OpenApiImportMapper(_ids).Map(read.Document, errors));
    }

    private Result<ExchangeDocument> Failed(string message, Exception? exception)
    {
        ReadFailed(_logger, message, exception);
        return Result<ExchangeDocument>.Failure(ResultErrors.Validation(message));
    }
}
