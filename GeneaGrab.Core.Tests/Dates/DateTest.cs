using GeneaGrab.Core.Models.Dates;
using GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican;
using GeneaGrab.Core.Models.Dates.Calendars.Gregorian;
using GeneaGrab.Core.Models.Dates.Calendars.Julian;
using Xunit.Abstractions;

namespace GeneaGrab.Core.Tests.Dates;

public class DateTest
{
    private readonly ITestOutputHelper output;
    public DateTest(ITestOutputHelper output) { this.output = output; }
    
    public static IEnumerable<object[]> ParserData()
    {
        yield return new object[] { "17/01/1420", new JulianDate(1420, 1, 17, precision: Precision.Days) };
        
        yield return new object[] { "2023-07-30 16:00:02", new GregorianDate(2023, 7, 30, 16, 0, 2, precision: Precision.Seconds) };
        yield return new object[] { "2023-07", new GregorianDate(2023, 7, precision: Precision.Months) };
        
        yield return new object[] { "An XII", new FrenchRepublicanDate(12, precision: Precision.Years) };
        yield return new object[] { "An XIII de la République Française", new FrenchRepublicanDate(13, precision: Precision.Years) };
        yield return new object[] { "23 Ventôse An 4", new FrenchRepublicanDate(4, 6, 23, precision: Precision.Days) };
        yield return new object[] { "23 Ventôse An IV", new FrenchRepublicanDate(4, 6, 23, precision: Precision.Days) };
        yield return new object[] { "23 Ventose An IV", new FrenchRepublicanDate(4, 6, 23, precision: Precision.Days) };
        yield return new object[] { "10 Vendémiaire An X de la République Française", new FrenchRepublicanDate(10, 1, 10, precision: Precision.Days) };
    }

    [Theory(DisplayName = "Check string parser")]
    [MemberData(nameof(ParserData))]
    public void CheckParser(string text, Date expected)
    {
        var parsed = Date.ParseDate(text);
        Assert.NotNull(parsed);
        output.WriteLine(parsed.ToString());
        Assert.Equal(expected.Precision, parsed.Precision);
        Assert.Equal(0, expected.CompareTo(parsed));
        Assert.True(expected == parsed);
    } 
    
    public static IEnumerable<object[]> StringifyData()
    {
        yield return new object[] { new JulianDate(1420, 1, 17), "1420-01-17" };
        
        yield return new object[] { new GregorianDate(2023, 7, 30, 16, 0, 2, precision: Precision.Seconds), "2023-07-30 16:00:02" };
        yield return new object[] { new GregorianDate(2023, 7, precision: Precision.Months), "2023-07" };
        
        yield return new object[] { new FrenchRepublicanDate(12, precision: Precision.Years), "An XII" };
        yield return new object[] { new FrenchRepublicanDate(13, precision: Precision.Years), "An XIII" };
        yield return new object[] { new FrenchRepublicanDate(4, 6, 23, precision: Precision.Days), "IV-06-23" };
        yield return new object[] { new FrenchRepublicanDate(10, 1, 10, precision: Precision.Days), "X-01-10", };
    }

    [Theory(DisplayName = "Check date to string conversion")]
    [MemberData(nameof(StringifyData))]
    public void CheckStringify(Date date, string expected) => Assert.Equal(expected, date.ToString());
}
