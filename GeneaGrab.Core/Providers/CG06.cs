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
        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "www.basesdocumentaires-cg06.fr" || !URL.AbsolutePath.StartsWith("/os-cgi")) return false;

            //TODO: Find a way to do this without having to make a request
            var task = GetInfos(URL);
            task.Wait();
            info = task.Result;
            return true;
        }

        public Task<RegistryInfo> Infos(Uri URL) => GetInfos(URL);
        async Task<RegistryInfo> GetInfos(Uri URL) //TODO: Parse city name
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
                Types = new System.Collections.Generic.List<RegistryType> { body["pers"]?.Value.Contains("Notaire(s)") ?? false ? RegistryType.Notarial : RegistryType.Unknown },
                Pages = form["l"]?.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select((p, i) => new RPage { Number = i + 1, URL = p }).ToArray()
            };

            Data.AddOrUpdate(Data.Providers["CG06"].Registries, registry.ID, registry);
            return new RegistryInfo { ProviderID = "CG06", RegistryID = registry.ID, PageNumber = 1 };
        }


        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"p{Page.Number}");
        public async Task<RPage> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            await Data.TryGetImageFromDrive(Registry, page, 0);
            return page;
        }
        public Task<RPage> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTile(Registry, page, 1, progress);
        public Task<RPage> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTile(Registry, page, 1, progress);
        public async Task<RPage> GetTile(Registry Registry, RPage page, int zoom, Action<Progress> progress)
        {
            if (await Data.TryGetImageFromDrive(Registry, page, zoom)) return page;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            await client.GetStringAsync($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/ISViewerTarget.php?imagePath={page.URL}").ConfigureAwait(false); //Create the cache on server side
            page.Image = await Grabber.GetImage($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/cache/{page.URL.Replace('/', '_')}", client); //Request the cache content
            page.Zoom = 1;
            progress?.Invoke(Progress.Finished);

            Data.Providers["CG06"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page);
            return page;
        }
    }
}
