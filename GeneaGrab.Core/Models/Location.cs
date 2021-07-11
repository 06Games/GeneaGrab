using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneaGrab
{
    /// <summary>Data on the location</summary>
    public class Location : IEquatable<Location>
    {
        public Location() { }
        public Location(Provider provider) => ProviderID = provider.ID;
        public string ProviderID { get; set; }

        public string ID { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string URL { get; set; }
        public string Name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore), System.ComponentModel.DefaultValue("")] public string District { get; set; }
        public override string ToString() => string.IsNullOrEmpty(District) ? Name : $"{Name} ({District})";

        [JsonIgnore] public Provider Provider => Data.Providers[ProviderID];
        [JsonIgnore] public IEnumerable<Registry> Registers => Provider.Registries.Values.Where(r => r.LocationID == ID);


        public bool Equals(Location other) => this == other;
        public override bool Equals(object obj) => Equals(obj as Location);
        public static bool operator ==(Location one, Location two) => (one is null || two is null) ? false : one.ID == two.ID;
        public static bool operator !=(Location one, Location two) => !(one == two);
        public override int GetHashCode() => ID.GetHashCode();
    }
}
