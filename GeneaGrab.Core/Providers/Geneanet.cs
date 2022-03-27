using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class Geneanet : ProviderAPI, IndexAPI
    {
        public string ProviderID => "Geneanet";
        public bool IndexSupport => true;

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "www.geneanet.org" || !URL.AbsolutePath.StartsWith("/registres/view")) return false;

            var regex = Regex.Match(URL.OriginalString, "(?:idcollection=(?<col>\\d*).*page=(?<page>\\d*))|(?:\\/(?<col>\\d+)(?:\\z|\\/(?<page>\\d*)))");
            info = new RegistryInfo
            {
                RegistryID = regex.Groups["col"]?.Value,
                ProviderID = ProviderID,
                PageNumber = int.TryParse(regex.Groups["page"].Success ? regex.Groups["page"].Value : "1", out var _p) ? _p : 1
            };
            return true;
        }

        #region Infos
        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var Registry = new Registry(Data.Providers[ProviderID]) { URL = URL.OriginalString };

            var regex = Regex.Match(Registry.URL, "(?:idcollection=(?<col>\\d*).*page=(?<page>\\d*))|(?:\\/(?<col>\\d+)(?:\\z|\\/(?<page>\\d*)))");
            Registry.ID = regex.Groups["col"]?.Value;
            if (string.IsNullOrEmpty(Registry.ID)) return null;

            var client = new HttpClient();
            string pages = await client.GetStringAsync($"https://www.geneanet.org/registres/api/images/{Registry.ID}");

            Registry.URL = $"https://www.geneanet.org/registres/view/{Registry.ID}";
            string page = await client.GetStringAsync(Registry.URL);
            var infos = Regex.Match(page, "Informations sur le document.*?<p>(\\[.*\\] - )?(?<location>.*) \\((?<locationDetails>.*?)\\) - (?<globalType>.*?)( \\((?<type>.*?)\\))?( - .*)? *\\| (?<from>.*) - (?<to>.*?)<\\/p>.*?<p>(?<cote>.*)</p>.*<p>(?<notaire>.*)</p>.*<p class=\\\"no-margin-bottom\\\">(?<betterType>.*?)(\\..*| -.*)?</p>.*<p>(?<note>.*)</p>.*<strong>Lien permanent : </strong>", RegexOptions.Multiline | RegexOptions.Singleline); //https://regex101.com/r/3Ou7DP/4
            Registry.Location = Registry.LocationID = infos.Groups["location"].Value.Trim(' ');
            Registry.LocationDetails = infos.Groups["locationDetails"]?.Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray() ?? new string[0];

            var notes = TryParseNotes(page, infos);
            Registry.Types = notes.types;
            Registry.Notes = notes.notes;
            Registry.District = Registry.DistrictID = string.IsNullOrWhiteSpace(notes.location) ? null : notes.location;

            Registry.From = Data.ParseDate(infos.Groups["from"].Value);
            Registry.To = Data.ParseDate(infos.Groups["to"].Value);
            Registry.Pages = JObject.Parse($"{{results: {pages}}}").Value<JArray>("results").Select(p => new RPage { Number = p.Value<int>("page"), URL = p.Value<string>("chemin_image") }).ToArray();
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

            Data.AddOrUpdate(Data.Providers[ProviderID].Registries, Registry.ID, Registry);
            return new RegistryInfo(Registry) { PageNumber = _p };
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
        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"{Registry.URL}/{Page.Number}");
        public async Task<Image> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            var tryGet = await Data.TryGetThumbnailFromDrive(Registry, page).ConfigureAwait(false);
            if (tryGet.success) return tryGet.image;
            return await GetTiles(Registry, page, 0, progress).ConfigureAwait(false);
        }
        public Task<Image> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, Zoomify.CalculateIndex(page) * 0.75F, progress);
        public Task<Image> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, Zoomify.CalculateIndex(page), progress);
        public static async Task<Image> GetTiles(Registry Registry, RPage page, double zoom, Action<Progress> progress)
        {
            var tryGet = await Data.TryGetImageFromDrive(Registry, page, zoom).ConfigureAwait(false);
            if (tryGet.success) return tryGet.image;

            progress?.Invoke(Progress.Unknown);
            var chemin_image = Uri.EscapeDataString($"doc/{page.URL}");
            var baseURL = $"https://www.geneanet.org/viewer/zoomify/api/{chemin_image}/";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");

            if (!page.TileSize.HasValue)
                (page.Width, page.Height, page.TileSize) = await Zoomify.ImageData(baseURL, client);

            if (page.MaxZoom == -1) page.MaxZoom = Zoomify.CalculateIndex(page);
            page.Zoom = zoom < page.MaxZoom ? (int)Math.Ceiling(zoom) : page.MaxZoom;
            var (tiles, diviser) = Zoomify.NbTiles(page, page.Zoom);

            progress?.Invoke(0);
            if (tryGet.image == null) tryGet.image = new Image<Rgb24>(page.Width, page.Height);
            var tasks = new Dictionary<Task<Image>, (int tileSize, int scale, Point pos)>();
            for (int y = 0; y < tiles.Y; y++)
                for (int x = 0; x < tiles.X; x++)
                    tasks.Add(Grabber.GetImage($"{baseURL}TileGroup0/{page.Zoom}-{x}-{y}.jpg", client).ContinueWith((task) =>
                    {
                        progress?.Invoke(tasks.Keys.Count(t => t.IsCompleted) / (float)tasks.Count);
                        return task.Result;
                    }), (page.TileSize.Value, diviser, new Point(x, y)));

            await Task.WhenAll(tasks.Keys).ConfigureAwait(false);
            progress?.Invoke(Progress.Finished);
            foreach (var tile in tasks) tryGet.image = tryGet.image.MergeTile(tile.Key.Result, tile.Value);

            Data.Providers["Geneanet"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page, tryGet.image, false).ConfigureAwait(false);
            return tryGet.image;
        }
        #endregion


        #region Index
        public async Task<bool> Login(string username, string password)
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = true, CookieContainer = new System.Net.CookieContainer { } });

            var loginData = new Dictionary<string, string> { { "_username", username }, { "_password", password } };
            var loginRequest = await client.PostAsync("https://www.geneanet.org/connexion/login_check", new FormUrlEncodedContent(loginData));
            if (!loginRequest.IsSuccessStatusCode) return false;

            if (loginRequest.Headers.TryGetValues("Set-Cookie", out var cookies))
                foreach (var cookie in cookies)
                {
                    //Get token
                }
            return true;
        }

        public async Task<IEnumerable<Index>> GetIndex(Registry Registry, RPage page)
        {
            var client = new HttpClient();
            //await Login("", "");
            await Task.CompletedTask;


            /*var body = await client.GetStringAsync($"https://www.geneanet.org/archives/registres/view/marqueurs-noms.php?action=showPage&idcollection={Registry.ID}&page={page.Number}");
            var xml = new FileFormat.XML.XML($"<html>{body}</html>");
            var marqueurs = xml.RootElement.GetItems("div/table/tr[1]/td/div/div/table/tbody/tr");
            await Task.CompletedTask.ConfigureAwait(false);*/
            return null;
            //return await GenericIndex.GetIndexFromLocalData(Registry, page);
        }

        public async Task AddIndex(Registry Registry, RPage page, Index index)
        {
            await GenericIndex.AddIndexToLocalData(Registry, page, index);
        }
        #endregion
    }
}
