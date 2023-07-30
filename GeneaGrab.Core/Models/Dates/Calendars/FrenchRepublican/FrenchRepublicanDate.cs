using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using RomanNumerals.Numerals;

namespace GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican
{
    public class FrenchRepublicanDate : Date
    {
        public override Calendar Calendar => Calendar.FrenchRepublican;

        public static bool TryParse(string dateString, out FrenchRepublicanDate date)
        {
            date = null;
            if (string.IsNullOrWhiteSpace(dateString)) return false;
            var regex = Regex.Match(dateString, @"((?<day>\d+) )?((?<month>\p{L}+) )?an (?<year>[IVX\d]+)", RegexOptions.IgnoreCase);
            if (!regex.Success) return false;
            
            date = new FrenchRepublicanDate();
            if (!TryGet("year", out var year)) return false;
            
            date.Year = new FrenchRepublicanYear(year);
            date.Precision = Precision.Years;
            if (!TryGet("month", out var month)) return true;
            
            date.Month = new FrenchRepublicanMonth(month);
            date.Precision = Precision.Months;
            if (!TryGet("day", out var day)) return true;
            
            date.Day = new FrenchRepublicanDay(day);
            date.Precision = Precision.Days;
            return true;

            bool TryGet(string key, out int value)
            {
                value = -1;
                if (!regex.Groups[key].Success) return false;
                if(int.TryParse(regex.Groups[key].Value, out var intVal)) value = intVal;
                else if (NumeralParser.Default.TryParse(regex.Groups[key].Value, out var uintVal)) value = (int)uintVal;
                else
                {
                    var monthName = FrenchRepublicanMonth.Months.FirstOrDefault(m => string.Compare(m, regex.Groups[key].Value, CultureInfo.InvariantCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0);
                    if (monthName != null) value = Array.IndexOf(FrenchRepublicanMonth.Months, monthName) + 1;
                }
                return value != -1;
            }
        }

        internal FrenchRepublicanDate() { }
        public FrenchRepublicanDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days)
            => SetDate(year, month, day, hour, minute, second, precision);

        internal sealed override Date SetDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days)
        {
            Year = new FrenchRepublicanYear(year);
            if (month.HasValue) Month = new FrenchRepublicanMonth(month.Value);
            if (day.HasValue) Day = new FrenchRepublicanDay(day.Value);
            if (hour.HasValue) Hour = new FrenchRepublicanHour(hour.Value);
            if (minute.HasValue) Minute = new FrenchRepublicanMinute(minute.Value);
            if (second.HasValue) Second = new FrenchRepublicanSecond(second.Value);
            Precision = precision;
            return this;
        }

        public override string ToString(Precision precision) => precision == Precision.Years || Precision == Precision.Years ? Year.Medium : base.ToString(Precision);
    }
}
