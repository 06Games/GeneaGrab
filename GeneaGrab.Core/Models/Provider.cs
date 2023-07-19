using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GeneaGrab.Core.Models
{
    public interface IndexAPI
    {
        Task<IEnumerable<Index>> GetIndex(Registry registry, RPage page);
        Task AddIndex(Registry registry, RPage page, Index index);
    }

    /// <summary>Registry provider</summary>
    public abstract class Provider : IEquatable<Provider>
    {
        public abstract string Id { get; }
        public abstract string Url { get; }
        public abstract bool IndexSupport { get; }
        public string Name => Data.Translate($"Provider.{Id}", Id);
        public string Icon => $"/Assets/Providers/{Id}.png";
        public override string ToString() => Name;

        
        public abstract bool TryGetRegistryId(Uri url, out RegistryInfo info);
        public abstract Task<RegistryInfo> Infos(Uri url);
        public abstract Task<Stream> Thumbnail(Registry registry, RPage page, Action<Progress> progress);
        public abstract Task<Stream> Preview(Registry registry, RPage page, Action<Progress> progress);
        public abstract  Task<Stream> Download(Registry registry, RPage page, Action<Progress> progress);
        public abstract Task<string> Ark(Registry registry, RPage page);
        

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
