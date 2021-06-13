﻿using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class AD06 : ProviderAPI
    {
        public bool TryGetRegistryID(Uri URL, out string id)
        {
            var check = URL.Host == "www.basesdocumentaires-cg06.fr" && URL.AbsolutePath.StartsWith("/archives/ImageZoomViewerEC.php");
            id = check ? System.Web.HttpUtility.ParseQueryString(URL.Query)["IDDOC"] : null;
            return check && !string.IsNullOrWhiteSpace(id);
        }

        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var Location = new Location(Data.Providers["AD06"]);
            var Registry = new Registry(Location) { URL = System.Web.HttpUtility.UrlDecode(URL.OriginalString, System.Text.Encoding.GetEncoding("iso-8859-1")) };

            var query = System.Web.HttpUtility.ParseQueryString(new Uri(Registry.URL).Query);
            Registry.ID = query["IDDOC"];

            var client = new HttpClient();
            string pageBody = await client.GetStringAsync(Registry.URL).ConfigureAwait(false);

            var regex = Regex.Matches(pageBody, "imagesListe\\.push\\('(?<page>.*?)'\\)");
            var pages = regex.Cast<Match>().Select(m => m.Groups["page"]?.Value).ToArray();
            var Pages = new List<RPage>();
            for (int i = 1; i <= pages.Length; i++) Pages.Add(new RPage { Number = i, URL = $"http://www.basesdocumentaires-cg06.fr/archives/ImageViewerTargetJP2.php?imagePath={pages[i - 1]}" });
            Registry.Pages = Pages.ToArray();

            Location.Name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(query["COMMUNE"].ToLower());
            Registry.LocationID = Location.ID = Array.IndexOf(cities, query["COMMUNE"]).ToString();
            Location.District = query["PAROISSE"];
            var dates = query["DATE"]?.Split(new[] { " à " }, StringSplitOptions.None);
            Registry.From = Data.ParseDate(dates.FirstOrDefault());
            Registry.To = Data.ParseDate(dates.LastOrDefault());
            Registry.Types = GetTypes(query["TYPEACTE"]);
            if (!int.TryParse(query["page"], out var _p)) _p = 1;

            Data.AddOrUpdate(Data.Providers["AD06"].Locations, Location.ID, Location);
            Data.AddOrUpdate(Data.Providers["AD06"].Registries, Registry.ID, Registry);
            return new RegistryInfo { ProviderID = "AD06", LocationID = Location.ID, RegistryID = Registry.ID, PageNumber = _p };
        }
        public List<Registry.Type> GetTypes(string TYPEACTE)
        {
            var types = new List<Registry.Type>();
            foreach (var t in Regex.Split(TYPEACTE, "(?=[A-Z])"))
                if (TryGetType(t.Trim(' '), out var type)) types.Add(type);

            bool TryGetType(string type, out Registry.Type t)
            {
                if (type == "Naissances") t = Registry.Type.Birth;
                else if (type == "Tables décennales des naissances") t = Registry.Type.BirthTable;
                else if (type == "Baptêmes") t = Registry.Type.Baptism;
                else if (type == "Publications" || type == "Publications de mariages") t = Registry.Type.Banns;
                else if (type == "Mariages") t = Registry.Type.Marriage;
                else if (type == "Tables décennales des mariages") t = Registry.Type.BirthTable;
                else if (type == "Décès") t = Registry.Type.Death;
                else if (type == "Tables décennales des décès") t = Registry.Type.BirthTable;
                else if (type == "Sépultures") t = Registry.Type.Burial;
                else
                {
                    t = Registry.Type.Unknown;
                    return false;
                }
                return true;
            }
            return types;
        }

        public Task<RPage> Thumbnail(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.1F, progress);
        public Task<RPage> GetTile(Registry Registry, RPage page, int zoom, Action<Progress> progress) => GetTiles(Registry, page, zoom / 100F, progress);
        public Task<RPage> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 1, progress);
        public static async Task<RPage> GetTiles(Registry Registry, RPage current, float zoom, Action<Progress> progress)
        {
            if (await Data.TryGetImageFromDrive(Registry, current, zoom)) return current;

            progress?.Invoke(Progress.UnterterminedProgress);
            var client = new HttpClient();
            string link = await client.GetStringAsync(current.URL).ConfigureAwait(false);
            string url = await client.GetStringAsync(link).ConfigureAwait(false);
            var id = Regex.Match(url, "location\\.replace\\(\"Fullscreen\\.ics\\?id=(?<id>.*?)&").Groups["id"]?.Value;
            if (string.IsNullOrWhiteSpace(id)) return current;

            //We can't track the progress because we don't know the final size
            current.Image = Image.Load(await client.GetStreamAsync(new Uri($"http://www.basesdocumentaires-cg06.fr:8080/ics/Converter?id={id}&s={zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)}")));
            current.Zoom = (int)(zoom * 100);
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD06"].Registries[Registry.ID].Pages[current.Number - 1] = current;
            await Data.SaveImage(Registry, current);
            return current;
        }



        public readonly string[] cities = new[]{
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
