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
                URL = p.Images.First().ServiceId
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

            Data.Providers["AD79-86"].Registries[Registry.ID].Pages[current.Number - 1] = current;
            await Data.SaveImage(Registry, current);
            return current;
        }
    }
}
