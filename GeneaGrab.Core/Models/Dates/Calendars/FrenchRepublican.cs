namespace GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican
{
    public class FrenchRepublicanYear : GenericYear
    {
        public override string Long => $"An {Short} de la République Française";
        public override string Medium => $"An {Short}";
        public override string Short => RomanNumerals.Convert.ToRomanNumerals(Value, RomanNumerals.Numerals.NumeralFlags.Unicode);
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
        public override string Medium => Value > 0 && Value <= 13 ? Months[Value] : null;
    }
    public class FrenchRepublicanDay : GenericDay { }
    public class FrenchRepublicanHour : GenericHour { }
    public class FrenchRepublicanMinute : GenericMinute { }
    public class FrenchRepublicanSecond : GenericSecond { }
}
