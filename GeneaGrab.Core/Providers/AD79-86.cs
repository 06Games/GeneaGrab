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
    public class AD79_86 : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "archives-deux-sevres-vienne.fr" || !URL.AbsolutePath.StartsWith("/ark:/")) return false;

            info = new RegistryInfo
            {
                ProviderID = "AD79-86",
                RegistryID = Regex.Match(URL.AbsolutePath, "$/ark:/").Groups["id"]?.Value
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var queries = Regex.Match(URL.AbsolutePath, "/ark:/(?<something>.*?)/(?<id>.*?)/daogrp/(?<seq>\\d*?)/(?<page>\\d*?)/").Groups;
            var Registry = new Registry(Data.Providers["AD79-86"]) { ID = queries["id"]?.Value };
            Registry.URL = $"https://archives-deux-sevres-vienne.fr/ark:/{queries["something"]?.Value}/{Registry.ID}";

            var client = new HttpClient();
            var iiif = new IIIF.Manifest(await client.GetStringAsync($"{Registry.URL}/manifest"));
            int.TryParse(queries["seq"]?.Value, out var seq);
            var sequence = iiif.Sequences.ElementAt(seq);

            Registry.Pages = sequence.Canvases.Select(p => new RPage
            {
                Number = int.Parse(p.Label),
                URL = p.Images.First().ServiceId,
                DownloadURL = p.Images.First().Id,
                Extra = p.Json["ligeoClasseur"]
            }).ToArray();

            var dates = sequence.Label.Split(new[] { "- (" }, StringSplitOptions.RemoveEmptyEntries).Last().Replace(") ", "").Split('-');
            Registry.From = Data.ParseDate(dates.First());
            Registry.To = Data.ParseDate(dates.Last());
            Registry.Types = ParseTypes(iiif.MetaData["Type de document"]);
            Registry.CallNumber = iiif.MetaData["Cote"];
            Registry.Notes = GenerateNotes(iiif.MetaData);
            Registry.Location = iiif.MetaData["Commune"];
            Registry.District = iiif.MetaData.TryGetValue("Paroisse", out var paroisse) ? paroisse : null;
            Registry.ArkURL = sequence.Id;

            Data.AddOrUpdate(Data.Providers["AD79-86"].Registries, Registry.ID, Registry);
            return new RegistryInfo(Registry) { PageNumber = 1 };
        }
        string GenerateNotes(Dictionary<string, string> MetaData)
        {
            var notes = new List<string>();
            if (MetaData.TryGetValue("Documents de substitution", out var docSub)) notes.Add($"Documents de substitution: {docSub}");
            if (MetaData.TryGetValue("Présentation du contenu", out var presentation)) notes.Add(presentation);
            return notes.Count == 0 ? null : string.Join("\n\n", notes);
        }
        IEnumerable<RegistryType> ParseTypes(string types)
        {
            foreach (var type in types.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (type == "naissance") yield return RegistryType.Birth;
                if (type == "mariage") yield return RegistryType.Marriage;
                if (type == "décès") yield return RegistryType.Death;
            }
        }

        public Task<string> Ark(Registry Registry, RPage Page) => Task.FromResult($"{Registry.ArkURL}/{Page.Number}");
        public async Task<Image> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            var tryGet = await Data.TryGetThumbnailFromDrive(Registry, page);
            if (tryGet.success) return tryGet.image;
            return await GetTiles(Registry, page, 0.1F, progress);
        }
        public Task<Image> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.5F, progress);
        public Task<Image> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 1, progress);
        public static async Task<Image> GetTiles(Registry Registry, RPage page, float scale, Action<Progress> progress)
        {
            int zoom = (int)(scale * 100);
            var tryGet = await Data.TryGetImageFromDrive(Registry, page, zoom);
            if (tryGet.success) return tryGet.image;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var image = await Image.LoadAsync(await client.GetStreamAsync(zoom == 100 ? new Uri(page.DownloadURL) : IIIF.IIIF.GenerateImageRequestUri(page.URL, size: $"pct:{zoom}")).ConfigureAwait(false)).ConfigureAwait(false);
            page.Zoom = zoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD79-86"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page, image, false);
            return image;
        }
    }
}
