using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;
using SixLabors.ImageSharp;

namespace GeneaGrab.Core.Providers
{
    public class AD06 : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri url, out RegistryInfo info)
        {
            info = null;
            if (url.Host != "archives06.fr" || !url.AbsolutePath.StartsWith("/ark:/")) return false;

            info = new RegistryInfo
            {
                ProviderID = "AD06",
                RegistryID = Regex.Match(url.AbsolutePath, "$/ark:/").Groups["id"].Value
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri url)
        {
            var queries = Regex.Match(url.AbsolutePath, "/ark:/(?<something>.*?)/(?<id>.*?)/(?<tag>.*?)/(?<seq>\\d*?)/((?<page>\\d*?)/)?").Groups;
            var registry = new Registry(Data.Providers["AD06"]) { ID = queries["id"].Value };
            registry.URL = $"https://archives06.fr/ark:/{queries["something"].Value}/{registry.ID}";

            var client = new HttpClient();
            var manifest = new LigeoManifest(await client.GetStringAsync($"{registry.URL}/manifest"));
            if (!int.TryParse(queries["seq"].Value, out var seq)) Data.Warn($"Couldn't parse sequence ({queries["seq"].Value}), using default one", null);
            var sequence = manifest.Sequences.ElementAt(seq);

            registry.Pages = sequence.Canvases.Select((p, i) => new RPage
            {
                Number = int.TryParse(p.Label, out var number) ? number : i + 1,
                URL = p.Images.First().ServiceId,
                Width = p.Images.First().Width,
                Height = p.Images.First().Height,
                Extra = p.Classeur
            }).ToArray();

            var classeur = sequence.Canvases.First().Classeur;
            registry.CallNumber = classeur.UnitId;
            registry.ArkURL = sequence.Id;

            // ReSharper disable StringLiteralTypo
            var notes = new List<string>();
            foreach (var metadata in manifest.MetaData)
            {
                switch (metadata.Key)
                {
                    case "Commune":
                    case "Commune d’exercice du notaire":
                    case "Lieu":
                        registry.Location = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(metadata.Value.ToLower());
                        break;
                    case "Paroisse":
                        registry.District = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(metadata.Value.ToLower());
                        break;
                    case "Date":
                    case "Date de l'acte":
                    case "Année (s)":
                    {
                        var dates = metadata.Value.Split('-');
                        registry.From = dates.FirstOrDefault()?.Trim();
                        registry.To = dates.Length == 2 ? dates.Last().Trim() : null;
                        break;
                    }
                    case "Typologie":
                    case "Type de document":
                    case "Type d'acte":
                        registry.Types = registry.Types.Concat(GetTypes(metadata.Value));
                        break;
                    default:
                        notes.Add($"{metadata.Key}: {metadata.Value}");
                        break;
                }
            }
            
            var labelRegexExp = classeur.EncodedArchivalDescriptionId.ToUpperInvariant() switch
            {
                "FRAD006_ETAT_CIVIL" => "(?<callnum>.+) +- +(?<type>.*?) *?- *?\\((?<from>.+) à (?<to>.+)\\)",
                "FRAD006_CADASTRE_MATRICE" => "(?<callnum>.+) +- +(?<title>.*?) *?-",
                "FRAD006_CADASTRE_ETAT_SECTION" => "(?<callnum>.+) +- +(?<title>.*?) *?-",
                "FRAD006_RECENSEMENT_POPULATION" => "(?<city>.+) +- +(?<from>.+)(, (?<title>.*))",
                "FRAD006_REPERTOIRE_NOTAIRES" => "(?<callnum>.+) +- +(?<title>.+)",
                "FRAD006_ARMOIRIES" => "(?<callnum>.+) +- +(?<title>.+)",
                "FRAD006_OUVRAGES" => "(?<callnum>.+) +- +(?<title>.+)",
                "FRAD006_BN_SOURCES_IMPRIMES" => "(?<title>.+)",
                "FRAD006_ANNUAIRES" => "(?<title>.+)",
                "FRAD006_DELIBERATIONS_CONSEIL_GENERAL" => "(?<callnum>.+) +- +(?<title>.+) +- +(?<from>.+?)(-(?<to>.+))?$",
                "FRAD006_11AV" => "(?<callnum>.+) +- +(?<title>.+) +- +(?<from>.+?)(-(?<to>.+))?$", // Audiovisuel
                "FRAD006_10FI" => "(?<callnum>.+) +- +(?<title>.+) +- +\\((?<from>.+?)(?<to>.+)\\)", // Iconographie
                _ => null
            };
            // ReSharper restore StringLiteralTypo

            if (labelRegexExp != null)
            {
                var data = Regex.Match(sequence.Label, labelRegexExp).Groups;
                registry.CallNumber ??= data["callnum"].Value;
                registry.Location ??= data["city"].Value;
                registry.From ??= data["from"].Value;
                registry.To ??= data["to"].Value;
                if (data.ContainsKey("title")) notes.Insert(0, data["title"].Value);
                if (data.ContainsKey("type")) registry.Types = registry.Types.Concat(GetTypes(data["type"].Value));
            }
            registry.Notes = string.Join("\n", notes);


            Data.AddOrUpdate(Data.Providers["AD06"].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = 1 };
        }

        private static IEnumerable<RegistryType> GetTypes(string typeActe) // TODO
        {
            foreach (var t in Regex.Split(typeActe, "(?=[A-Z])"))
            {
                var type = t.Trim(' ');

                if (type == "Naissances") yield return RegistryType.Birth;
                else if (type == "Tables décennales des naissances") yield return RegistryType.BirthTable;
                else if (type == "Baptêmes") yield return RegistryType.Baptism;
                else if (type == "Tables des baptêmes") yield return RegistryType.BaptismTable;

                else if (type is "Publications" or "Publications de mariages") yield return RegistryType.Banns;
                else if (type == "Mariages") yield return RegistryType.Marriage;
                else if (type is "Tables des mariages" or "Tables décennales des mariages") yield return RegistryType.MarriageTable;

                else if (type == "Décès") yield return RegistryType.Death;
                else if (type == "Tables décennales des décès") yield return RegistryType.DeathTable;
                else if (type == "Sépultures") yield return RegistryType.Burial;
                else if (type == "Tables des sépultures") yield return RegistryType.BurialTable;

                else if (type == "matrice cadastrale") yield return RegistryType.CadastralMatrix;
                else if (type == "état de section") yield return RegistryType.CadastralSectionStates;
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
            var (success, stream) = Data.TryGetImageFromDrive(registry, page, zoom);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();
            var image = await Image
                .LoadAsync(await client.GetStreamAsync(zoom >= 100 ? Iiif.GenerateImageRequestUri(page.URL) : Iiif.GenerateImageRequestUri(page.URL, size: $"{page.Width * zoom / 100},"))
                    .ConfigureAwait(false))
                .ConfigureAwait(false);
            page.Zoom = zoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD06"].Registries[registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(registry, page, image, false);
            return image.ToStream();
        }
    }
}
