using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace GeneaGrab.Core.Models.Dates
{
    public enum Calendar { Gregorian, Julian, FrenchRepublican } // TODO: Maybe add FrenchRepublicanWithDecimalTime
    public enum Precision { Unknown = -1, Years, Months, Days, Hours, Minutes, Seconds }

    [JsonConverter(typeof(DateConverter))]
    public class Date : IComparable<Date>
    {
        public IYear Year { get; internal set; }
        public IMonth Month { get; internal set; }
        public IDay Day { get; internal set; }
        public IHour Hour { get; internal set; }
        public IMinute Minute { get; internal set; }
        public ISecond Second { get; internal set; }

        public Calendar Calendar { get; set; } = Calendar.Gregorian;
        public Precision Precision { get; set; } = Precision.Seconds;

        public Date() { }
        public Date(DateTime dt, Precision precision = Precision.Days)
        {
            Year = new Calendars.Gregorian.GregorianYear { Value = dt.Year };
            Month = new Calendars.Gregorian.GregorianMonth { Value = dt.Month };
            Day = new Calendars.Gregorian.GregorianDay { Value = dt.Day };
            Hour = new Calendars.Gregorian.GregorianHour { Value = dt.Hour };
            Minute = new Calendars.Gregorian.GregorianMinute { Value = dt.Minute };
            Second = new Calendars.Gregorian.GregorianSecond { Value = dt.Second };
            Precision = precision;
        }

        public static implicit operator Date(string date) => ParseDate(date);
        public static Date ParseDate(string date) //TODO
        {
            var culture = new System.Globalization.CultureInfo("fr-FR");
            var style = System.Globalization.DateTimeStyles.AssumeLocal;

            if (string.IsNullOrWhiteSpace(date)) return null;
            else if (DateTime.TryParse(date, culture, style, out var d)) return new Date(d);
            else if (DateTime.TryParseExact(date, "yyyy", culture, style, out d)) return new Date(d, Precision.Years);
            return null;
        }

        public override string ToString() => ToString(Precision);
        public string ToString(Precision precision)
        {
            var txt = new System.Text.StringBuilder();
            var format = (Precision)Math.Min((int)precision, (int)Precision);

            txt.Append(Year.Short);
            if (format == Precision.Years) return txt.ToString();
            txt.Append("-");
            txt.Append(Month.Short);
            if (format == Precision.Months) return txt.ToString();
            txt.Append("-");
            txt.Append(Day.Short);
            if (format == Precision.Days) return txt.ToString();

            txt.Append(" ");
            txt.Append(Hour.Short);
            if (format == Precision.Hours) return txt.ToString();
            txt.Append(":");
            txt.Append(Minute.Short);
            if (format == Precision.Minutes) return txt.ToString();
            txt.Append(":");
            txt.Append(Second.Short);
            return txt.ToString();
        }

        public static Type GetYearType(Calendar calendar)
        {
            if (calendar == Calendar.Julian) return typeof(Calendars.Julian.JulianYear);
            if (calendar == Calendar.Gregorian) return typeof(Calendars.Gregorian.GregorianYear);
            if (calendar == Calendar.FrenchRepublican) return typeof(Calendars.FrenchRepublican.FrenchRepublicanYear);
            return null;
        }
        public static Type GetMonthType(Calendar calendar)
        {
            if (calendar == Calendar.Julian) return typeof(Calendars.Julian.JulianMonth);
            if (calendar == Calendar.Gregorian) return typeof(Calendars.Gregorian.GregorianMonth);
            if (calendar == Calendar.FrenchRepublican) return typeof(Calendars.FrenchRepublican.FrenchRepublicanMonth);
            return null;
        }
        public static Type GetDayType(Calendar calendar)
        {
            if (calendar == Calendar.Julian) return typeof(Calendars.Julian.JulianDay);
            if (calendar == Calendar.Gregorian) return typeof(Calendars.Gregorian.GregorianDay);
            if (calendar == Calendar.FrenchRepublican) return typeof(Calendars.FrenchRepublican.FrenchRepublicanDay);
            return null;
        }
        public static Type GetHourType(Calendar calendar)
        {
            if (calendar == Calendar.Julian) return typeof(Calendars.Julian.JulianHour);
            if (calendar == Calendar.Gregorian) return typeof(Calendars.Gregorian.GregorianHour);
            if (calendar == Calendar.FrenchRepublican) return typeof(Calendars.FrenchRepublican.FrenchRepublicanHour);
            return null;
        }
        public static Type GetMinuteType(Calendar calendar)
        {
            if (calendar == Calendar.Julian) return typeof(Calendars.Julian.JulianMinute);
            if (calendar == Calendar.Gregorian) return typeof(Calendars.Gregorian.GregorianMinute);
            if (calendar == Calendar.FrenchRepublican) return typeof(Calendars.FrenchRepublican.FrenchRepublicanMinute);
            return null;
        }
        public static Type GetSecondType(Calendar calendar)
        {
            if (calendar == Calendar.Julian) return typeof(Calendars.Julian.JulianSecond);
            if (calendar == Calendar.Gregorian) return typeof(Calendars.Gregorian.GregorianSecond);
            if (calendar == Calendar.FrenchRepublican) return typeof(Calendars.FrenchRepublican.FrenchRepublicanSecond);
            return null;
        }

        public int CompareTo(Date other)
        {
            if (this == other) return 0;
            if (other == null) return 1;

            if (Compare(Year?.Value, other.Year?.Value, out var year)) return year;
            if (Compare(Month?.Value, other.Month?.Value, out var month)) return month;
            if (Compare(Day?.Value, other.Day?.Value, out var day)) return day;
            if (Compare(Hour?.Value, other.Hour?.Value, out var hour)) return hour;
            if (Compare(Minute?.Value, other.Minute?.Value, out var minute)) return minute;
            if (Compare(Second?.Value, other.Second?.Value, out var second)) return second;
            return 0;

            bool Compare(int? one, int? two, out int c)
            {
                c = 0;
                if (!one.HasValue || !two.HasValue) return true; // This is the maximum level of precision, aborting

                if (one < two) c = -1;
                else if (one > two) c = 1;
                return c != 0;
            }
        }
        
        public static bool operator ==(Date date1, Date date2)
        {
            if (ReferenceEquals(date1, date2)) return true;
            if (date1 is null || date2 is null) return false;
            if (date1.Precision != date2.Precision || date1.Calendar != date2.Calendar) return false;

            if (date1.Precision >= Precision.Years && date1.Year?.Value != date2.Year?.Value) return false;
            if (date1.Precision >= Precision.Months && date1.Month?.Value != date2.Month?.Value) return false;
            if (date1.Precision >= Precision.Days && date1.Day?.Value != date2.Day?.Value) return false;
            if (date1.Precision >= Precision.Hours && date1.Hour?.Value != date2.Hour?.Value) return false;
            if (date1.Precision >= Precision.Minutes && date1.Minute?.Value != date2.Minute?.Value) return false;
            return date1.Precision < Precision.Seconds || date1.Second?.Value == date2.Second?.Value;
        }
        public static bool operator !=(Date date1, Date date2) => !(date1 == date2);
    }

    class DateConverter : JsonConverter<Date>
    {
        public override Date ReadJson(JsonReader reader, Type objectType, Date existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String) return Date.ParseDate((string)reader.Value);
            if (reader.TokenType != JsonToken.StartObject) return null;

            var jObject = JObject.Load(reader);
            var calendar = (Calendar)Enum.Parse(typeof(Calendar), jObject.Value<string>("Calendar"));

            serializer.Converters.Add(new DateValueConverter());
            return new Date
            {
                Calendar = calendar,
                Precision = (Precision)Enum.Parse(typeof(Precision), jObject.Value<string>("Precision")),
                Year = jObject.GetValue("Year")?.ToObject(Date.GetYearType(calendar), serializer) as IYear,
                Month = jObject.GetValue("Month")?.ToObject(Date.GetMonthType(calendar), serializer) as IMonth,
                Day = jObject.GetValue("Day")?.ToObject(Date.GetDayType(calendar), serializer) as IDay,
                Hour = jObject.GetValue("Hour")?.ToObject(Date.GetHourType(calendar), serializer) as IHour,
                Minute = jObject.GetValue("Minute")?.ToObject(Date.GetMinuteType(calendar), serializer) as IMinute,
                Second = jObject.GetValue("Second")?.ToObject(Date.GetSecondType(calendar), serializer) as ISecond
            };
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


            void WriteProperty(string _name, object _value)
            {
                writer.WritePropertyName(_name);
                writer.WriteValue(_value);
            }
        }
    }
    class DateValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(Generic));
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var generic = (Generic)Activator.CreateInstance(objectType);
            generic.Value = (int)(long)reader.Value;
            return generic;
        }

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
