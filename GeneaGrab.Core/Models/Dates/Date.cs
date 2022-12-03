using Newtonsoft.Json;
using System;
using GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican;
using GeneaGrab.Core.Models.Dates.Calendars.Gregorian;
using GeneaGrab.Core.Models.Dates.Calendars.Julian;

namespace GeneaGrab.Core.Models.Dates
{
    public enum Calendar { Gregorian, Julian, FrenchRepublican } // TODO: Maybe add FrenchRepublicanWithDecimalTime
    public enum Precision { Unknown = -1, Years, Months, Days, Hours, Minutes, Seconds }

    [JsonConverter(typeof(DateConverter))]
    public abstract class Date : IComparable<Date>
    {
        public IYear Year { get; internal set; }
        public IMonth Month { get; internal set; }
        public IDay Day { get; internal set; }
        public IHour Hour { get; internal set; }
        public IMinute Minute { get; internal set; }
        public ISecond Second { get; internal set; }

        public abstract Calendar Calendar { get; }
        public Precision Precision { get; set; } = Precision.Seconds;

        internal abstract Date SetDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days);

        public static implicit operator Date(string date) => ParseDate(date);
        public static Date ParseDate(string date)
        {
            if (GregorianDate.TryParse(date, out var gregorianDate)) return gregorianDate;
            if (JulianDate.TryParse(date, out var julianDate)) return julianDate;
            if (FrenchRepublicanDate.TryParse(date, out var frenchRepublicanDate)) return frenchRepublicanDate;
            return null;
        }

        public override string ToString() => ToString(Precision);
        public virtual string ToString(Precision precision)
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
}
