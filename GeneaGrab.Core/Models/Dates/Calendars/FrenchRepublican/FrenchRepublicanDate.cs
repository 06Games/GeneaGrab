using System.Text.RegularExpressions;

namespace GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican
{
    public class FrenchRepublicanDate : Date
    {
        public override Calendar Calendar => Calendar.FrenchRepublican;

        public static bool TryParse(string dateString, out FrenchRepublicanDate date)
        {
            date = null;
            var regex = Regex.Match(dateString, @"an (?<year>.*)", RegexOptions.IgnoreCase);
            if (!regex.Success) return false;
            date = new FrenchRepublicanDate();
            if (regex.Groups["year"].Success && RomanNumerals.Numerals.NumeralParser.TryParse(regex.Groups["year"].Value, out var year))
            {
                date.Year = new FrenchRepublicanYear(year);
                date.Precision = Precision.Years;
            }
            else return false;
            return true;
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
    }
}
