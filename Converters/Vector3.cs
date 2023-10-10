using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace GW2IO.Maps.Converters
{
    public class Vector3Converter : JsonConverter<Vector3>
    {

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            float x = (float)obj["X"];
            float y = (float)obj["Y"];
            float z = (float)obj["Z"];
            return new Vector3(x, y, z);
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }
    }
}
