using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class Antenati : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri url, out RegistryInfo info)
        {
            info = null;
            if (url.Host != "dam-antenati.san.beniculturali.it" || !url.AbsolutePath.StartsWith("/antenati/containers/")) return false;

            info = new RegistryInfo
            {
                ProviderID = "Antenati",
                RegistryID = Regex.Match(url.AbsolutePath, "$/antenati/containers/(?<id>.*?)/").Groups["id"]?.Value
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri url)
        {
            var registry = new Registry(Data.Providers["Antenati"]) { ID = Regex.Match(url.AbsolutePath, "/antenati/containers/(?<id>.*?)/").Groups["id"]?.Value };
            registry.URL = $"https://dam-antenati.san.beniculturali.it/antenati/containers/{registry.ID}";

            var client = new HttpClient();
            var iiif = new IIIF.Manifest(await client.GetStringAsync($"{registry.URL}/manifest"));

            registry.Pages = iiif.Sequences.First().Canvases.Select(p => new RPage
            {
                Number = int.Parse(p.Label.Substring("pag. ".Length)),
                URL = p.Images.First().ServiceId
            }).ToArray();

            var dates = iiif.MetaData["Datazione"].Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            registry.From = Core.Models.Dates.Date.ParseDate(dates[0]);
            registry.To = Core.Models.Dates.Date.ParseDate(dates[1]);
            registry.Types = ParseTypes(new[] { iiif.MetaData["Tipologia"] });
            var location = iiif.MetaData["Contesto archivistico"].Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries);
            registry.Location = location.Last();
            registry.LocationDetails = location.Take(location.Length - 1).ToArray();
            registry.ArkURL = Regex.Match(iiif.MetaData["Vedi il registro"], "<a .*>(?<url>.*)</a>").Groups["url"]?.Value;

            Data.AddOrUpdate(Data.Providers["Antenati"].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = 1 };
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

        public Task<string> Ark(Registry registry, RPage page) => Task.FromResult($"{registry.ArkURL} (p{page.Number})");
        public async Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(registry, page).ConfigureAwait(false);
            if (success) return stream;
            return await GetTiles(registry, page, 0.1F, progress).ConfigureAwait(false);
        }
        public Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 0.5F, progress);
        public Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 1, progress);
        public static async Task<Stream> GetTiles(Registry registry, RPage page, float scale, Action<Progress> progress)
        {
            var zoom = (int)(scale * 100);
            var (success, stream) = await Data.TryGetImageFromDrive(registry, page, zoom).ConfigureAwait(false);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var image = await Image.LoadAsync(await client.GetStreamAsync(IIIF.IIIF.GenerateImageRequestUri(page.URL, size: $"pct:{zoom}")).ConfigureAwait(false)).ConfigureAwait(false);
            page.Zoom = zoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["Antenati"].Registries[registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(registry, page, image, false).ConfigureAwait(false);
            return image.ToStream();
        }
    }
}
