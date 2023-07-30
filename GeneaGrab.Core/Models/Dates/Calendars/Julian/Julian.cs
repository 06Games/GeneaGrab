using System;
using System.Diagnostics.CodeAnalysis;
using Convert = RomanNumerals.Convert;

namespace GeneaGrab.Core.Models.Dates.Calendars.Julian
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class JulianYear : GenericYear
    {
        public override string Long => Value < 0 ? $"Annus {Convert.ToRomanNumerals(Math.Abs(Value))} ante Christum natum" : $"Anno Domini {Short}";
        public override string Medium => Value < 0 ? $"{Short} a.C.n." : $"{Short} p.C.n."; // a.C.n. = Ante Christum natum ; p.C.n. = post Christum natum
        public override string Short => Value.ToString();
        internal override int MinValue => int.MinValue;
        internal override int MaxValue => int.MaxValue;
        public JulianYear(uint value) : base(value) { }
        public JulianYear(int value) : base(value) { }
    }
    
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class JulianMonth : GenericMonth
    {
        public static readonly string[] Months = {
            "Ianuarius", "Februarius", "Martius",
            "Aprilis", "Maius", "Iunius",
            "Iulius", "Augustus", "September",
            "October", "November", "December"
        };
        public override string Long => Months[Value-1];
        public override string Medium => Long[..3].ToUpperInvariant();
        internal override int MinValue => 1;
        internal override int MaxValue => 12;
        public JulianMonth(uint value) : base(value) { }
        public JulianMonth(int value) : base(value) { }
    }
    
    public class JulianDay : GenericDay
    {
        internal override int MinValue => 1;
        internal override int MaxValue => 31;
        public JulianDay(uint value) : base(value) { }
        public JulianDay(int value) : base(value) { }
    }
    
    public class JulianHour : GenericHour
    {
        internal override int MinValue => 0;
        internal override int MaxValue => 23;
        public JulianHour(uint value) : base(value) { }
        public JulianHour(int value) : base(value) { }
    }
    
    public class JulianMinute : GenericMinute
    {
        internal override int MinValue => 0;
        internal override int MaxValue => 59;
        public JulianMinute(uint value) : base(value) { }
        public JulianMinute(int value) : base(value) { }
    }
    
    public class JulianSecond : GenericSecond
    {
        internal override int MinValue => 0;
        internal override int MaxValue => 59;
        public JulianSecond(uint value) : base(value) { }
        public JulianSecond(int value) : base(value) { }
    }
}
