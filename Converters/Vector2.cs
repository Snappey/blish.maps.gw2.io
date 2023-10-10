using System;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GW2IO.Maps.Converters
{
    public class Vector2Converter : JsonConverter<Vector2>
    {

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            float x = (float)obj["X"];
            float y = (float)obj["Y"];
            return new Vector2(x, y);
        }

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }
    }
}
