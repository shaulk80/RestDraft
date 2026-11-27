using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using RestDraft.Domain.Results;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// Turns YAML into JSON, so that one JSON reader serves both. Keys, quoted values and block values are text. A plain value
// is null, true or false when written so, a number when it is written as a JSON number (kept as written, so 1.50 stays
// 1.50), and text otherwise. Run the YAML safety check first: an alias would be followed here.
internal static partial class YamlToJsonConverter
{
    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static Result<string> Convert(string yaml)
    {
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            return stream.Documents.Count == 1
                ? Write(stream.Documents[0].RootNode)
                : Failure("The file must hold exactly one YAML document.");
        }
        catch (YamlException exception)
        {
            return Failure($"The file is not valid YAML: {exception.Message}");
        }
    }

    private static Result<string> Write(YamlNode root)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            if (!TryWrite(writer, root))
            {
                return Failure("The file has a key that is not text.");
            }
        }

        return Result<string>.Success(Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
    }

    private static bool TryWrite(Utf8JsonWriter writer, YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                return TryWriteObject(writer, mapping);
            case YamlSequenceNode sequence:
                return TryWriteArray(writer, sequence);
            default:
                WriteScalar(writer, (YamlScalarNode)node);
                return true;
        }
    }

    private static bool TryWriteObject(Utf8JsonWriter writer, YamlMappingNode mapping)
    {
        writer.WriteStartObject();
        foreach (var pair in mapping.Children)
        {
            if (pair.Key is not YamlScalarNode key)
            {
                return false;
            }

            writer.WritePropertyName(key.Value ?? string.Empty);
            if (!TryWrite(writer, pair.Value))
            {
                return false;
            }
        }

        writer.WriteEndObject();
        return true;
    }

    private static bool TryWriteArray(Utf8JsonWriter writer, YamlSequenceNode sequence)
    {
        writer.WriteStartArray();
        foreach (var item in sequence.Children)
        {
            if (!TryWrite(writer, item))
            {
                return false;
            }
        }

        writer.WriteEndArray();
        return true;
    }

    private static void WriteScalar(Utf8JsonWriter writer, YamlScalarNode scalar)
    {
        var text = scalar.Value ?? string.Empty;
        if (!IsUntypedPlain(scalar))
        {
            writer.WriteStringValue(text);
            return;
        }

        switch (text)
        {
            case "" or "~" or "null" or "Null" or "NULL":
                writer.WriteNullValue();
                return;
            case "true" or "True" or "TRUE":
                writer.WriteBooleanValue(true);
                return;
            case "false" or "False" or "FALSE":
                writer.WriteBooleanValue(false);
                return;
        }

        if (JsonNumber().IsMatch(text))
        {
            writer.WriteRawValue(text);
            return;
        }

        writer.WriteStringValue(text);
    }

    // A value without quotes and without a tag: the only kind whose type the text itself decides.
    private static bool IsUntypedPlain(YamlScalarNode scalar)
    {
        return scalar.Style == ScalarStyle.Plain && (scalar.Tag.IsEmpty || scalar.Tag.Value == "?");
    }

    private static Result<string> Failure(string message)
    {
        return Result<string>.Failure(ResultErrors.Validation(message));
    }

    [GeneratedRegex(@"\A-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?\z")]
    private static partial Regex JsonNumber();
}
