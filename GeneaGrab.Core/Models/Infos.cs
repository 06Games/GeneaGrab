using GeneaGrab.Providers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GeneaGrab
{
    public class RegistryInfo
    {
        public RegistryInfo() { }
        public RegistryInfo(Registry r) { ProviderID = r.ProviderID; LocationID = r.LocationID; RegistryID = r.ID; }

        public string ProviderID;
        public Provider Provider => Data.Providers[ProviderID];
        public string LocationID;
        public Location Location => Data.Locations[LocationID];
        public string RegistryID;
        public Registry Registry => Data.Registries[RegistryID];
        public int PageNumber;
        public RPage Page => Registry.Pages.ElementAtOrDefault(PageNumber - 1) ?? Registry.Pages.FirstOrDefault();
    }

    public static class Data
    {
        public static System.Func<string, string, string> Translate { get; set; } = (id, fallback) => fallback;
        public static System.Func<Registry, RPage, System.Threading.Tasks.Task<SixLabors.ImageSharp.Image>> GetImage { get; set; } = (r, p) => null;

        private static ReadOnlyDictionary<string, Provider> _providers;
        public static ReadOnlyDictionary<string, Provider> Providers
        {
            get
            {
                if (_providers != null) return _providers;

                var providers = new List<Provider>();
                providers.Add(new Provider(new Geneanet(), "Geneanet") { URL = "https://www.geneanet.org/" });
                //TODO: Add the others
                //dic.Add(new Provider(null, "AD06") { URL = "https://www.departement06.fr/archives-departementales/outils-de-recherche-et-archives-numerisees-2895.html" });
                return _providers = new ReadOnlyDictionary<string, Provider>(providers.ToDictionary(k => k.ID, v => v));
            }
        }
        public static Dictionary<string, Location> Locations { get; } = new Dictionary<string, Location>();
        public static Dictionary<string, Registry> Registries { get; } = new Dictionary<string, Registry>();
    }
}