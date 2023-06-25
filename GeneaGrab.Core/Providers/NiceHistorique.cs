using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;
using GeneaGrab.Core.Models.Dates;

namespace GeneaGrab.Core.Providers
{
    public class NiceHistorique : ProviderAPI
    {
        public string ProviderID => "NiceHistorique";
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri url, out RegistryInfo info)
        {
            info = null;
            if (url.Host != "www.nicehistorique.org" || !url.AbsolutePath.StartsWith("/vwr")) return false;

            //TODO: Find a way to do this without having to make a request
            var task = GetInfos(url);
            task.Wait();
            info = task.Result;
            return true;
        }

        public Task<RegistryInfo> Infos(Uri url) => GetInfos(url);
        async Task<RegistryInfo> GetInfos(Uri url)
        {
            var client = new HttpClient();
            var pageBody = await client.GetStringAsync(url).ConfigureAwait(false);

            var data = Regex.Match(pageBody, "<h2>R&eacute;f&eacute;rence :  (?<title>.* (?<number>\\d*)) de l'ann&eacute;e (?<year>\\d*).*<\\/h2>").Groups;
            var date = Date.ParseDate(data["year"]?.Value);

            var pageData = Regex.Match(pageBody, "var pages = Array\\((?<pages>.*)\\);\\n.*var path = \"(?<path>.*)\";").Groups;
            Uri.TryCreate(url, pageData["path"].Value, out var path);
            var pages = pageData["pages"].Value.Split(new[] { ", " }, StringSplitOptions.None);

            var pagesTable = Regex.Matches(pageBody, "<a href=\"#\" class=\"(?<class>.*)\" onclick=\"doc\\.set\\('(?<index>\\d*)'\\); return false;\" title=\".*\">(?<number>\\d*)<\\/a>").Cast<Match>().ToArray();

            var registry = new Registry(Data.Providers[ProviderID])
            {
                URL = url.OriginalString,
                Types = new[] { RegistryType.Periodical },
                ProviderID = ProviderID,
                ID = data["number"]?.Value,
                CallNumber = HttpUtility.HtmlDecode(data["title"]?.Value),
                From = date,
                To = date,
                Pages = pagesTable.Select(page =>
                {
                    var pData = HttpUtility.UrlDecode(pages[int.Parse(page.Groups["index"].Value) - 1]).Trim('"', ' ');
                    return new RPage
                    {
                        Number = int.Parse(page.Groups["number"].Value),
                        URL = $"{path.AbsoluteUri}{pData}"
                    };
                }).ToArray()
            };

            Data.AddOrUpdate(Data.Providers[ProviderID].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = int.Parse(pagesTable.FirstOrDefault(p => p.Groups["class"]?.Value == "current")?.Groups["index"]?.Value ?? "1") };
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
        private async Task<Stream> GetTile(Registry registry, RPage page, int zoom, Action<Progress> progress)
        {
            var (success, stream) = Data.TryGetImageFromDrive(registry, page, zoom);
            if (success) return stream;
            var index = Array.IndexOf(registry.Pages, page);

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var image = await Grabber.GetImage(page.URL, client).ConfigureAwait(false);
            page.Zoom = 1;
            progress?.Invoke(Progress.Finished);

            Data.Providers[ProviderID].Registries[registry.ID].Pages[index] = page;
            await Data.SaveImage(registry, page, image, false).ConfigureAwait(false);
            return image.ToStream();
        }
    }
}
