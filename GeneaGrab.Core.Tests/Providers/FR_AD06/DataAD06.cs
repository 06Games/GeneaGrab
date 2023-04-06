using GeneaGrab.Core.Models.Dates;
using GeneaGrab.Core.Models.Dates.Calendars.FrenchRepublican;
using GeneaGrab.Core.Models.Dates.Calendars.Gregorian;
using GeneaGrab.Core.Models.Dates.Calendars.Julian;

namespace GeneaGrab.Core.Tests.Providers.FR_AD06;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class DataAD06 : IEnumerable<object[]>
{
    // How to add data:
    // 1. Go to https://www.archinoe.net/v2/ad17/registre.html and open a registry
    // 2. In the console, execute
    // >>     window.location.href + "&infos=" + encodeURIComponent($("option:selected").prop('outerHTML')) + "&page=" + $("#visu_pagination").text().split('/')[0]
    private readonly List<object[]> _data = new()
    {
        // Etat civil
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerEC.php?HR=1&IDDOC=2003116181420696967&COMMUNE=LANTOSQUE&PAROISSE=&TYPEACTE=S%C3%A9pultures&DATE=1781%20%C3%A0%201784&page=3",
            Id = "2003116181420696967",
            Ville = "Lantosque",
            Types = new [] { RegistryType.Burial },
            From = new GregorianDate(1781, precision: Precision.Years),
            To = new GregorianDate(1784, precision: Precision.Years),
            Page = 3
        },
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerEC.php?HR=1&IDDOC=2004012016534450751946&COMMUNE=NICE&PAROISSE=Sainte-H%C3%A9l%C3%A8ne&TYPEACTE=S%C3%A9pultures%20des%20enfants%20d%C3%A9c%C3%A9d%C3%A9s%20sans%20bapt%C3%AAmes&DATE=1840%20%C3%A0%201870&page=35",
            Id = "2004012016534450751946",
            Ville = "Nice",
            Paroisse = "Sainte-Hélène",
            Types = new [] { RegistryType.Burial },
            From = new GregorianDate(1840, precision: Precision.Years),
            To = new GregorianDate(1870, precision: Precision.Years),
            Page = 35
        },
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerEC.php?HR=1&IDDOC=2016071815353524951886&COMMUNE=NICE&PAROISSE=Notre-Dame%20du%20Port&TYPEACTE=Bapt%C3%AAmes%20Tables%20des%20bapt%C3%AAmes&DATE=1823%20%C3%A0%201824&page=1",
            Id = "2016071815353524951886",
            Ville = "Nice",
            Paroisse = "Notre-Dame du Port",
            Types = new [] { RegistryType.Baptism, RegistryType.BaptismTable },
            From = new GregorianDate(1823, precision: Precision.Years),
            To = new GregorianDate(1824, precision: Precision.Years),
            Page = 1
        },
        
        // Cadastres
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerCAD.php?e=1/2000&c=BERRE-LES-ALPES&l=D1%20-%20Sena&t=S&cote=25FI%20015/1/D1/COM&a=1866&che=25Fi/015/015_1_D1_COM.jp2&ana=undefined",
            Id = "25FI 015/1/D1/COM",
            Cote = "25FI 015/1/D1/COM",
            Ville = "Berre-Les-Alpes",
            Paroisse = "D1 - Sena",
            Types = new [] { RegistryType.CadastralMap },
            From = new GregorianDate(1866, precision: Precision.Years),
            To = new GregorianDate(1866, precision: Precision.Years),
            Page = 1
        },
        
        // Matrices cadastrales
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerMAT_ETS.php?IDDOC=2011055132655345658831&COMMUNE=UTELLE&COMPLEMENTLIEUX=&COTE=03P_1503&NATURE=Etat%20des%20sections%20A%20%C3%A0%20M&DATE=1875&CHOIX=ETS&CODECOM=154&page=30",
            Id = "2011055132655345658831",
            Cote = "03P_1503",
            Ville = "Utelle",
            Types = new [] { RegistryType.CadastralSectionStates },
            From = new GregorianDate(1875, precision: Precision.Years),
            To = new GregorianDate(1875, precision: Precision.Years),
            Notes = "Etat des sections A à M",
            Page = 30
        },
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerMAT_ETS.php?IDDOC=2010122144614149114846&COMMUNE=UTELLE&COMPLEMENTLIEUX=&COTE=03P_1511&NATURE=Matrice%20cadastrale%20des%20propri%C3%A9t%C3%A9s%20non%20b%C3%A2ties&DATE=1913%20-%201969&CHOIX=MAT&CODECOM=154&FOLIO=folios%201489%20%C3%A0%201988&page=158",
            Id = "2010122144614149114846",
            Cote = "03P_1511",
            Ville = "Utelle",
            Types = new [] { RegistryType.CadastralMatrix },
            From = new GregorianDate(1913, precision: Precision.Years),
            To = new GregorianDate(1969, precision: Precision.Years),
            Notes = "Matrice cadastrale des propriétés non bâties (folios 1489 à 1988)",
            Page = 158
        },
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerMAT_ETS.php?IDDOC=2010122143545357252600&COMMUNE=NICE&COMPLEMENTLIEUX=Nice-Est%20:%20quartier%20Saint-Roch&COTE=03P_0828&NATURE=Matrice%20cadastrale%20des%20propri%C3%A9t%C3%A9s%20fonci%C3%A8res,%20b%C3%A2ties%20et%20non%20b%C3%A2ties,%20sections%20A%20%C3%A0%20E&DATE=1872%20-%201913&CHOIX=MAT&CODECOM=90&FOLIO=folios%201%20%C3%A0%20680",
            Id = "2010122143545357252600",
            Cote = "03P_0828",
            Ville = "Nice",
            Paroisse = "Nice-Est : quartier Saint-Roch",
            Types = new [] { RegistryType.CadastralMatrix },
            From = new GregorianDate(1872, precision: Precision.Years),
            To = new GregorianDate(1913, precision: Precision.Years),
            Notes = "Matrice cadastrale des propriétés foncières, bâties et non bâties, sections A à E (folios 1 à 680)",
            Page = 1
        },
        
        // Recensements
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerRP.php?cote=06M%200112&date=1891&c=Lantosque&page=7",
            Id = "06M 0112___1891",
            Cote = "06M 0112",
            Ville = "Lantosque",
            Types = new [] { RegistryType.Census },
            From = new GregorianDate(1891, precision: Precision.Years),
            To = new GregorianDate(1891, precision: Precision.Years),
            Page = 7
        }, 
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerRP.php?cote=06M%200149&date=1911,%20canton%20ouest%20(d%C3%83%C2%A9but)&c=Nice&page=62",
            Id = "06M 0149___1911",
            Cote = "06M 0149",
            Ville = "Nice",
            Paroisse = "Canton ouest",
            Types = new [] { RegistryType.Census },
            From = new GregorianDate(1911, precision: Precision.Years),
            To = new GregorianDate(1911, precision: Precision.Years),
            Notes = "début",
            Page = 62
        },
        
        // Sources imprimées
        new Data
        {
            URL = @"http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerSI.php?cote=II755&repertoire=II_755&d=MANTEYER+(Georges+de),+La+Provence+du+premier+au+douzième+siècle+:+études+d'histoire+et+de+géographie+politique+:+tables,+Gap,+1926,+985+p.",
            Id = "II755___II_755",
            Cote = "II755",
            Types = new [] { RegistryType.Book },
            From = new GregorianDate(1926, precision: Precision.Years),
            To = new GregorianDate(1926, precision: Precision.Years),
            Notes = "MANTEYER (Georges de), La Provence du premier au douzième siècle : études d'histoire et de géographie politique : tables, Gap, 1926, 985 p.",
            Page = 1
        }
    };

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class Data
{
    public string URL = null!;
    public string Id = null!;
    public int Page;
    public string? Cote;
    public string Ville = null!;
    public string? Paroisse;
    public RegistryType[] Types = null!;
    public Date From = null!;
    public string? Notes;
    public Date To = null!;

    public static implicit operator object[](Data data) => new object[] { data };
}
