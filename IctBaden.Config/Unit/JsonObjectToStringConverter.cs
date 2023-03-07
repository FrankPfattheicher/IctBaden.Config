using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IctBaden.Config.Unit;

internal class JsonObjectToStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        return Encoding.UTF8.GetString(span);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
