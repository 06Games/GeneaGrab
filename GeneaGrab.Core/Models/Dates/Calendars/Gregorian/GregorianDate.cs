using System;
using System.Globalization;
using GeneaGrab.Core.Models.Dates.Calendars.Julian;

namespace GeneaGrab.Core.Models.Dates.Calendars.Gregorian
{
    public class GregorianDate : Date
    {
        public override Calendar Calendar => Calendar.Gregorian;

        public static bool TryParse(string dateString, out GregorianDate date)
        {
            date = null;

            var culture = new CultureInfo("fr-FR");
            const DateTimeStyles style = DateTimeStyles.AssumeLocal;

            if (string.IsNullOrWhiteSpace(dateString)) return false;

            Precision precision;
            if (DateTime.TryParse(dateString, culture, style, out var d)) precision = Precision.Days;
            else if (DateTime.TryParseExact(dateString, "yyyy", culture, style, out d)) precision = Precision.Years;
            else return false;

            if (d.Year < 1582) return false;
            date = new GregorianDate(d, precision);
            return true;
        }

        internal GregorianDate() { }
        public GregorianDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days)
            => SetDate(year, month, day, hour, minute, second, precision);
        private GregorianDate(DateTime dt, Precision precision = Precision.Days) => SetDate(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, precision);

        internal sealed override Date SetDate(int year, int? month = null, int? day = null, int? hour = null, int? minute = null, int? second = null, Precision precision = Precision.Days)
        {
            if (year < 1582)
            {
                Data.Warn($"The year (${Year}) is prior to the creation of the gregorian calendar (1582), using the Julian calendar instead.", null); 
                return new JulianDate(year, month, day, hour, minute, second, precision);
            }
            Year = new GregorianYear(year);
            if (month.HasValue) Month = new GregorianMonth(month.Value);
            if (day.HasValue) Day = new GregorianDay(day.Value);
            if (hour.HasValue) Hour = new GregorianHour(hour.Value);
            if (minute.HasValue) Minute = new GregorianMinute(minute.Value);
            if (second.HasValue) Second = new GregorianSecond(second.Value);
            Precision = precision;
            return this;
        }
    }
}
