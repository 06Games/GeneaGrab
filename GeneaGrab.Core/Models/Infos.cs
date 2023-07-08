using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Providers;
using SixLabors.ImageSharp;

namespace GeneaGrab.Core.Models
{
    public class RegistryInfo : IEquatable<RegistryInfo>
    {
        public RegistryInfo() { PageNumber = Registry?.Pages?.FirstOrDefault()?.Number ?? 1; }
        public RegistryInfo(Registry r)
        {
            ProviderID = r.ProviderID;
            RegistryID = r.ID;
            PageNumber = Registry?.Pages?.FirstOrDefault()?.Number ?? 1;
        }

        public string ProviderID;
        public Provider Provider => ProviderID is null ? null : (Data.Providers.TryGetValue(ProviderID, out var p) ? p : null);
        public string RegistryID;
        public Registry Registry => RegistryID is null ? null : (Provider.Registries.TryGetValue(RegistryID, out var r) ? r : null);
        public int PageNumber;
        public int PageIndex => Array.IndexOf(Registry.Pages, Page);
        public RPage Page => GetPage(PageNumber);
        public RPage GetPage(int number) => Registry.Pages.FirstOrDefault(page => page.Number == number);


        public bool Equals(RegistryInfo other) => ProviderID == other?.ProviderID && RegistryID == other?.RegistryID;
        public override bool Equals(object obj) => Equals(obj as RegistryInfo);
        public static bool operator ==(RegistryInfo one, RegistryInfo two) => one?.ProviderID == two?.ProviderID && one?.RegistryID == two?.RegistryID;
        public static bool operator !=(RegistryInfo one, RegistryInfo two) => !(one == two);
        public override int GetHashCode() => (ProviderID + RegistryID).GetHashCode();
    }

    public static class Data
    {
        public static Func<string, string, string> Translate { get; set; } = (_, fallback) => fallback;
        public static Func<Registry, RPage, bool, Stream> GetImage { get; set; } = (_, _, _) => null;
        public static Func<Registry, RPage, Image, bool, Task<string>> SaveImage { get; set; } = (_, _, _, _) => Task.CompletedTask as Task<string>;
        public static Func<Image, Task<Image>> ToThumbnail { get; set; } = Task.FromResult;
        public static Action<string, Exception> Log { get; set; } = (l, d) => Debug.WriteLine($"{l}: {d}");
        public static Action<string, Exception> Warn { get; set; } = (l, d) => Debug.WriteLine($"{l}: {d}");
        public static Action<string, Exception> Error { get; set; } = (l, d) => Debug.WriteLine($"{l}: {d}");

        private static ReadOnlyDictionary<string, Provider> _providers;
        public static ReadOnlyDictionary<string, Provider> Providers
        {
            get
            {
                if (_providers != null) return _providers;

                var providers = new List<Provider>
                {
                    // France
                    new(new Geneanet(), "Geneanet") { Url = "https://www.geneanet.org/" },
                    new(new AD06(), "AD06") { Url = "https://archives06.fr/" },
                    new(new NiceHistorique(), "NiceHistorique") { Url = "https://www.nicehistorique.org/" },
                    new(new AD17(), "AD17") { Url = "https://www.archinoe.net/v2/ad17/registre.html" },
                    new(new AD79_86(), "AD79-86") { Url = "https://archives-deux-sevres-vienne.fr/" },
                    //TODO: Gironde and Cantal

                    // Italy
                    new(new Antenati(), "Antenati") { Url = "https://www.antenati.san.beniculturali.it/" },
                };
                return _providers = new ReadOnlyDictionary<string, Provider>(providers.ToDictionary(k => k.Id, v => v));
            }
        }



        public static void AddOrUpdate<T>(Dictionary<string, T> dic, string key, T obj)
        {
            if (dic.ContainsKey(key)) dic[key] = obj;
            else dic.Add(key, obj);
        }
        public static async Task<(bool success, Stream stream)> TryGetThumbnailFromDrive(Registry registry, RPage current)
        {
            var image = GetImage(registry, current, true);
            if (image != null) return (true, image);

            var (success, stream) = TryGetImageFromDrive(registry, current, 0);
            if (!success) return (false, null);

            var thumb = await ToThumbnail(await Image.LoadAsync(stream).ConfigureAwait(false)).ConfigureAwait(false);
            await SaveImage(registry, current, thumb, true);
            return (true, thumb.ToStream());
        }
        public static (bool success, Stream stream) TryGetImageFromDrive(Registry registry, RPage current, double zoom)
        {
            if (zoom > current.Zoom) return (false, null);

            var image = GetImage(registry, current, false);
            return image != null ? (true, image) : (false, null);
        }
    }
}
