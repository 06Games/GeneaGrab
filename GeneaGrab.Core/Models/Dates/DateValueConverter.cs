using System;
using Newtonsoft.Json;

namespace GeneaGrab.Core.Models.Dates
{
    class DateValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(Generic));
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is null) return null;
            var generic = (Generic)Activator.CreateInstance(objectType);
            generic.Value = (int)(long)reader.Value;
            return generic;
        }

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
