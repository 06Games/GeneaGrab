using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Providers;
using Serilog;
using SixLabors.ImageSharp;

namespace GeneaGrab.Core.Models
{
    public sealed class RegistryInfo : IEquatable<RegistryInfo>
    {
        public RegistryInfo() { PageNumber = Registry?.Pages?.FirstOrDefault()?.Number ?? 1; }
        public RegistryInfo(Registry r)
        {
            ProviderId = r.ProviderID;
            RegistryId = r.ID;
            PageNumber = Registry?.Pages?.FirstOrDefault()?.Number ?? 1;
        }

        public string ProviderId { get; set; }
        public Provider Provider => ProviderId is not null && Data.Providers.TryGetValue(ProviderId, out var p) ? p : null;
        public string RegistryId { get; set; }
        public Registry Registry => RegistryId is not null && Provider.Registries.TryGetValue(RegistryId, out var r) ? r : null;
        public int PageNumber { get; set; }
        public int PageIndex => Array.IndexOf(Registry.Pages, Page);
        public RPage Page => GetPage(PageNumber);
        public RPage GetPage(int number) => Array.Find(Registry.Pages, page => page.Number == number);


        public bool Equals(RegistryInfo other) => ProviderId == other?.ProviderId && RegistryId == other?.RegistryId;
        public override bool Equals(object obj) => Equals(obj as RegistryInfo);
        public static bool operator ==(RegistryInfo one, RegistryInfo two) => one?.ProviderId == two?.ProviderId && one?.RegistryId == two?.RegistryId;
        public static bool operator !=(RegistryInfo one, RegistryInfo two) => !(one == two);
        public override int GetHashCode() => HashCode.Combine(ProviderId, RegistryId);
    }

    public static class Data
    {
        public static Func<string, string, string> Translate { get; set; } = (_, fallback) => fallback;
        public static Func<Registry, RPage, bool, Stream> GetImage { get; set; } = (_, _, _) => null;
        public static Func<Registry, RPage, Image, bool, Task<string>> SaveImage { get; set; } = (_, _, _, _) => Task.CompletedTask as Task<string>;
        public static Func<Image, Task<Image>> ToThumbnail { get; set; } = Task.FromResult;
        public static void SetLogger(ILogger value) => Log.Logger = value;

        private static ReadOnlyDictionary<string, Provider> _providers;
        public static ReadOnlyDictionary<string, Provider> Providers
        {
            get
            {
                if (_providers != null) return _providers;

                var providers = new List<Provider>
                {
                    // France
                    new Geneanet(),
                    new AD06(),
                    new Nice(),
                    new NiceHistorique(),
                    new AD17(),
                    new AD79_86(),

                    // Italy
                    new Antenati(),
                };
                return _providers = new ReadOnlyDictionary<string, Provider>(providers.ToDictionary(k => k.Id, v => v));
            }
        }



        public static void AddOrUpdate<T>(Dictionary<string, T> dic, string key, T obj)
        {
            if (dic.ContainsKey(key))
            {
                Log.Warning("Overwriting {Obj}", obj);
                dic[key] = obj;
            }
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
