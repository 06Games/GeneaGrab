using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class CG06 : ProviderAPI
    {
        public bool IndexSupport => false;

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
        async Task<RegistryInfo> GetInfos(Uri URL)
        {
            var client = new HttpClient();
            string pageBody = Regex.Replace(System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252).GetString(await client.GetByteArrayAsync(URL).ConfigureAwait(false)).Replace("\n", "").Replace("\r", "").Replace("\t", ""), "<font color=#0000FF><b>(?<content>.*)<\\/b><\\/font>", m => m.Groups["content"]?.Value, RegexOptions.IgnoreCase);
            var form = Regex.Match(pageBody, @"document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""c\\\"" value=\\\""(?<c>.*)\\\"">\""\);.*document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""l\\\"" value=\\\""(?<l>.*?)\\\"">\""\);.*document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""t\\\"" value=\\\""(?<t>.*)\\\"">\""\);.*document\.write\(\""<\/form>\""\);").Groups;
            var body = Regex.Match(pageBody, @"<!-- CORPS DU DOCUMENT -->.*?<body style=\""text-align=justify\"">(.*?<h2 align=\""center\"">(?<title>.*?)<\/h2>)?(?<type>.*?)<b>.*?<\/b>.*?du (?<from>.*?)au (?<to>.*?)(<br>(?<source>.*?))?(<br> *?(?<pers>.*?))?<p>(.*?<p>)?(?<details>.*?)(.*?<p>.*?<hr.*?>(?<notes>.*))?(.*?)?<!-- Affichage des images -->", RegexOptions.IgnoreCase).Groups;

            var registry = new Registry(Data.Providers["CG06"])
            {
                URL = URL.OriginalString,
                ID = form["c"]?.Value,
                CallNumber = form["c"]?.Value,
                From = Data.ParseDate(body["from"]?.Value),
                To = Data.ParseDate(body["to"]?.Value),
                Pages = form["l"]?.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select((p, i) => new RPage { Number = i + 1, URL = p }).ToArray()
            };

            //Additional analysis
            ModuleExtractor(GetModuleName(URL), ref registry);
            string GetModuleName(Uri url) => System.Web.HttpUtility.ParseQueryString(url.Query)["fnmq"].Split('/').FirstOrDefault();
            void ModuleExtractor(string module, ref Registry reg)
            {
                if (module == "arno") //Archives notariales
                {
                    var additionnalInfo = Regex.Match(body["pers"]?.Value ?? "", "Notaire\\(s\\) (?<notary>.*) à (?<city>.*)").Groups;
                    reg.Location = additionnalInfo["city"]?.Value;
                    reg.Notes = Notes((form["t"] ?? additionnalInfo["notary"])?.Value, body["notes"]?.Value);
                    reg.Types = new[] { RegistryType.Notarial };
                }
                else if (module == "arca") //Archives anciennes & révolutionnaires
                {
                    reg.Location = string.IsNullOrWhiteSpace(body["title"]?.Value) ? form["t"]?.Value : body["title"]?.Value;
                    reg.Notes = body["notes"]?.Value;
                    reg.Types = new[] { RegistryType.Other };
                }
                else if (module == "comm") //Archives communales & hospitalières
                {
                    reg.Location = form["t"]?.Value;
                    reg.Notes = Notes(body["title"]?.Value, body["notes"]?.Value);
                    reg.Types = new[] { RegistryType.Other };
                }
                else
                {
                    reg.Notes = string.IsNullOrWhiteSpace(body["title"]?.Value) ? form["t"]?.Value : body["title"]?.Value;
                    reg.Types = new[] { RegistryType.Unknown };
                }

                string Notes(params string[] notes) => string.Join("\n\n", notes.Where(n => !string.IsNullOrWhiteSpace(n)));
            }

            Data.AddOrUpdate(Data.Providers["CG06"].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = 1 };
        }


        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"p{Page.Number}");
        public async Task<SixLabors.ImageSharp.Image> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            var tryGet = await Data.TryGetThumbnailFromDrive(Registry, page);
            if (tryGet.success) return tryGet.image;
            return await GetTile(Registry, page, 0, progress);
        }
        public Task<SixLabors.ImageSharp.Image> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTile(Registry, page, 1, progress);
        public Task<SixLabors.ImageSharp.Image> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTile(Registry, page, 1, progress);
        public async Task<SixLabors.ImageSharp.Image> GetTile(Registry Registry, RPage page, int zoom, Action<Progress> progress)
        {
            var tryGet = await Data.TryGetImageFromDrive(Registry, page, zoom);
            if (tryGet.success) return tryGet.image;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            await client.GetStringAsync($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/ISViewerTarget.php?imagePath={page.URL}").ConfigureAwait(false); //Create the cache on server side
            var image = await Grabber.GetImage($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/cache/{page.URL.Replace('/', '_')}", client); //Request the cache content
            page.Zoom = 1;
            progress?.Invoke(Progress.Finished);

            Data.Providers["CG06"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page, image, false);
            return image;
        }
    }
}
