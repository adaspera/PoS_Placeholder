using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoS_Placeholder.Server.Utilities
{
    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        private const string TimeFormat = "HH:mm";

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? timeStr = reader.GetString();
            if (TimeOnly.TryParseExact(timeStr, TimeFormat, out TimeOnly time))
            {
                return time;
            }
            throw new JsonException("Unable to convert to TimeOnly from provided JSON value.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(TimeFormat));
        }
    }
}
