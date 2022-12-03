using GeneaGrab.Core.Models.Dates.Calendars.Julian;

namespace GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican
{
    public class FrenchRepublicanYear : GenericYear
    {
        public override string Long => $"An {Short} de la République Française";
        public override string Medium => $"An {Short}";
        public override string Short => RomanNumerals.Convert.ToRomanNumerals(Value);
        internal override int MinValue => 1;
        internal override int MaxValue => 14;
        public FrenchRepublicanYear(uint value) : base(value) { }
        public FrenchRepublicanYear(int value) : base(value) { }
    }
    public class FrenchRepublicanMonth : GenericMonth
    {
        public static readonly string[] Months = {
            "Vendémiaire", "Brumaire", "Frimaire", // Autumn months
            "Nivôse", "Pluviôse", "Ventôse", // Winter months
            "Germinal", "Floréal", "Prairial", // Spring months
            "Messidor", "Thermidor", "Fructidor", // Summer months
            "Jours complémentaires" // Sans-culottides (Epagomenal days)
        };
        public override string Long => Months[Value];
        public override string Medium => Long.Substring(0,4).ToUpper();
        internal override int MinValue => 1;
        internal override int MaxValue => 13;
        public FrenchRepublicanMonth(uint value) : base(value) { }
        public FrenchRepublicanMonth(int value) : base(value) { }
    }
    public class FrenchRepublicanDay : GenericDay
    {
        internal override int MinValue => 1;
        internal override int MaxValue => 30;
        public FrenchRepublicanDay(uint value) : base(value) { }
        public FrenchRepublicanDay(int value) : base(value) { }
    }
    public class FrenchRepublicanHour : JulianHour
    {
        public FrenchRepublicanHour(uint value) : base(value) { }
        public FrenchRepublicanHour(int value) : base(value) { }
    }
    public class FrenchRepublicanMinute : JulianMinute
    {
        public FrenchRepublicanMinute(uint value) : base(value) { }
        public FrenchRepublicanMinute(int value) : base(value) { }
    }
    public class FrenchRepublicanSecond : JulianSecond
    {
        public FrenchRepublicanSecond(uint value) : base(value) { }
        public FrenchRepublicanSecond(int value) : base(value) { }
    }
}
