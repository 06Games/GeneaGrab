using GeneaGrab.Providers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GeneaGrab
{
    public class RegistryInfo
    {
        public RegistryInfo() { }
        public RegistryInfo(Registry r) { ProviderID = r.ProviderID; LocationID = r.LocationID; RegistryID = r.ID; }

        public string ProviderID;
        public Provider Provider => Data.Providers.TryGetValue(ProviderID, out var p) ? p : null;
        public string LocationID;
        public Location Location => Provider.Locations.TryGetValue(LocationID ?? "", out var l) ? l : null;
        public string RegistryID;
        public Registry Registry => Provider.Registries.TryGetValue(RegistryID, out var r) ? r : null;
        public int PageNumber = 1;
        public RPage Page => Registry.Pages.ElementAtOrDefault(PageNumber - 1) ?? Registry.Pages.FirstOrDefault();
    }

    public static class Data
    {
        public static Func<string, string, string> Translate { get; set; } = (id, fallback) => fallback;
        public static Func<Registry, RPage, Task<SixLabors.ImageSharp.Image>> GetImage { get; set; } = (r, p) => Task.CompletedTask as Task<SixLabors.ImageSharp.Image>;
        public static Func<Registry, RPage, Task<string>> SaveImage { get; set; } = (r, p) => Task.CompletedTask as Task<string>;

        private static ReadOnlyDictionary<string, Provider> _providers;
        public static ReadOnlyDictionary<string, Provider> Providers
        {
            get
            {
                if (_providers != null) return _providers;

                var providers = new List<Provider>();
                providers.Add(new Provider(new Geneanet(), "Geneanet") { URL = "https://www.geneanet.org/" });
                providers.Add(new Provider(new AD06(), "AD06") { URL = "https://www.departement06.fr/archives-departementales/outils-de-recherche-et-archives-numerisees-2895.html" });
                //TODO: Add the others
                return _providers = new ReadOnlyDictionary<string, Provider>(providers.ToDictionary(k => k.ID, v => v));
            }
        }



        public static void AddOrUpdate<T>(Dictionary<string, T> dic, string key, T obj)
        {
            if (dic.ContainsKey(key)) dic[key] = obj;
            else dic.Add(key, obj);
        }
        public static DateTime? ParseDate(string date)
        {
            var culture = new System.Globalization.CultureInfo("fr-FR");
            var style = System.Globalization.DateTimeStyles.AssumeLocal;

            if (DateTime.TryParse(date, culture, style, out var d)) return d;
            else if (DateTime.TryParseExact(date, "yyyy", culture, style, out d)) return d;
            return null;
        }
        public static async Task<bool> TryGetImageFromDrive(Registry registry, RPage current, double zoom)
        {
            if (zoom > current.Zoom) return false;
            if (current.Image != null) return true;

            current.Image = await GetImage(registry, current).ConfigureAwait(false);
            if (current.Image != null) return true;
            else return false;
        }
    }
}