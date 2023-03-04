using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class AD06 : ProviderAPI
    {
        public bool IndexSupport => false;

        private readonly string[] supportedServices = { "EC", "CAD", "MAT_ETS", "RP" };
        private delegate void Service(NameValueCollection query, string pageBody, ref Registry registry);
        private readonly Dictionary<string, Service> applications = new Dictionary<string, Service> {
            { "ec", EC }, // Etat civil
            { "cad", CAD }, // Cadastre (Plan)
            { "etc_mat", ETC_MAT }, // Cadastre (Etat de section + Matrice)
            { "rp", RP } // Recensements
        };

        public bool TryGetRegistryID(Uri url, out RegistryInfo info)
        {
            info = null;
            if (url.Host != "www.basesdocumentaires-cg06.fr" || !supportedServices.Any(s => url.AbsolutePath.StartsWith($"/archives/ImageZoomViewer{s}.php"))) return false;

            var query = System.Web.HttpUtility.ParseQueryString(url.Query);
            info = new RegistryInfo
            {
                RegistryID = query["IDDOC"] ?? query["cote"],
                ProviderID = "AD06",
                PageNumber = int.TryParse(query["page"], out var _p) ? _p : 1
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri url)
        {
            var registry = new Registry(Data.Providers["AD06"]) { URL = System.Web.HttpUtility.UrlDecode(url.OriginalString) };

            var client = new HttpClient();
            var pageBody = await client.GetStringAsync(registry.URL).ConfigureAwait(false);

            var appli = Regex.Match(pageBody, "<input type=\"hidden\" id=\"appliTag\" name=\"appliTag\" value=\"(?<appli>.*?)\" \\/>").Groups["appli"]?.Value;
            var infos = Regex.Match(pageBody, "<input type=\"hidden\" id=\"infosTag\" name=\"infosTag\" value=\"(?<infos>.*?)\" \\/>").Groups["infos"]?.Value;
            var pages = Regex.Matches(pageBody, "imagesListe\\.push\\('(?<page>.*?)'\\)").Cast<Match>().Select(m => m.Groups["page"]?.Value).ToArray();
            registry.Pages = pages.Select((p, i) => new RPage { Number = i + 1, URL = $"http://www.basesdocumentaires-cg06.fr/archives/ImageViewerTargetJP2.php?appli={appli}&imagePath={pages[i]}&infos={infos}" }).ToArray();

            var query = System.Web.HttpUtility.ParseQueryString(url.Query);
            if (appli != null && this.applications.TryGetValue(appli, out var service)) service(query, pageBody, ref registry);
            if (!int.TryParse(query["page"], out var pageNumber)) pageNumber = 1;

            Data.AddOrUpdate(Data.Providers["AD06"].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = pageNumber };
        }

        #region Services

        private static void EC(NameValueCollection query, string _, ref Registry registry)
        {
            registry.ID = query["IDDOC"];
            registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["COMMUNE"].ToLower());
            registry.LocationID = Array.IndexOf(Cities, query["COMMUNE"]).ToString();
            registry.District = registry.DistrictID = string.IsNullOrWhiteSpace(query["PAROISSE"]) ? null : query["PAROISSE"];
            var dates = query["DATE"]?.Split(new[] { " à " }, StringSplitOptions.None);
            if (dates != null)
            {
                registry.From = Core.Models.Dates.Date.ParseDate(dates.FirstOrDefault());
                registry.To = Core.Models.Dates.Date.ParseDate(dates.LastOrDefault());
            }
            registry.Types = GetTypes(query["TYPEACTE"]);

            IEnumerable<RegistryType> GetTypes(string typeActe)
            {
                foreach (var t in Regex.Split(typeActe, "(?=[A-Z])"))
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

        private static void CAD(NameValueCollection query, string pageBody, ref Registry registry)
        {
            registry.ID = query["cote"];
            registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["c"].ToLower());
            registry.LocationID = Array.IndexOf(Cities, query["c"]).ToString();
            registry.District = registry.DistrictID = query["l"] == "TA - Tableau d'assemblage" ? null : query["l"];
            registry.CallNumber = query["cote"];
            registry.From = registry.To = Core.Models.Dates.Date.ParseDate(query["DATE"]);
            registry.Types = GetTypes(query["t"]);
            registry.Notes = $"{Regex.Match(pageBody, "<td colspan=\"3\">Analyse : <b>(?<analyse>.*?)<\\/b><\\/td>").Groups["analyse"]?.Value}\nÉchelle: {query["e"]}";

            IEnumerable<RegistryType> GetTypes(string type)
            {
                if (type == "T") yield return RegistryType.CadastralAssemblyTable; // Tableau d'assemblage
                else if (type == "S") yield return RegistryType.CadastralMap; // Section
            }
        }


        private static void ETC_MAT(NameValueCollection query, string _, ref Registry registry)
        {
            registry.ID = query["IDDOC"];
            registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["COMMUNE"].ToLower());
            registry.LocationID = Array.IndexOf(Cities, query["COMMUNE"]).ToString();
            if(!string.IsNullOrWhiteSpace(query["COMPLEMENTLIEUX"])) registry.District = registry.DistrictID = query["COMPLEMENTLIEUX"];
            registry.CallNumber = query["COTE"];
            var dates = query["DATE"]?.Split(new[] { " - " }, StringSplitOptions.None);
            if (dates != null)
            {
                registry.From = Core.Models.Dates.Date.ParseDate(dates.FirstOrDefault());
                registry.To = Core.Models.Dates.Date.ParseDate(dates.LastOrDefault());
            }
            registry.Types = GetTypes(query["CHOIX"]).ToList();

            registry.Notes = query["NATURE"];
            var folio = query["FOLIO"];
            if (!string.IsNullOrWhiteSpace(folio)) registry.Notes += $" ({folio})";

            IEnumerable<RegistryType> GetTypes(string type)
            {
                if (type == "ETS") yield return RegistryType.CadastralSectionStates; // Tableau d'assemblage
                else if (type == "MAT") yield return RegistryType.CadastralMatrix; // Section
            }
        }

        private static void RP(NameValueCollection query, string pageBody, ref Registry registry)
        {
            registry.ID = $"{query["cote"]}___{query["date"]}";
            registry.Location = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["c"].ToLower());
            registry.LocationID = Array.IndexOf(Cities, query["c"].ToUpper()).ToString();
            registry.From = registry.To = Core.Models.Dates.Date.ParseDate(query["date"]);
            registry.Types = new[] { RegistryType.Census };
            registry.CallNumber = query["cote"];
        }

        #endregion

        public Task<string> Ark(Registry registry, RPage page) => Task.FromResult($"p{page.Number}");
        public async Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(registry, page).ConfigureAwait(false);
            if (success) return stream;
            return await GetTiles(registry, page, 0.1F, progress).ConfigureAwait(false);
        }
        public Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 0.5F, progress);
        public Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 1, progress);
        private static async Task<Stream> GetTiles(Registry registry, RPage page, float zoom, Action<Progress> progress)
        {
            var pageZoom = (int)(zoom * 100);
            var (success, stream) = await Data.TryGetImageFromDrive(registry, page, pageZoom).ConfigureAwait(false);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var link = await client.GetStringAsync(page.URL).ConfigureAwait(false);
            var url = await client.GetStringAsync(Regex.Match(link, "(https?:\\/\\/.*)").Value).ConfigureAwait(false);
            var id = Regex.Match(url, "location\\.replace\\(\"Fullscreen\\.ics\\?id=(?<id>.*?)&").Groups["id"]?.Value;
            if (string.IsNullOrWhiteSpace(id)) return null;

            //We can't track the progress because we don't know the final size
            var image = await Grabber.GetImage($"http://www.basesdocumentaires-cg06.fr:8080/ics/Converter?id={id}&s={zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)}", client);
            page.Zoom = pageZoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD06"].Registries[registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(registry, page, image, false).ConfigureAwait(false);
            return image.ToStream();
        }



        [SuppressMessage("ReSharper", "StringLiteralTypo")] private static readonly string[] Cities = {
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
