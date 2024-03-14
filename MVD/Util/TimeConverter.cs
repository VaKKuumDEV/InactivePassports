using Newtonsoft.Json;

namespace MVD.Util
{
    public class TimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (TimeOnly.TryParse((reader.Value ?? "00:00").ToString(), out TimeOnly result)) return result;

            return TimeOnly.MinValue;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue((((TimeOnly?)value) ?? new TimeOnly(0, 0)).ToLongTimeString());
        }
    }
}
