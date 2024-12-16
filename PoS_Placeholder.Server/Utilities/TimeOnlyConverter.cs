using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoS_Placeholder.Server.Utilities
{
    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the JSON object as a JsonElement
            var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;

            // Extract the "hour" and "minute" properties
            var hour = jsonObject.GetProperty("hour").GetInt32();
            var minute = jsonObject.GetProperty("minute").GetInt32();

            // Return a TimeOnly object based on the hour and minute
            return new TimeOnly(hour, minute);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            // Write the TimeOnly object as a JSON object with "hour" and "minute"
            writer.WriteStartObject();
            writer.WriteNumber("hour", value.Hour);
            writer.WriteNumber("minute", value.Minute);
            writer.WriteEndObject();
        }
    }
}
