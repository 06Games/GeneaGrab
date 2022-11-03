using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class Geneanet : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "www.geneanet.org" || !URL.AbsolutePath.StartsWith("/registres/view")) return false;

            var regex = Regex.Match(URL.OriginalString, "(?:idcollection=(?<col>\\d*).*page=(?<page>\\d*))|(?:\\/(?<col>\\d+)(?:\\z|\\/(?<page>\\d*)))");
            info = new RegistryInfo
            {
                RegistryID = regex.Groups["col"]?.Value,
                ProviderID = "Geneanet",
                PageNumber = int.TryParse(regex.Groups["page"].Success ? regex.Groups["page"].Value : "1", out var _p) ? _p : 1
            };
            return true;
        }

        #region Infos
        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var Registry = new Registry(Data.Providers["Geneanet"]) { URL = URL.OriginalString };

            var regex = Regex.Match(Registry.URL, "(?:idcollection=(?<col>\\d*).*page=(?<page>\\d*))|(?:\\/(?<col>\\d+)(?:\\z|\\/(?<page>\\d*)))");
            Registry.ID = regex.Groups["col"]?.Value;
            if (string.IsNullOrEmpty(Registry.ID)) return null;

            var client = new HttpClient();
            string page = await client.GetStringAsync(Registry.URL);
            var infos = Regex.Match(page, "Informations sur le document.*?<p>(\\[.*\\] - )?(?<location>.*) \\((?<locationDetails>.*?)\\) - (?<globalType>.*?)( \\((?<type>.*?)\\))?( - .*)? *\\| (?<from>.*) - (?<to>.*?)<\\/p>.*?<p>(?<cote>.*)</p>(.*<p>(?<notaire>.*)</p>)?.*<p class=\\\"no-margin-bottom\\\">(?<betterType>.*?)(\\..*| -.*)?</p>.*<p>(?<note>.*)</p>.*<strong>Lien permanent : </strong>", RegexOptions.Multiline | RegexOptions.Singleline); //https://regex101.com/r/3Ou7DP/5
            Registry.Location = Registry.LocationID = infos.Groups["location"].Value.Trim(' ');
            Registry.LocationDetails = infos.Groups["locationDetails"]?.Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray() ?? new string[0];

            var notes = TryParseNotes(page, infos);
            Registry.Types = notes.types;
            Registry.Notes = notes.notes;
            Registry.District = Registry.DistrictID = string.IsNullOrWhiteSpace(notes.location) ? null : notes.location;

            Registry.From = Core.Models.Dates.Date.ParseDate(infos.Groups["from"].Value);
            Registry.To = Core.Models.Dates.Date.ParseDate(infos.Groups["to"].Value);

            Registry = await UpdateInfos(Registry, client);
            int.TryParse(regex.Groups["page"].Success ? regex.Groups["page"].Value : "1", out var _p);

            // TODO: Necessite un token
            /* string marqueursPage = await client.GetStringAsync($"https://www.geneanet.org/registres/api/tool-panel/marqueur_date/view/{Registry.ID}/{_p}?lang=fr");
            var marqueurs = Regex.Matches(marqueursPage.Replace("\t", "").Replace("\n", "").Replace("\r", ""), "<option *value=\\\"(?<year>\\d*)-(?<month>\\d*)-(?<type>.)\\\" *data-redirect-url=\\\".*?\\/(?<page>\\d*)\\\" *>"); //https://regex101.com/r/t6l2HF/1
            foreach (var pageMarqueurs in marqueurs.Cast<Match>().GroupBy(m => m.Groups["page"].Value))
            {
                if (!int.TryParse(pageMarqueurs.Key ?? "0", out int i) || i < 1) continue;
                Registry.Pages[i - 1].Notes = string.Join(" - ", pageMarqueurs.Select(marqueur => marqueur.Groups["year"].Value)) + "\n\n"
                                            + string.Join("\n", pageMarqueurs.Select(marqueur => $"{marqueur.Groups["month"]}/{marqueur.Groups["year"]} ({marqueur.Groups["type"]})"));
            } */

            Data.AddOrUpdate(Data.Providers["Geneanet"].Registries, Registry.ID, Registry);
            return new RegistryInfo(Registry) { PageNumber = _p };
        }
        static async Task<Registry> UpdateInfos(Registry Registry, HttpClient client = null)
        {
            if (client == null) client = new HttpClient();

            Registry.URL = $"https://www.geneanet.org/registres/view/{Registry.ID}";

            string pagesData = await client.GetStringAsync($"https://www.geneanet.org/registres/api/images/{Registry.ID}?min_page=1&max_page={int.MaxValue}");
            Registry.Pages = JObject.Parse($"{{results: {pagesData}}}").Value<JArray>("results").Select(p =>
            {
                var pageNumber = p.Value<int>("page");
                var page = Registry.Pages?.FirstOrDefault(Page => Page.Number == pageNumber) ?? new RPage { Number = pageNumber };
                page.DownloadURL = $"https://www.geneanet.org{p.Value<string>("image_base_url").TrimEnd('/')}/";
                page.URL = p.Value<string>("image_route");
                return page;
            }).ToArray();
            return Registry;
        }

        static (List<RegistryType> types, string location, string notes) TryParseNotes(string page, Match infos)
        {
            var types = new List<RegistryType>();
            var global = (infos.Groups["globalType"] ?? infos.Groups["type"])?.Value.Trim(' ').ToLowerInvariant();
            foreach (var t in (infos.Groups["betterType"] ?? infos.Groups["type"])?.Value.Split(','))
                if (TryGetType(t.Trim(' ').ToLowerInvariant(), out var type)) types.Add(type);

            bool TryGetType(string type, out RegistryType t)
            {
                var civilStatus = global.Contains("état civil");
                if (type.Contains("naissances")) t = civilStatus ? RegistryType.Birth : RegistryType.BirthTable;
                else if (type.Contains("baptemes")) t = RegistryType.Baptism;
                else if (type.Contains("communions")) t = RegistryType.Communion;
                else if (type.Contains("confirmations")) t = RegistryType.Confirmation;
                else if (type.Contains("promesses de mariage")) t = RegistryType.Banns;
                else if (type.Contains("mariages")) t = civilStatus ? RegistryType.Marriage : RegistryType.MarriageTable;
                else if (type.Contains("décès")) t = civilStatus ? RegistryType.Death : RegistryType.DeathTable;
                else if (type.Contains("sépultures") || type.Contains("inhumation")) t = RegistryType.Burial;

                else if (type.Contains("recensements")) t = RegistryType.Census;
                else if (type.Contains("etat des âmes")) t = RegistryType.LiberStatutAnimarum;
                else if (type.Contains("archives notariales")) t = RegistryType.Notarial;
                else if (type.Contains("registres matricules")) t = RegistryType.Military;

                else if (type.Contains("autres") || type.Contains("archives privées")) t = RegistryType.Other;
                else
                {
                    t = RegistryType.Unknown;
                    return false;
                }
                return true;
            }

            var location = Regex.Match(page, "Paroisse de (?<location>.*?)(\\.|-|<)").Groups["location"]?.Value;
            var note = infos.Groups["notes"]?.Value;

            return (types, location, note);
        }
        #endregion

        #region Page
        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult(Page.URL);
        public async Task<Stream> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(Registry, page);
            if (success) return stream;
            return await GetTiles(Registry, page, 0, progress);
        }
        public Task<Stream> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, Zoomify.CalculateIndex(page) * 0.75F, progress);
        public Task<Stream> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, Zoomify.CalculateIndex(page), progress);
        public static async Task<Stream> GetTiles(Registry Registry, RPage page, double zoom, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetImageFromDrive(Registry, page, zoom);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");

            if (page.DownloadURL == null)
            {
                Data.Providers["Geneanet"].Registries[Registry.ID] = Registry = await UpdateInfos(Registry, client);
                page = Registry.Pages.FirstOrDefault(p => p.Number == page.Number);
                if (page == null) return null;
            }
            if (!page.TileSize.HasValue) (page.Width, page.Height, page.TileSize) = await Zoomify.ImageData(page.DownloadURL, client);

            if (page.MaxZoom == -1) page.MaxZoom = Zoomify.CalculateIndex(page);
            page.Zoom = zoom < page.MaxZoom ? (int)Math.Ceiling(zoom) : page.MaxZoom;
            var (tiles, diviser) = Zoomify.NbTiles(page, page.Zoom);

            progress?.Invoke(0);
            Image image = new Image<Rgb24>(page.Width, page.Height);
            var tasks = new Dictionary<Task<Image>, (int tileSize, int scale, Point pos)>();
            for (int y = 0; y < tiles.Y; y++)
                for (int x = 0; x < tiles.X; x++)
                    tasks.Add(Grabber.GetImage($"{page.DownloadURL}TileGroup0/{page.Zoom}-{x}-{y}.jpg", client).ContinueWith((task) =>
                    {
                        progress?.Invoke(tasks.Keys.ToList().Count(t => t.IsCompleted) / (float)tasks.Count);
                        return task.Result;
                    }), (page.TileSize.Value, diviser, new Point(x, y)));

            await Task.WhenAll(tasks.Keys).ConfigureAwait(false);
            foreach (var tile in tasks) image = image.MergeTile(tile.Key.Result, tile.Value);
            progress?.Invoke(Progress.Finished);

            Data.Providers["Geneanet"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page, image, false).ConfigureAwait(false);
            return image.ToStream();
        }
        #endregion
    }
}
