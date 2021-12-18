using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class Antenati : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "dam-antenati.san.beniculturali.it" || !URL.AbsolutePath.StartsWith("/antenati/containers/")) return false;

            info = new RegistryInfo
            {
                ProviderID = "Antenati",
                RegistryID = Regex.Match(URL.AbsolutePath, "$/antenati/containers/(?<id>.*?)/").Groups["id"]?.Value
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var Registry = new Registry(Data.Providers["Antenati"]) { ID = Regex.Match(URL.AbsolutePath, "/antenati/containers/(?<id>.*?)/").Groups["id"]?.Value };
            Registry.URL = $"https://dam-antenati.san.beniculturali.it/antenati/containers/{Registry.ID}";

            var client = new HttpClient();
            var iiif = new IIIF.Manifest(await client.GetStringAsync($"{Registry.URL}/manifest"));

            Registry.Pages = iiif.Sequences.First().Canvases.Select(p => new RPage
            {
                Number = int.Parse(p.Label.Substring("pag. ".Length)),
                URL = p.Images.First().ServiceId
            }).ToArray();

            var dates = iiif.MetaData["Datazione"].Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            Registry.From = Data.ParseDate(dates[0]);
            Registry.To = Data.ParseDate(dates[1]);
            Registry.Types = ParseTypes(new[] { iiif.MetaData["Tipologia"] });
            Registry.Location = iiif.MetaData["Contesto archivistico"].Replace(" > ", ", ");
            Registry.ArkURL = Regex.Match(iiif.MetaData["Vedi il registro"], "<a .*>(?<url>.*)</a>").Groups["url"]?.Value;

            Data.AddOrUpdate(Data.Providers["Antenati"].Registries, Registry.ID, Registry);
            return new RegistryInfo { ProviderID = "Antenati", RegistryID = Registry.ID, PageNumber = 1 };
        }
        IEnumerable<RegistryType> ParseTypes(string[] types)
        {
            foreach (var type in types)
            {
                if (type == "Nati") yield return RegistryType.Birth;
                if (type == "Matrimoni") yield return RegistryType.Marriage;
                if (type == "Morti") yield return RegistryType.Death;
            }
        }

        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"{Registry.ArkURL} (p{Page.Number})");
        public Task<RPage> Thumbnail(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.1F, progress);
        public Task<RPage> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.5F, progress);
        public Task<RPage> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 1, progress);
        public static async Task<RPage> GetTiles(Registry Registry, RPage current, float scale, Action<Progress> progress)
        {
            int zoom = (int)(scale * 100);
            if (await Data.TryGetImageFromDrive(Registry, current, zoom)) return current;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            current.Image = await Image.LoadAsync(await client.GetStreamAsync(IIIF.IIIF.GenerateImageRequestUri(current.URL, size: $"pct:{zoom}")).ConfigureAwait(false)).ConfigureAwait(false);
            current.Zoom = zoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["Antenati"].Registries[Registry.ID].Pages[current.Number - 1] = current;
            await Data.SaveImage(Registry, current);
            return current;
        }
    }
}
