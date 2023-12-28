using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;
using GeneaGrab.Core.Models.Dates;
using SixLabors.ImageSharp;

namespace GeneaGrab.Core.Providers
{
    public class Antenati : Provider
    {
        public override string Id => "Antenati";
        public override string Url => "https://www.antenati.san.beniculturali.it/";

        public override async Task<RegistryInfo> GetRegistryFromUrlAsync(Uri url)
        {
            if (url.Host != "dam-antenati.san.beniculturali.it" || !url.AbsolutePath.StartsWith("/antenati/containers/")) return null;

            return new RegistryInfo
            {
                ProviderId = "Antenati",
                RegistryId = Regex.Match(url.AbsolutePath, "$/antenati/containers/(?<id>.*?)/").Groups["id"].Value
            };
        }

        public override async Task<(Registry, int)> Infos(Uri url)
        {
            var registry = new Registry(Data.Providers["Antenati"]) { RemoteId = Regex.Match(url.AbsolutePath, "/antenati/containers/(?<id>.*?)/").Groups["id"]?.Value };
            registry.URL = $"https://dam-antenati.san.beniculturali.it/antenati/containers/{registry.RemoteId}";

            var client = new HttpClient();
            var iiif = new Iiif(await client.GetStringAsync($"{registry.URL}/manifest"));

            registry.Frames = iiif.Sequences.First().Canvases.Select(p => new Frame
            {
                FrameNumber = int.Parse(p.Label.Substring("pag. ".Length)),
                DownloadUrl = p.Images.First().ServiceId
            }).ToArray();

            var dates = iiif.MetaData["Datazione"].Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            registry.From = Date.ParseDate(dates[0]);
            registry.To = Date.ParseDate(dates[1]);
            registry.Types = ParseTypes(new[] { iiif.MetaData["Tipologia"] }).ToArray();
            var location = iiif.MetaData["Contesto archivistico"].Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries);
            registry.Location = location;
            registry.ArkURL = Regex.Match(iiif.MetaData["Vedi il registro"], "<a .*>(?<url>.*)</a>").Groups["url"]?.Value;

            return (registry, 1);
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

        public override Task<string> Ark(Frame page) => Task.FromResult($"{page.Registry.ArkURL} (p{page.FrameNumber})");

        public override async Task<Stream> GetFrame(Frame page, Scale scale, Action<Progress> progress)
        {
            var (success, stream) = Data.TryGetImageFromDrive(page, scale);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var size = scale switch
            {
                Scale.Thumbnail => "!512,512",
                Scale.Navigation => "!2048,2048",
                _ => "max"
            };
            var image = await Image.LoadAsync(await client.GetStreamAsync(Iiif.GenerateImageRequestUri(page.DownloadUrl, size: size)).ConfigureAwait(false)).ConfigureAwait(false);
            page.ImageSize = scale;
            progress?.Invoke(Progress.Finished);

            await Data.SaveImage(page, image, false).ConfigureAwait(false);
            return image.ToStream();
        }
    }
}
