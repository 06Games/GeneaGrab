using System;

namespace GeneaGrab.Core.Models.Dates.Calendars.Julian
{
    public class JulianYear : GenericYear
    {
        public override string Long => Value < 0 ? $"Annus {Short} ante Christum natum" : $"Anno Domini {Short}";
        public override string Medium => Value < 0 ? $"{Short} a.C.n." : $"{Short} p.C.n."; // a.C.n. = Ante Christum natum ; p.C.n. = post Christum natum
        public override string Short => (Value < 0 ? "−" : "") + RomanNumerals.Convert.ToRomanNumerals(Math.Abs(Value), RomanNumerals.Numerals.NumeralFlags.Unicode);
    }
    public class JulianMonth : GenericMonth
    {
        public static readonly string[] Months = new[] {
            "Ianuarius", "Februarius", "Martius",
            "Aprilis", "Maius", "Iunius",
            "Iulius", "Augustus", "September",
            "October", "November", "December"
        };
        public override string Medium => Value > 0 && Value <= 12 ? Months[Value-1] : null;
    }
    public class JulianDay : GenericDay { }
    public class JulianHour : GenericHour { }
    public class JulianMinute : GenericMinute { }
    public class JulianSecond : GenericSecond { }
}
