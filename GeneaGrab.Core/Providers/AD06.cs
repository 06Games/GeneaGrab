using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;
using Serilog;
using SixLabors.ImageSharp;

namespace GeneaGrab.Core.Providers
{
    public class AD06 : Provider
    {
        public override string Id => "AD06";
        public override string Url => "https://archives06.fr/";
        public override bool IndexSupport => false;

        public override async Task<RegistryInfo> GetRegistryFromUrlAsync(Uri url)
        {
            if (url.Host != "archives06.fr" || !url.AbsolutePath.StartsWith("/ark:/")) return null;

            var queries = Regex.Match(url.AbsolutePath, @"/ark:/(?<something>[\w\.]+)(/(?<id>[\w\.]+))?(/(?<tag>[\w\.]+))?(/(?<seq>\d+))?(/(?<page>\d+))?").Groups;
            return new RegistryInfo
            {
                ProviderID = "AD06",
                RegistryID = queries["id"].Value,
                PageNumber = int.TryParse(queries["page"].Value, out var page) ? page : 1
            };
        }

        public override async Task<RegistryInfo> Infos(Uri url)
        {
            var queries = Regex.Match(url.AbsolutePath, @"/ark:/(?<something>[\w\.]+)(/(?<id>[\w\.]+))?(/(?<tag>[\w\.]+))?(/(?<seq>\d+))?(/(?<page>\d+))?").Groups;
            var registry = new Registry(Data.Providers["AD06"]) { ID = queries["id"].Value };
            registry.URL = $"https://archives06.fr/ark:/{queries["something"].Value}/{registry.ID}";

            var client = new HttpClient();
            var manifest = new LigeoManifest(await client.GetStringAsync($"{registry.URL}/manifest"));
            if (!int.TryParse(queries["seq"].Value, out var seq)) Log.Warning("Couldn't parse sequence ({SequenceValue}), using default one", queries["seq"].Value);
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
            registry.CallNumber = string.IsNullOrWhiteSpace(classeur.UnitId) ? null : classeur.UnitId;
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
                    case "Lieu d'édition":
                        registry.Location = ToTitleCase(metadata.Value.ToLower());
                        break;
                    case "Paroisse":
                    case "Complément de lieu":
                        registry.District = ToTitleCase(metadata.Value.ToLower());
                        break;
                    case "Date":
                    case "Date de l'acte":
                    case "Année (s)":
                    {
                        var dates = metadata.Value.Split('-');
                        registry.From = dates.FirstOrDefault()?.Trim();
                        registry.To = dates.LastOrDefault()?.Trim();
                        break;
                    }
                    case "Typologie":
                    case "Type de document":
                    case "Type d'acte":
                        registry.Types = registry.Types.Union(GetTypes(metadata.Value));
                        break;
                    case "Analyse":
                        registry.Title = metadata.Value;
                        break;
                    case "Folio":
                    case "Volume":
                        registry.Subtitle = metadata.Value;
                        break;
                    case "Auteur":
                    case "Photographe":
                    case "Sigillant":
                    case "Bureau":
                    case "Présentation du producteur":
                        registry.Author = metadata.Value;
                        break;
                    default:
                        notes.Add($"{metadata.Key}: {metadata.Value}");
                        break;
                }
            }

            var (labelRegexExp, type) = classeur.EncodedArchivalDescriptionId.ToUpperInvariant() switch
            {
                "FRAD006_ETAT_CIVIL" => (@"(?<callnum>.+) +- +(?<type>.*?) *?- *?\((?<from>.+?)( à (?<to>.+))?\)", null),
                "FRAD006_CADASTRE_PLAN" => ("(?<callnum>.+) +- +(?<district>.*?) +- +(?<subtitle>.*?) +- +(?<from>.+?)", new[] { RegistryType.CadastralMap }),
                "FRAD006_CADASTRE_MATRICE" => ("(?<callnum>.+?) +- +(?<title>.*?) *?-", new[] { RegistryType.CadastralMatrix }),
                "FRAD006_CADASTRE_ETAT_SECTION" => ("(?<callnum>.+) +- +(?<title>.*?) *?-", new[] { RegistryType.CadastralSectionStates }),
                "FRAD006_RECENSEMENT_POPULATION" => ("(?<city>.+) +- +(?<from>.+)(, (?<district>.*))", new[] { RegistryType.Census }),
                "FRAD006_HYPOTHEQUES" => (@"(?<callnum>.+) +- +(?<title>.+) +-", new[] { RegistryType.Catalogue }),
                "FRAD006_HYPOTHEQUES_ACTES_TRANSLATIFS" => (@"(?<callnum>.+?) ?- +(?<author>.+?) ?\.?- +(?<title>.+?) ?- +(?<from>.+?)(-(?<to>.+))?$", new[] { RegistryType.Engrossments }),
                "FRAD006_REPERTOIRE_NOTAIRES" => ("(?<callnum>.+) +- +(?<title>.+)", new[] { RegistryType.Notarial }),
                "FRAD006_3E" => (@"(?<callnum>3 E [\d ]+) (?<title>.*)\. (?<from>.*)-(?<to>.*)", new[] { RegistryType.Notarial }), // Notaire
                "FRAD006_C" => (@"(?<callnum>C [\d ]+) (?<title>.*)\. (?<from>.*)-(?<to>.*)", new[] { RegistryType.Other }), // Archives anciennes
                "FRAD006_ARMOIRIES" => ("(?<callnum>.+) +- +(?<title>.+)", new[] { RegistryType.Other }),
                "FRAD006_OUVRAGES" => ("(?<callnum>.+) +- +(?<title>.+)", new[] { RegistryType.Book }),
                "FRAD006_BN_SOURCES_IMPRIMES" => ("(?<title>.+)", new[] { RegistryType.Book }),
                "FRAD006_ANNUAIRES" => ("(?<title>.+)", new[] { RegistryType.Other }),
                "FRAD006_PRESSE" => (@"(?<title>.+) \(\d*-\d*\), .*? +- +(?<from>(\d|\/)+)(-(?<to>(\d|\/)+))?", new[] { RegistryType.Newspaper }),
                "FRAD006_DELIBERATIONS_CONSEIL_GENERAL" => ("(?<callnum>.+) +- +(?<title>.+) +- +(?<from>.+?)(-(?<to>.+))?$", new[] { RegistryType.Book }),
                "FRAD006_11AV" => ("(?<callnum>.+) +- +(?<title>.+) +- +(?<from>.+?)(-(?<to>.+))?$", new[] { RegistryType.Other }), // Audiovisuel
                "FRAD006_10FI" => ("(?<callnum>.+) +- +(?<title>.+) +- +\\((?<from>.+?)-(?<to>.+)\\)", new[] { RegistryType.Other }), // Iconographie
                _ => (null, null)
            };
            // ReSharper restore StringLiteralTypo

            if (labelRegexExp != null)
            {
                var data = Regex.Match(sequence.Label, labelRegexExp).Groups;
                registry.CallNumber ??= data.TryGetValue("callnum");
                registry.Location ??= ToTitleCase(data.TryGetValue("city"));
                registry.District ??= ToTitleCase(data.TryGetValue("district"));
                registry.From ??= data.TryGetValue("from");
                registry.To ??= data.TryGetValue(data["to"].Success ? "to" : "from");
                registry.Title ??= data.TryGetValue("title");
                registry.Subtitle ??= data.TryGetValue("subtitle");
                registry.Author ??= data.TryGetValue("author");
                if (data["type"].Success) registry.Types = registry.Types.Union(GetTypes(data.TryGetValue("type")));
                if (type?.Length > 0) registry.Types = registry.Types.Union(type);


                var analyse = await client.GetStringAsync(registry.URL);
                registry.LocationDetails ??= Regex
                    .Matches(analyse, @"<ul><li><a href=[^>]+?><span>(?<archivePath>[^>]+?)\.?</span></a>")
                    .Select(m => m.Groups.TryGetValue("archivePath"))
                    .ToArray();

                if (classeur.EncodedArchivalDescriptionId.ToUpperInvariant() == "FRAD006_ETAT_CIVIL") // The civil registry collection only provides the city through the analysis page
                    registry.Location ??= ToTitleCase(registry.LocationDetails.LastOrDefault());
                else if (classeur.EncodedArchivalDescriptionId.ToUpperInvariant() == "FRAD006_3E" && data.TryGetValue("title") == $"{data.TryGetValue("from")}-{data.TryGetValue("to")}")
                    registry.Title = registry.LocationDetails.LastOrDefault();
            }
            registry.Notes = string.Join("\n", notes);


            Data.AddOrUpdate(Data.Providers["AD06"].Registries, registry.ID, registry);
            return new RegistryInfo(registry) { PageNumber = int.TryParse(queries["page"].Value, out var page) ? page : 1 };
        }

        private static string ToTitleCase(string text) => text is null ? null : Regex.Replace(text, @"\p{L}+", match => match.Value[..1].ToUpper() + match.Value[1..].ToLower());

        private static IEnumerable<RegistryType> GetTypes(string typeActe)
        {
            foreach (var t in Regex.Split(typeActe, "(?=[A-Z])"))
            {
                var type = t.Trim(' ');

                if (type is "Naissances") yield return RegistryType.Birth;
                else if (type is "Tables décennales des naissances" or "Tables alphabétiques des naissances") yield return RegistryType.BirthTable;
                else if (type is "Baptêmes") yield return RegistryType.Baptism;
                else if (type is "Tables des baptêmes") yield return RegistryType.BaptismTable;

                else if (type is "Confirmations") yield return RegistryType.Confirmation;
                else if (type is "Tables des communions") yield return RegistryType.Communion;

                else if (type is "Publications" or "Publications de mariages") yield return RegistryType.Banns;
                else if (type is "Mariages") yield return RegistryType.Marriage;
                else if (type is "Tables des mariages" or "Tables décennales des mariages" or "Tables alphabétiques des mariages") yield return RegistryType.MarriageTable;
                else if (type is "Divorces") yield return RegistryType.Divorce;

                else if (type is "Décès") yield return RegistryType.Death;
                else if (type is "Tables décennales des décès" or "Tables alphabétiques des décès") yield return RegistryType.DeathTable;
                else if (type is "Sépultures" or "Sépultures des enfants décédés sans baptêmes") yield return RegistryType.Burial;
                else if (type is "Tables des sépultures") yield return RegistryType.BurialTable;

                else if (type is "Répertoire") yield return RegistryType.Catalogue;
                else if (type is "Inventaire") yield return RegistryType.Other;

                else if (type is "matrice cadastrale") yield return RegistryType.CadastralMatrix;
                else if (type is "état de section") yield return RegistryType.CadastralSectionStates;
            }
        }


        public override Task<string> Ark(Registry registry, RPage page) => Task.FromResult($"{registry.ArkURL}/{page.Number}");
        public override async Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(registry, page);
            if (success) return stream;
            return await GetTiles(registry, page, 0.1F, progress);
        }
        public override Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 0.5F, progress);
        public override Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress) => GetTiles(registry, page, 1, progress);
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
