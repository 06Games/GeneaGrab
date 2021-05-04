using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeneaGrab
{
    /// <summary>Interface to communicate with the registry provider</summary>
    public interface ProviderAPI
    {
        bool CheckURL(Uri URL);
        Task<RegistryInfo> Infos(Uri URL);
        Task<RPage> GetTile(Registry Registry, RPage page, int zoom);
        Task<RPage> Download(Registry Registry, RPage page);
    }
    /// <summary>Data on the registry provider</summary>
    public class Provider : IEquatable<Provider>
    {
        public Provider(ProviderAPI api) => API = api;
        public Provider(ProviderAPI api, string id)
        {
            ID = id;
            Name = Data.Translate($"Provider/{ID}", ID);
            Icon = $"/Assets/Providers/{ID}.png";
            API = api;
        }

        public string ID { get; set; }
        public string URL { get; set; }
        public string Name { get; set; }
        public ProviderAPI API { get; set; }
        public override string ToString() => Name;

        public string Icon { get; set; }

        public Dictionary<string, Location> Locations { get; } = new Dictionary<string, Location>();
        public Dictionary<string, Registry> Registries { get; } = new Dictionary<string, Registry>();
        public string RegisterCount
        {
            get
            {
                var count = Registries.Count;
                if (count == 0) return "Aucun registre";
                else if (count == 1) return "1 registre";
                else return $"{count} registres";
            }
        }


        public bool Equals(Provider other) => ID == other.ID;
        public override bool Equals(object obj) => Equals(obj as Provider);
        public static bool operator ==(Provider one, Provider two) => one?.ID == two?.ID;
        public static bool operator !=(Provider one, Provider two) => !(one == two);
        public override int GetHashCode() => ID.GetHashCode();
    }
}
