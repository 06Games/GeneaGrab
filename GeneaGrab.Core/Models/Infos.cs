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
        public RegistryInfo() { PageNumber = 1; }
        public RegistryInfo(Registry r)
        {
            ProviderId = r.ProviderId;
            RegistryId = r.Id;
            PageNumber = 1;
        }

        public string ProviderId { get; set; }
        public Provider Provider => ProviderId is not null && Data.Providers.TryGetValue(ProviderId, out var p) ? p : null;
        public string RegistryId { get; set; }
        public int PageNumber { get; set; }


        public bool Equals(RegistryInfo other) => ProviderId == other?.ProviderId && RegistryId == other?.RegistryId;
        public override bool Equals(object obj) => Equals(obj as RegistryInfo);
        public static bool operator ==(RegistryInfo one, RegistryInfo two) => one?.ProviderId == two?.ProviderId && one?.RegistryId == two?.RegistryId;
        public static bool operator !=(RegistryInfo one, RegistryInfo two) => !(one == two);
        public override int GetHashCode() => HashCode.Combine(ProviderId, RegistryId);
    }

    public static class Data
    {
        public static Func<string, string, string> Translate { get; set; } = (_, fallback) => fallback;
        public static Func<Frame, bool, Stream> GetImage { get; set; } = (_, _) => null;
        public static Func<Frame, Image, bool, Task<string>> SaveImage { get; set; } = (_, _, _) => Task.CompletedTask as Task<string>;
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
        public static async Task<(bool success, Stream stream)> TryGetThumbnailFromDrive(Frame current)
        {
            var image = GetImage(current, true);
            if (image != null) return (true, image);

            var (success, stream) = TryGetImageFromDrive(current, 0);
            if (!success) return (false, null);

            var thumb = await ToThumbnail(await Image.LoadAsync(stream).ConfigureAwait(false)).ConfigureAwait(false);
            await SaveImage(current, thumb, true);
            return (true, thumb.ToStream());
        }
        public static (bool success, Stream stream) TryGetImageFromDrive(Frame current, Scale zoom)
        {
            if (zoom > current.ImageSize) return (false, null);

            var image = GetImage(current, false);
            return image != null ? (true, image) : (false, null);
        }
    }
}
