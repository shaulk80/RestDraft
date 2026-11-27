using RestDraft.Domain.Results;

namespace RestDraft.Application.Exchange;

// Finds the format that a text is written in and reads it.
public sealed class CollectionTextReader
{
    private const char ByteOrderMark = '﻿';

    public CollectionTextReader(IEnumerable<ICollectionFormat> formats)
    {
        Formats = formats.ToList();
    }

    public IReadOnlyList<ICollectionFormat> Formats { get; }

    public Result<ExchangeDocument> Read(string text)
    {
        var content = text.TrimStart(ByteOrderMark);
        foreach (var format in Formats)
        {
            var canRead = format.CanRead(content);
            if (!canRead.IsSuccess)
            {
                return Result<ExchangeDocument>.Failure(canRead.Error!);
            }

            if (canRead.Value)
            {
                return format.Read(content);
            }
        }

        return Result<ExchangeDocument>.Failure(ResultErrors.Validation(NotSupportedMessage(content)));
    }

    private string NotSupportedMessage(string text)
    {
        var names = string.Join(", ", Formats.Select(format => format.DisplayName));
        var message = $"The file is not in a supported format. Supported: {names}.";
        return text.TrimStart().StartsWith('{')
            ? message + " Insomnia v4 JSON is not supported yet."
            : message;
    }
}
