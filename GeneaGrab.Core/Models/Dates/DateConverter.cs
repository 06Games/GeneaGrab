using System;
using GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican;
using GeneaGrab.Core.Models.Dates.Calendars.Gregorian;
using GeneaGrab.Core.Models.Dates.Calendars.Julian;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeneaGrab.Core.Models.Dates
{
    internal class DateConverter : JsonConverter<Date>
    {
        public override Date ReadJson(JsonReader reader, Type objectType, Date existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String) return Date.ParseDate((string)reader.Value);
            if (reader.TokenType != JsonToken.StartObject) return null;

            var jObject = JObject.Load(reader);
            Enum.TryParse(jObject.Value<string>("Calendar"), out Calendar calendar);

            serializer.Converters.Add(new DateValueConverter());
            Date date = calendar switch
            {
                Calendar.Julian => new JulianDate(),
                Calendar.FrenchRepublican => new FrenchRepublicanDate(),
                _ => new GregorianDate()
            };
            return date.SetDate(
                precision: Enum.TryParse(jObject.Value<string>("Precision"), out Precision precision) ? precision : default,
                year: jObject.Value<int>("Year"),
                month: jObject.Value<int?>("Month"),
                day: jObject.Value<int?>("Day"),
                hour: jObject.Value<int?>("Hour"),
                minute: jObject.Value<int?>("Minute"),
                second: jObject.Value<int?>("Second")
            );
        }
        public override void WriteJson(JsonWriter writer, Date value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WriteProperty(nameof(value.Calendar), Enum.GetName(typeof(Calendar), value.Calendar));
            WriteProperty(nameof(value.Precision), Enum.GetName(typeof(Precision), value.Precision));

            if (value.Precision >= Precision.Years) WriteProperty(nameof(value.Year), value.Year.Value);
            if (value.Precision >= Precision.Months) WriteProperty(nameof(value.Month), value.Month.Value);
            if (value.Precision >= Precision.Days) WriteProperty(nameof(value.Day), value.Day.Value);
            if (value.Precision >= Precision.Hours) WriteProperty(nameof(value.Hour), value.Hour.Value);
            if (value.Precision >= Precision.Minutes) WriteProperty(nameof(value.Minute), value.Minute.Value);
            if (value.Precision >= Precision.Seconds) WriteProperty(nameof(value.Second), value.Second.Value);

            writer.WriteEndObject();


            void WriteProperty(string propertyName, object propertyValue)
            {
                writer.WritePropertyName(propertyName);
                writer.WriteValue(propertyValue);
            }
        }
    }
}
