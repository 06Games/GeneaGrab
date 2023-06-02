using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using GeneaGrab.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeneaGrab
{
    public class Index : IEquatable<Index>
    {
        public string Id { get; set; }
        public int Page { get; set; }
        public Rectangle Position { get; set; }

        public DateTime? Date { get; set; }
        public RegistryType Type { get; set; }
        public string District { get; set; }

        public string Notes { get; set; }
        public string Retranscription { get; set; }
        public List<Person> Persons { get; set; }


        public bool Equals(Index other) => Id == other?.Id;
        public override bool Equals(object obj) => Equals(obj as Index);
        public static bool operator ==(Index one, Index two) => one?.Id == two?.Id;
        public static bool operator !=(Index one, Index two) => !(one == two);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public class Person : IEquatable<Person>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Surname { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Nickname { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Profession { get; set; }
        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))] public RelationType Relation { get; set; }
        public enum RelationType
        {
            Main,
            Husband,
            Wife,
            Father,
            Mother,
            Godfather,
            Godmother,
            Declaring,
            Witness,
            Priest,
            Other = -1 // Avoid as much as possible
        }


        public bool Equals(Person other) => Id == other?.Id;
        public override bool Equals(object obj) => Equals(obj as Person);
        public static bool operator ==(Person one, Person two) => one?.Id == two?.Id;
        public static bool operator !=(Person one, Person two) => !(one == two);
        public override int GetHashCode() => Id.GetHashCode();
    }


    public class GenericIndex : IndexAPI
    {
        public bool IndexSupport => true;

        public Task<IEnumerable<Index>> GetIndex(Registry Registry, RPage page) => GetIndexFromLocalData(Registry, page);
        public static async Task<IEnumerable<Index>> GetIndexFromLocalData(Registry Registry, RPage page)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return null;
        }

        public Task AddIndex(Registry Registry, RPage page, Index index) => AddIndexToLocalData(Registry, page, index);
        public static async Task AddIndexToLocalData(Registry Registry, RPage page, Index index)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
