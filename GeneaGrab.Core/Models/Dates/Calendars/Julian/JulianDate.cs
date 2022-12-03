using System;
using System.Globalization;

namespace GeneaGrab.Core.Models.Dates.Calendars.Julian
{
    public class JulianDate : Date
    {
        public override Calendar Calendar => Calendar.Julian;

        public static bool TryParse(string dateString, out JulianDate date)
        {
            date = null;

            var culture = new CultureInfo("fr-FR");
            const DateTimeStyles style = DateTimeStyles.AssumeLocal;

            if (string.IsNullOrWhiteSpace(dateString)) return false;
            if (DateTime.TryParse(dateString, culture, style, out var d)) date = new JulianDate(d);
            else if (DateTime.TryParseExact(dateString, "yyyy", culture, style, out d)) date = new JulianDate(d, Precision.Years);
            else return false;
            return true;
        }

        internal JulianDate() { }
        public JulianDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days)
            => SetDate(year, month, day, hour, minute, second, precision);
        private JulianDate(DateTime dt, Precision precision = Precision.Days) => SetDate(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, precision);

        internal sealed override Date SetDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days)
        {
            Year = new JulianYear(year);
            if (month.HasValue) Month = new JulianMonth(month.Value);
            if (day.HasValue) Day = new JulianDay(day.Value);
            if (hour.HasValue) Hour = new JulianHour(hour.Value);
            if (minute.HasValue) Minute = new JulianMinute(minute.Value);
            if (second.HasValue) Second = new JulianSecond(second.Value);
            Precision = precision;
            return this;
        }
    }
}
