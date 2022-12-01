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
    public class AD79_86 : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri url, out RegistryInfo info)
        {
            info = null;
            if (url.Host != "archives-deux-sevres-vienne.fr" || !url.AbsolutePath.StartsWith("/ark:/")) return false;

            info = new RegistryInfo
            {
                ProviderID = "AD79-86",
                RegistryID = Regex.Match(url.AbsolutePath, "$/ark:/").Groups["id"]?.Value
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri url)
        {
            var queries = Regex.Match(url.AbsolutePath, "/ark:/(?<something>.*?)/(?<id>.*?)/daogrp/(?<seq>\\d*?)/(?<page>\\d*?)/").Groups;
            var registry = new Registry(Data.Providers["AD79-86"]) { ID = queries["id"]?.Value };
            registry.URL = $"https://archives-deux-sevres-vienne.fr/ark:/{queries["something"]?.Value}/{registry.ID}";

            var client = new HttpClient();
            var iiif = new IIIF.Manifest(await client.GetStringAsync($"{registry.URL}/manifest"));
            int.TryParse(queries["seq"]?.Value, out var seq);
            var sequence = iiif.Sequences.ElementAt(seq);

            registry.Pages = sequence.Canvases.Select(p => new RPage
            {
                Number = int.Parse(p.Label),
                URL = p.Images.First().ServiceId,
                DownloadURL = p.Images.First().Id,
                Extra = p.Json["ligeoClasseur"]
            }).ToArray();

            var dates = sequence.Label.Split(new[] { "- (" }, StringSplitOptions.RemoveEmptyEntries).Last().Replace(") ", "").Split('-');
            registry.From = Core.Models.Dates.Date.ParseDate(dates.First());
            registry.To = Core.Models.Dates.Date.ParseDate(dates.Last());
            registry.Types = ParseTypes(iiif.MetaData["Type de document"]);
            registry.CallNumber = iiif.MetaData["Cote"];
            registry.Notes = GenerateNotes(iiif.MetaData);
            registry.Location = iiif.MetaData["Commune"];
            registry.District = iiif.MetaData.TryGetValue("Paroisse", out var paroisse) ? paroisse : null;
            registry.ArkURL = sequence.Id;

            Data.AddOrUpdate(Data.Providers["AD79-86"].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = 1 };
        }
        private static string GenerateNotes(IReadOnlyDictionary<string, string> metaData)
        {
            var notes = new List<string>();
            if (metaData.TryGetValue("Documents de substitution", out var docSub)) notes.Add($"Documents de substitution: {docSub}");
            if (metaData.TryGetValue("Présentation du contenu", out var presentation)) notes.Add(presentation);
            return notes.Count == 0 ? null : string.Join("\n\n", notes);
        }
        private static IEnumerable<RegistryType> ParseTypes(string types)
        {
            foreach (var type in types.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (type == "naissance") yield return RegistryType.Birth;
                if (type == "mariage") yield return RegistryType.Marriage;
                if (type == "décès") yield return RegistryType.Death;
            }
        }

        public Task<string> Ark(Registry registry, RPage page) => Task.FromResult($"{registry.ArkURL}/{page.Number}");
        public async Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(registry, page);
            if (success) return stream;
            return await GetTiles(registry, page, 0.1F, progress);
        }
        public Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 0.5F, progress);
        public Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 1, progress);
        private static async Task<Stream> GetTiles(Registry registry, RPage page, float scale, Action<Progress> progress)
        {
            var zoom = (int)(scale * 100);
            var (success, stream) = await Data.TryGetImageFromDrive(registry, page, zoom);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var image = await Image.LoadAsync(await client.GetStreamAsync(zoom == 100 ? new Uri(page.DownloadURL) : IIIF.IIIF.GenerateImageRequestUri(page.URL, size: $"pct:{zoom}")).ConfigureAwait(false)).ConfigureAwait(false);
            page.Zoom = zoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD79-86"].Registries[registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(registry, page, image, false);
            return image.ToStream();
        }
    }
}
