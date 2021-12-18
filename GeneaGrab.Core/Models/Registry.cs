using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneaGrab
{
    /// <summary>Data on the registry</summary>
    public class Registry : IEquatable<Registry>
    {
        public Registry() { }
        public Registry(Provider provider) => ProviderID = provider.ID;
        public string ProviderID { get; set; }
        [JsonIgnore] public Provider Provider => Data.Providers[ProviderID];
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Location { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string LocationID { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string District { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string DistrictID { get; set; }

        public string ID { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string CallNumber { get; set; }
        public string URL { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string ArkURL { get; set; }
        [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))] public IEnumerable<RegistryType> Types { get; set; } = Array.Empty<RegistryType>();
        [JsonIgnore]
        public string TypeToString => Types.Any() ? string.Join(", ", Types.Select(t =>
        {
            var type = Enum.GetName(typeof(RegistryType), t);
            return Data.Translate($"Registry/Type/{type}", type);
        })) : null;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Notes { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")] public DateTime? From { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")] public DateTime? To { get; set; }
        [JsonIgnore]
        public string Dates
        {
            get
            {
                const string Date = "dd/MM/yyyy";
                const string Month = "MM/yyyy";
                const string Year = "yyyy";
                if (From.HasValue && To.HasValue && From.Value.Date == To.Value.Date) return From.Value.ToString(Date);
                else if (From.HasValue && To.HasValue)
                {
                    var from = From.Value;
                    var to = To.Value;
                    var format = Year;
                    if (from.Day != to.Day || from.Day != 1) format = Date;
                    else if (from.Month != to.Month || from.Month != 1) format = Month;
                    return $"{from.ToString(format)} - {to.ToString(format)}";
                }
                else if (From.HasValue) return $"{From.Value.ToString(Date)} - ?";
                else if (To.HasValue) return $"? - {From.Value.ToString(Date)}";
                return null;
            }
        }


        [JsonIgnore]
        public string Name
        {
            get
            {
                //Type
                var name = TypeToString ?? "";

                //Dates
                var dates = Dates;
                if (!string.IsNullOrEmpty(dates)) name += $" ({dates})";

                //Notes
                if (!string.IsNullOrEmpty(Notes)) name += $" - {Notes.Split('\n').FirstOrDefault()}";

                return name;
            }
        }
        public override string ToString() => Name;

        [JsonConverter(typeof(PagesConverter))] public RPage[] Pages { get; set; }


        public bool Equals(Registry other) => ID == other?.ID;
        public override bool Equals(object obj) => Equals(obj as Registry);
        public static bool operator ==(Registry one, Registry two) => one?.ID == two?.ID;
        public static bool operator !=(Registry one, Registry two) => !(one == two);
        public override int GetHashCode() => ID.GetHashCode();
    }
    public class DateFormatConverter : Newtonsoft.Json.Converters.IsoDateTimeConverter { public DateFormatConverter(string format) => DateTimeFormat = format; }
    public class PagesConverter : JsonConverter<RPage[]>
    {
        public override RPage[] ReadJson(JsonReader reader, Type objectType, RPage[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<Dictionary<int, RPage>>(reader).Select(kv =>
            {
                kv.Value.Number = kv.Key;
                return kv.Value;
            }).ToArray();
        }
        public override void WriteJson(JsonWriter writer, RPage[] value, JsonSerializer serializer) => serializer.Serialize(writer, value.ToDictionary(k => k.Number, v => v));
    }

    /// <summary>Data on the page of the registry</summary>
    public class RPage
    {
        [JsonIgnore] public int Number { get; set; }
        public override string ToString() => Number.ToString();
        public string URL { get; set; }
        [JsonIgnore] public SixLabors.ImageSharp.Image Image { get; set; }

        public int Zoom { get; set; } = -1;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), System.ComponentModel.DefaultValue(-1)] public int MaxZoom { get; set; } = -1;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int Width { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int Height { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public int? TileSize { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Notes { get; set; }
    }
}
