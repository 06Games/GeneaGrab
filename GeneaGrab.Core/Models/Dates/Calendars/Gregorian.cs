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
    }
    public class GregorianMonth : JulianMonth { }
    public class GregorianDay : JulianDay { }
    public class GregorianHour : JulianHour { }
    public class GregorianMinute : JulianMinute { }
    public class GregorianSecond : JulianSecond { }
}
