using System.Diagnostics.CodeAnalysis;
using GeneaGrab.Core.Models.Dates.Calendars.Julian;

namespace GeneaGrab.Core.Models.Dates.Calendars.Gregorian
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class GregorianYear : GenericYear
    {
        public override string Long => Value < 0 ? $"Annus {Short} ante Christum natum" : $"Anno Domini {Short}";
        public override string Medium => Value < 0 ? $"{Short} a.C.n." : $"{Short} p.C.n."; // a.C.n. = Ante Christum natum ; p.C.n. = post Christum natum
        public override string Short => Value.ToString();
        internal override int MinValue => 1582;
        internal override int MaxValue => int.MaxValue;
        public GregorianYear(uint value) : base(value) { }
        public GregorianYear(int value) : base(value) { }
    }
    public class GregorianMonth : JulianMonth
    {
        public GregorianMonth(uint value) : base(value) { }
        public GregorianMonth(int value) : base(value) { }
    }
    public class GregorianDay : JulianDay
    {
        public GregorianDay(uint value) : base(value) { }
        public GregorianDay(int value) : base(value) { }
    }
    public class GregorianHour : JulianHour
    {
        public GregorianHour(uint value) : base(value) { }
        public GregorianHour(int value) : base(value) { }
    }
    public class GregorianMinute : JulianMinute
    {
        public GregorianMinute(uint value) : base(value) { }
        public GregorianMinute(int value) : base(value) { }
    }
    public class GregorianSecond : JulianSecond
    {
        public GregorianSecond(uint value) : base(value) { }
        public GregorianSecond(int value) : base(value) { }
    }
}
