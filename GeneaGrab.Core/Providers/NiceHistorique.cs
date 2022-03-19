using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class NiceHistorique : ProviderAPI
    {
        public string ProviderID => "NiceHistorique";
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "www.nicehistorique.org" || !URL.AbsolutePath.StartsWith("/vwr")) return false;

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
            string pageBody = await client.GetStringAsync(URL).ConfigureAwait(false);

            var data = Regex.Match(pageBody, "<h2>R&eacute;f&eacute;rence :  (?<title>.* (?<number>\\d*)) de l'ann&eacute;e (?<year>\\d*).*<\\/h2>").Groups;
            var date = Data.ParseDate(data["year"]?.Value);

            var pageData = Regex.Match(pageBody, "var pages = Array\\((?<pages>.*)\\);\\n.*var path = \"(?<path>.*)\";").Groups;
            Uri.TryCreate(URL, pageData["path"].Value, out var path);
            var pages = pageData["pages"].Value.Split(new[] { ", " }, StringSplitOptions.None);

            var pagesTable = Regex.Matches(pageBody, "<a href=\"#\" class=\"(?<class>.*)\" onclick=\"doc\\.set\\('(?<index>\\d*)'\\); return false;\" title=\".*\">(?<number>\\d*)<\\/a>").Cast<Match>();

            var registry = new Registry(Data.Providers[ProviderID])
            {
                URL = URL.OriginalString,
                Types = new[] { RegistryType.Periodical },
                ProviderID = ProviderID,
                ID = data["number"]?.Value,
                CallNumber = System.Web.HttpUtility.HtmlDecode(data["title"]?.Value),
                From = date,
                To = date,
                Pages = pagesTable.Select(page =>
                {
                    var pData = System.Web.HttpUtility.UrlDecode(pages[int.Parse(page.Groups["index"].Value) - 1]).Trim('"', ' ');
                    return new RPage {
                        Number = int.Parse(page.Groups["number"].Value),
                        URL = $"{path.AbsoluteUri}{pData}"
                    };
                }).ToArray()
            };

            Data.AddOrUpdate(Data.Providers[ProviderID].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = int.Parse(pagesTable.FirstOrDefault(p => p.Groups["class"]?.Value == "current").Groups["index"]?.Value) };
        }


        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"p{Page.Number}");
        public async Task<RPage> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            if (await Data.TryGetThumbnailFromDrive(Registry, page)) return page;
            return await GetTile(Registry, page, 0, progress);
        }
        public Task<RPage> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTile(Registry, page, 1, progress);
        public Task<RPage> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTile(Registry, page, 1, progress);
        public async Task<RPage> GetTile(Registry Registry, RPage page, int zoom, Action<Progress> progress)
        {
            if (await Data.TryGetImageFromDrive(Registry, page, zoom)) return page;
            var index = Array.IndexOf(Registry.Pages, page);

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            page.Image = await Grabber.GetImage(page.URL, client);
            page.Zoom = 1;
            progress?.Invoke(Progress.Finished);

            Data.Providers[ProviderID].Registries[Registry.ID].Pages[index] = page;
            await Data.SaveImage(Registry, page, false);
            return page;
        }
    }
}
