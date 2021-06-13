using SixLabors.ImageSharp;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class CG06 : ProviderAPI
    {
        public bool TryGetRegistryID(Uri URL, out string ID)
        {
            var check = URL.Host == "www.basesdocumentaires-cg06.fr" && URL.AbsolutePath.StartsWith("/os-cgi");
            if (!check) { ID = null; return false; }

            var task = GetInfos(URL);
            task.Wait();
            ID = task.Result.RegistryID;
            return !string.IsNullOrWhiteSpace(ID);
        }

        public Task<RegistryInfo> Infos(Uri URL) => GetInfos(URL);
        async Task<RegistryInfo> GetInfos(Uri URL)
        {
            var client = new HttpClient();
            string pageBody = System.Text.Encoding.UTF8.GetString(await client.GetByteArrayAsync(URL).ConfigureAwait(false)).Replace("\n", "").Replace("\r", "").Replace("\t", "");
            var form = Regex.Match(pageBody, @"document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""c\\\"" value=\\\""(?<c>.*)\\\"">\""\);.*document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""l\\\"" value=\\\""(?<l>.*?)\\\"">\""\);.*document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""t\\\"" value=\\\""(?<t>.*)\\\"">\""\);.*document\.write\(\""<\/form>\""\);").Groups;
            var body = Regex.Match(pageBody, @"<!-- CORPS DU DOCUMENT -->.*?<body style=\""text-align=justify\"">(.*?<h2 align=\""center\"">(?<title>.*?)<\/h2>)?(?<type>.*?)<b>.*?<\/b>.*?du (?<from>.*?)au (?<to>.*?)(<br>(?<source>.*?))?(<br> *?(?<pers>.*?))?<p>(.*?<p>)?(?<details>.*?)(.*?<p>.*?<hr.*?>(?<notes>.*))?(.*?)?<!-- Affichage des images -->", RegexOptions.IgnoreCase).Groups;

            var registry = new Registry(Data.Providers["CG06"])
            {
                URL = URL.OriginalString,
                ID = form["c"]?.Value,
                Notes = string.IsNullOrWhiteSpace(body["title"]?.Value) ? form["t"]?.Value : body["title"]?.Value,
                From = Data.ParseDate(body["from"]?.Value),
                To = Data.ParseDate(body["to"]?.Value),
                Types = new System.Collections.Generic.List<Registry.Type> { body["pers"]?.Value.Contains("Notaire(s)") ?? false ? Registry.Type.Notarial : Registry.Type.Unknown },
                Pages = form["l"]?.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select((p, i) => new RPage { Number = i + 1, URL = p }).ToArray()
            };

            Data.AddOrUpdate(Data.Providers["CG06"].Registries, registry.ID, registry);
            return new RegistryInfo { ProviderID = "CG06", RegistryID = registry.ID, PageNumber = 1 };
        }


        public async Task<RPage> Thumbnail(Registry Registry, RPage page)
        {
            await Data.TryGetImageFromDrive(Registry, page, 0);
            return page;
        }
        public Task<RPage> Download(Registry Registry, RPage page) => GetTile(Registry, page, 1);
        public async Task<RPage> GetTile(Registry Registry, RPage page, int zoom)
        {
            if (await Data.TryGetImageFromDrive(Registry, page, zoom)) return page;

            var client = new HttpClient();
            await client.GetStringAsync($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/ISViewerTarget.php?imagePath={page.URL}").ConfigureAwait(false); //Create the cache on server side
            page.Image = Image.Load(await client.GetStreamAsync($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/cache/{page.URL.Replace('/', '_')}").ConfigureAwait(false)); //Request the cache content
            page.Zoom = 1;

            Data.Providers["CG06"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page);
            return page;
        }
    }
}
