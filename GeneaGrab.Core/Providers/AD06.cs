using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class AD06 : ProviderAPI
    {
        public bool IndexSupport => false;

        readonly string[] SupportedServices = new[] { "EC", "CAD", "MAT_ETS", "RP" };
        delegate void Service(NameValueCollection query, string pageBody, ref Registry Registry);
        readonly Dictionary<string, Service> Appli = new Dictionary<string, Service> {
            { "ec", EC }, // Etat civil
            { "cad", CAD }, // Cadastre (Plan)
            { "etc_mat", ETC_MAT }, // Cadastre (Etat de section + Matrice)
            { "rp", RP } // Recensements
        };

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "www.basesdocumentaires-cg06.fr" || !SupportedServices.Any(s => URL.AbsolutePath.StartsWith($"/archives/ImageZoomViewer{s}.php"))) return false;

            var query = System.Web.HttpUtility.ParseQueryString(URL.Query);
            info = new RegistryInfo
            {
                RegistryID = query["IDDOC"] ?? query["cote"],
                ProviderID = "AD06",
                PageNumber = int.TryParse(query["page"], out var _p) ? _p : 1
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var Registry = new Registry(Data.Providers["AD06"]) { URL = System.Web.HttpUtility.UrlDecode(URL.OriginalString) };

            var client = new HttpClient();
            string pageBody = await client.GetStringAsync(Registry.URL).ConfigureAwait(false);

            var appli = Regex.Match(pageBody, "<input type=\"hidden\" id=\"appliTag\" name=\"appliTag\" value=\"(?<appli>.*?)\" \\/>").Groups["appli"]?.Value;
            var infos = Regex.Match(pageBody, "<input type=\"hidden\" id=\"infosTag\" name=\"infosTag\" value=\"(?<infos>.*?)\" \\/>").Groups["infos"]?.Value;
            var pages = Regex.Matches(pageBody, "imagesListe\\.push\\('(?<page>.*?)'\\)").Cast<Match>().Select(m => m.Groups["page"]?.Value).ToArray();
            Registry.Pages = pages.Select((p, i) => new RPage { Number = i + 1, URL = $"http://www.basesdocumentaires-cg06.fr/archives/ImageViewerTargetJP2.php?appli={appli}&imagePath={pages[i]}&infos={infos}" }).ToArray();

            var query = System.Web.HttpUtility.ParseQueryString(URL.Query);
            if (Appli.TryGetValue(appli, out var service)) service(query, pageBody, ref Registry);
            if (!int.TryParse(query["page"], out var _p)) _p = 1;

            Data.AddOrUpdate(Data.Providers["AD06"].Registries, Registry.ID, Registry);
            return new RegistryInfo(Registry) { PageNumber = _p };
        }

        #region Services

        static void EC(NameValueCollection query, string _, ref Registry Registry)
        {
            Registry.ID = query["IDDOC"];
            Registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["COMMUNE"].ToLower());
            Registry.LocationID = Array.IndexOf(cities, query["COMMUNE"]).ToString();
            Registry.District = Registry.DistrictID = string.IsNullOrWhiteSpace(query["PAROISSE"]) ? null : query["PAROISSE"];
            var dates = query["DATE"]?.Split(new[] { " à " }, StringSplitOptions.None);
            Registry.From = Data.ParseDate(dates.FirstOrDefault());
            Registry.To = Data.ParseDate(dates.LastOrDefault());
            Registry.Types = GetTypes(query["TYPEACTE"]);

            IEnumerable<RegistryType> GetTypes(string TYPEACTE)
            {
                foreach (var t in Regex.Split(TYPEACTE, "(?=[A-Z])"))
                {
                    var type = t.Trim(' ');

                    if (type == "Naissances") yield return RegistryType.Birth;
                    else if (type == "Tables décennales des naissances") yield return RegistryType.BirthTable;
                    else if (type == "Baptêmes") yield return RegistryType.Baptism;
                    else if (type == "Tables des baptêmes") yield return RegistryType.BaptismTable;

                    else if (type == "Publications" || type == "Publications de mariages") yield return RegistryType.Banns;
                    else if (type == "Mariages") yield return RegistryType.Marriage;
                    else if (type == "Tables des mariages" || type == "Tables décennales des mariages") yield return RegistryType.MarriageTable;

                    else if (type == "Décès") yield return RegistryType.Death;
                    else if (type == "Tables décennales des décès") yield return RegistryType.DeathTable;
                    else if (type == "Sépultures") yield return RegistryType.Burial;
                    else if (type == "Tables des sépultures") yield return RegistryType.BurialTable;
                }
            }
        }

        static void CAD(NameValueCollection query, string pageBody, ref Registry Registry)
        {
            Registry.ID = query["cote"];
            Registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["c"].ToLower());
            Registry.LocationID = Array.IndexOf(cities, query["c"]).ToString();
            Registry.District = Registry.DistrictID = query["l"] == "TA - Tableau d'assemblage" ? null : query["l"];
            Registry.From = Registry.To = Data.ParseDate(query["a"]);
            Registry.Types = GetTypes(query["t"]);
            Registry.Notes = $"{Regex.Match(pageBody, "<td colspan=\"3\">Analyse : <b>(?<analyse>.*?)<\\/b><\\/td>").Groups["analyse"]?.Value}\nÉchelle: {query["e"]}";

            IEnumerable<RegistryType> GetTypes(string type)
            {
                if (type == "T") yield return RegistryType.CadastralAssemblyTable; // Tableau d'assemblage
                else if (type == "S") yield return RegistryType.CadastralMap; // Section
            }
        }


        static void ETC_MAT(NameValueCollection query, string _, ref Registry Registry)
        {
            Registry.ID = query["IDDOC"];
            Registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["COMMUNE"].ToLower());
            Registry.LocationID = Array.IndexOf(cities, query["COMMUNE"]).ToString();
            Registry.District = Registry.DistrictID = query["COMPLEMENTLIEUX"];
            Registry.From = Registry.To = Data.ParseDate(query["DATE"]);
            Registry.Types = GetTypes(query["CHOIX"]).ToList();
            Registry.Notes = $"{query["NATURE"]}\nCote: {query["COTE"]}";

            IEnumerable<RegistryType> GetTypes(string type)
            {
                if (type == "ETS") yield return RegistryType.CadastralSectionStates; // Tableau d'assemblage
                else if (type == "MAT") yield return RegistryType.CadastralMatrix; // Section
            }
        }

        static void RP(NameValueCollection query, string pageBody, ref Registry Registry)
        {
            Registry.ID = $"{query["cote"]}___{query["date"]}";
            Registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["c"].ToLower());
            Registry.LocationID = Array.IndexOf(cities, query["c"].ToUpper()).ToString();
            Registry.From = Registry.To = Data.ParseDate(query["date"]);
            Registry.Types = new[] { RegistryType.Census };
            Registry.Notes = query["cote"];
        }

        #endregion

        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"p{Page.Number}");
        public Task<RPage> Thumbnail(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.1F, progress);
        public Task<RPage> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.5F, progress);
        public Task<RPage> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 1, progress);
        public static async Task<RPage> GetTiles(Registry Registry, RPage current, float zoom, Action<Progress> progress)
        {
            if (await Data.TryGetImageFromDrive(Registry, current, zoom * 100)) return current;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            string link = await client.GetStringAsync(current.URL).ConfigureAwait(false);
            string url = await client.GetStringAsync(Regex.Match(link, "(https?:\\/\\/.*)").Value).ConfigureAwait(false);
            var id = Regex.Match(url, "location\\.replace\\(\"Fullscreen\\.ics\\?id=(?<id>.*?)&").Groups["id"]?.Value;
            if (string.IsNullOrWhiteSpace(id)) return current;

            //We can't track the progress because we don't know the final size
            current.Image = await Grabber.GetImage($"http://www.basesdocumentaires-cg06.fr:8080/ics/Converter?id={id}&s={zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)}", client);
            current.Zoom = (int)(zoom * 100);
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD06"].Registries[Registry.ID].Pages[current.Number - 1] = current;
            await Data.SaveImage(Registry, current);
            return current;
        }



        static readonly string[] cities = new[]{
            "Choisissez une commune",
            "AIGLUN",
            "AMIRAT",
            "ANDON",
            "ANTIBES",
            "ASCROS",
            "ASPREMONT",
            "AURIBEAU-SUR-SIAGNE",
            "AUVARE",
            "BAIROLS",
            "BAR-SUR-LOUP (LE)",
            "BEAULIEU-SUR-MER",
            "BEAUSOLEIL",
            "BELVEDERE",
            "BENDEJUN",
            "BERRE-LES-ALPES",
            "BEUIL",
            "BEZAUDUN-LES-ALPES",
            "BIOT",
            "BLAUSASC",
            "BOLLENE-VESUBIE (LA)",
            "BONSON",
            "BOUYON",
            "BREIL-SUR-ROYA",
            "BRIANCONNET",
            "BRIGUE (LA)",
            "BROC (LE)",
            "CABRIS",
            "CAGNES-SUR-MER",
            "CAILLE",
            "CANNES",
            "CANNET (LE)",
            "CANTARON",
            "CAP-D'AIL",
            "CARROS",
            "CASTAGNIERS",
            "CASTELLAR",
            "CASTILLON",
            "CAUSSOLS",
            "CHATEAUNEUF-DE-GRASSE",
            "CHATEAUNEUF-D'ENTRAUNES",
            "CHATEAUNEUF-VILLEVIEILLE",
            "CIPIERES",
            "CLANS",
            "COARAZE",
            "COLLE-SUR-LOUP (LA)",
            "COLLONGUES",
            "COLOMARS",
            "COMMUNES LIGURIENNES",
            "CONSEGUDES",
            "CONTES",
            "COURMES",
            "COURSEGOULES",
            "CROIX-SUR-ROUDOULE (LA)",
            "CUEBRIS",
            "DALUIS",
            "DRAP",
            "DURANUS",
            "ENTRAUNES",
            "ESCARENE (L')",
            "ESCRAGNOLLES",
            "EZE",
            "FALICON",
            "FERRES (LES)",
            "FONTAN",
            "GARS",
            "GATTIERES",
            "GAUDE (LA)",
            "GILETTE",
            "GORBIO",
            "GOURDON",
            "GRASSE",
            "GREOLIERES",
            "GUILLAUMES",
            "ILONSE",
            "ISOLA",
            "LANTOSQUE",
            "LEVENS",
            "LIEUCHE",
            "LUCERAM",
            "MALAUSSENE",
            "MANDELIEU-LA-NAPOULE",
            "MARIE",
            "MAS (LE)",
            "MASSOINS",
            "MENTON",
            "MONACO",
            "MOUANS-SARTOUX",
            "MOUGINS",
            "MOULINET",
            "MUJOULS (LES)",
            "NICE",
            "OPIO",
            "PEGOMAS",
            "PEILLE",
            "PEILLON",
            "PENNE (LA)",
            "PEONE",
            "PEYMEINADE",
            "PIERLAS",
            "PIERREFEU",
            "PUGET-ROSTANG",
            "PUGET-THENIERS",
            "REVEST-LES-ROCHES",
            "RIGAUD",
            "RIMPLAS",
            "ROQUEBILLIERE",
            "ROQUEBRUNE-CAP-MARTIN",
            "ROQUE-EN-PROVENCE (LA)",
            "ROQUEFORT-LES-PINS",
            "ROQUESTERON",
            "ROQUETTE-SUR-SIAGNE (LA)",
            "ROQUETTE-SUR-VAR (LA)",
            "ROUBION",
            "ROURE",
            "ROURET (LE)",
            "SAINT-ANDRE-DE-LA-ROCHE",
            "SAINT-ANTONIN",
            "SAINT-AUBAN",
            "SAINT-BLAISE",
            "SAINT-CEZAIRE-SUR-SIAGNE",
            "SAINT-DALMAS-LE-SELVAGE",
            "SAINTE-AGNES",
            "SAINT-ETIENNE-DE-TINEE",
            "SAINT-JEAN-CAP-FERRAT",
            "SAINT-JEANNET",
            "SAINT-LAURENT-DU-VAR",
            "SAINT-LEGER",
            "SAINT-MARTIN-D'ENTRAUNES",
            "SAINT-MARTIN-DU-VAR",
            "SAINT-MARTIN-VESUBIE",
            "SAINT-PAUL-DE-VENCE",
            "SAINT-SAUVEUR-SUR-TINEE",
            "SAINT-VALLIER-DE-THIEY",
            "SALLAGRIFFON",
            "SAORGE",
            "SAUZE",
            "SERANON",
            "SIGALE",
            "SOSPEL",
            "SPERACEDES",
            "TENDE",
            "THIERY",
            "TIGNET (LE)",
            "TOUDON",
            "TOUËT-DE-L'ESCARENE",
            "TOUËT-SUR-VAR",
            "TOUR (LA)",
            "TOURETTE-DU-CHÂTEAU",
            "TOURNEFORT",
            "TOURRETTE-LEVENS",
            "TOURRETTES-SUR-LOUP",
            "TRINITE (LA)",
            "TURBIE (LA)",
            "UTELLE",
            "VALBONNE",
            "VALDEBLORE",
            "VALDEROURE",
            "VALLAURIS",
            "VENANSON",
            "VENCE",
            "VILLARS-SUR-VAR",
            "VILLEFRANCHE-SUR-MER",
            "VILLENEUVE-D'ENTRAUNES",
            "VILLENEUVE-LOUBET",
            ""
        };
    }
}
