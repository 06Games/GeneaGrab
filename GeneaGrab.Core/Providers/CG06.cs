using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;
using GeneaGrab.Core.Models.Dates;

namespace GeneaGrab.Core.Providers
{
    public class CG06 : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri url, out RegistryInfo info)
        {
            info = null;
            if (url.Host != "www.basesdocumentaires-cg06.fr" || !url.AbsolutePath.StartsWith("/os-cgi")) return false;

            //TODO: Find a way to do this without having to make a request
            var task = GetInfos(url);
            task.Wait();
            info = task.Result;
            return true;
        }

        public Task<RegistryInfo> Infos(Uri url) => GetInfos(url);
        private static async Task<RegistryInfo> GetInfos(Uri url)
        {
            var client = new HttpClient();
            var pageBody = Regex.Replace(CodePagesEncodingProvider.Instance.GetEncoding(1252).GetString(await client.GetByteArrayAsync(url).ConfigureAwait(false)).Replace("\n", "").Replace("\r", "").Replace("\t", ""), "<font color=#0000FF><b>(?<content>.*)<\\/b><\\/font>", m => m.Groups["content"]?.Value, RegexOptions.IgnoreCase);
            var form = Regex.Match(pageBody, @"document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""c\\\"" value=\\\""(?<c>.*)\\\"">\""\);.*document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""l\\\"" value=\\\""(?<l>.*?)\\\"">\""\);.*document\.write\(\""<input type=\\\""hidden\\\"" name=\\\""t\\\"" value=\\\""(?<t>.*)\\\"">\""\);.*document\.write\(\""<\/form>\""\);").Groups;
            var body = Regex.Match(pageBody, @"<!-- CORPS DU DOCUMENT -->.*?<body style=\""text-align=justify\"">(.*?<h2 align=\""center\"">(?<title>.*?)<\/h2>)?(?<type>.*?)<b>.*?<\/b>.*?du (?<from>.*?)au (?<to>.*?)(<br>(?<source>.*?))?(<br> *?(?<pers>.*?))?<p>(.*?<p>)?(?<details>.*?)(.*?<p>.*?<hr.*?>(?<notes>.*))?(.*?)?<!-- Affichage des images -->", RegexOptions.IgnoreCase).Groups;

            var registry = new Registry(Data.Providers["CG06"])
            {
                URL = url.OriginalString,
                ID = form["c"]?.Value,
                CallNumber = form["c"]?.Value,
                From = Date.ParseDate(body["from"]?.Value),
                To = Date.ParseDate(body["to"]?.Value),
                Pages = form["l"]?.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select((p, i) => new RPage { Number = i + 1, URL = p }).ToArray()
            };

            //Additional analysis
            ModuleExtractor(GetModuleName(url), ref registry);
            string GetModuleName(Uri uri) => HttpUtility.ParseQueryString(uri.Query)["fnmq"].Split('/').FirstOrDefault();
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


        public Task<string> Ark(Registry registry, RPage page) => Task.FromResult($"p{page.Number}");
        public async Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(registry, page).ConfigureAwait(false);
            if (success) return stream;
            return null;
        }
        public Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress) => GetTile(registry, page, 1, progress);
        public Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress) => GetTile(registry, page, 1, progress);
        private static async Task<Stream> GetTile(Registry registry, RPage page, int zoom, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetImageFromDrive(registry, page, zoom);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            await client.GetStringAsync($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/ISViewerTarget.php?imagePath={page.URL}").ConfigureAwait(false); //Create the cache on server side
            var image = await Grabber.GetImage($"http://www.basesdocumentaires-cg06.fr/ISVIEWER/cache/{page.URL.Replace('/', '_')}", client); //Request the cache content
            page.Zoom = 1;
            progress?.Invoke(Progress.Finished);

            Data.Providers["CG06"].Registries[registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(registry, page, image, false);
            return image.ToStream();
        }
    }
}
