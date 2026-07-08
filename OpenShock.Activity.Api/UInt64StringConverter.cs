using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenShock.Activity.Api;

/// <summary>
/// Serializes <see cref="ulong"/> (Discord snowflake IDs) as JSON strings and reads them from either a
/// string or a number. Necessary because Discord IDs exceed JavaScript's safe-integer range and would
/// lose precision if sent as JSON numbers.
/// </summary>
public sealed class UInt64StringConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => ulong.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetUInt64(),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} for ulong.")
        };

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
