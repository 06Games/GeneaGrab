using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GeneaGrab.Core.Models
{
    /// <summary>Interface to communicate with the registry provider</summary>
    public interface ProviderAPI
    {
        bool TryGetRegistryID(Uri url, out RegistryInfo info);
        Task<RegistryInfo> Infos(Uri url);
        Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress);
        Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress);
        Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress);
        Task<string> Ark(Registry registry, RPage page);

        bool IndexSupport { get; }
    }
    public interface IndexAPI
    {
        Task<IEnumerable<Index>> GetIndex(Registry registry, RPage page);
        Task AddIndex(Registry registry, RPage page, Index index);
    }

    /// <summary>Data on the registry provider</summary>
    public class Provider : IEquatable<Provider>
    {
        public Provider(ProviderAPI api, string id)
        {
            Id = id;
            Name = Data.Translate($"Provider.{Id}", Id);
            Icon = $"/Assets/Providers/{Id}.png";
            Api = api;
        }
        

        public string Id { get; }
        public string Url { get; set; }
        public string Name { get; set; }
        public ProviderAPI Api { get; }
        public override string ToString() => Name;

        public string Icon { get; set; }

        public Dictionary<string, Registry> Registries { get; } = new();
        public string RegisterCount
        {
            get
            {
                var count = Registries.Count;
                if (count == 0) return "Aucun registre";
                if (count == 1) return "1 registre";
                return $"{count} registres";
            }
        }


        public bool Equals(Provider other) => Id == other?.Id;
        public override bool Equals(object obj) => Equals(obj as Provider);
        public static bool operator ==(Provider one, Provider two) => one?.Id == two?.Id;
        public static bool operator !=(Provider one, Provider two) => !(one == two);
        public override int GetHashCode() => Id.GetHashCode();
    }


    public class Progress
    {
        public static readonly Progress Finished = new() { Value = 1, Done = true };
        public static readonly Progress Unknown = new() { Undetermined = true };
        private Progress() { }

        public static implicit operator Progress(int v) => new(v);
        public static implicit operator Progress(float v) => new(v);
        public static implicit operator Progress(decimal v) => new((float)v);
        public Progress(float value) => Value = value;

        public static implicit operator float(Progress p) => p.Value;
        public float Value { get; private set; }
        public bool Done { get; private set; }
        public bool Undetermined { get; private set; }
    }
}
