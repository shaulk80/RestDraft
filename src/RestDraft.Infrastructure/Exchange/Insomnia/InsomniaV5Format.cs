using Microsoft.Extensions.Logging;
using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.Infrastructure.Exchange.Insomnia.Dto;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// The Insomnia v5 collection file (type collection.insomnia.rest/5.x). Any 5.x is read, and keys it does not know are ignored.
public sealed class InsomniaV5Format : ICollectionFormat
{
    private const string TypePrefix = "collection.insomnia.rest/5.";

    private static readonly Action<ILogger, string, Exception?> ReadFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(10, nameof(ReadFailed)),
        "An Insomnia file could not be read: {Reason}");

    private static readonly Action<ILogger, string, Exception?> WriteFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(11, nameof(WriteFailed)),
        "An Insomnia file could not be written: {Reason}");

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithQuotingNecessaryStrings()
        .WithIndentedSequences()
        .WithEventEmitter(next => new MultilineLiteralEventEmitter(next))
        .Build();

    private readonly IIdGenerator _ids;
    private readonly IClock _clock;
    private readonly ILogger<InsomniaV5Format> _logger;

    public InsomniaV5Format(IIdGenerator ids, IClock clock, ILogger<InsomniaV5Format> logger)
    {
        _ids = ids;
        _clock = clock;
        _logger = logger;
    }

    public string Id => "insomnia-v5";

    public string DisplayName => "Insomnia v5 collection (YAML)";

    public IReadOnlyList<string> FileExtensions { get; } = [".yaml", ".yml"];

    public Result<bool> CanRead(string text)
    {
        var type = InsomniaYamlScanner.FindType(text);
        return Result<bool>.Success(type is not null && type.StartsWith(TypePrefix, StringComparison.Ordinal));
    }

    public Result<ExchangeDocument> Read(string text)
    {
        var scanned = InsomniaYamlScanner.Validate(text);
        if (!scanned.IsSuccess)
        {
            return Result<ExchangeDocument>.Failure(scanned.Error!);
        }

        try
        {
            var dto = Deserializer.Deserialize<InsomniaCollectionDto?>(text);
            if (dto is null)
            {
                return Result<ExchangeDocument>.Failure(ResultErrors.Validation("The file is empty."));
            }

            return Result<ExchangeDocument>.Success(new InsomniaImportMapper(_ids).Map(dto));
        }
        catch (Exception exception) when (exception is YamlException or InvalidCastException or FormatException)
        {
            ReadFailed(_logger, exception.Message, exception);
            return Result<ExchangeDocument>.Failure(ResultErrors.Validation($"The file could not be read: {exception.Message}"));
        }
    }

    public Result<string> Write(Collection collection)
    {
        try
        {
            var dto = new InsomniaExportMapper(_ids, _clock).Map(collection);
            return Result<string>.Success(Serializer.Serialize(dto));
        }
        catch (YamlException exception)
        {
            WriteFailed(_logger, exception.Message, exception);
            return Result<string>.Failure(new Error(ErrorCategory.Unexpected, "The collection could not be converted to YAML.", exception.Message));
        }
    }
}
