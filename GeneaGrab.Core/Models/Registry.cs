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
        public Registry(Location location)
        {
            LocationID = location.ID;
            ProviderID = location.ProviderID;
        }
        public string LocationID { get; set; }
        public string ProviderID { get; set; }

        public string ID { get; set; }
        public string URL { get; set; }
        public enum Type
        {
            /// <summary>Unable to determine</summary>
            Unknown = -2,
            /// <summary>Uncategorized</summary>
            Other = -1,

            /// <summary>Birth certificates</summary>
            Birth,
            /// <summary>Table of birth certificates</summary>
            BirthTable,
            /// <summary>Baptismal records</summary>
            Baptism,
            /// <summary>Banns of marriage</summary>
            Banns,
            /// <summary>Marriage certificates</summary>
            Marriage,
            /// <summary>Table of marriage certificates</summary>
            MarriageTable,
            /// <summary>Death certificates</summary>
            Death,
            /// <summary>Table of death certificates</summary>
            DeathTable,
            /// <summary>Burial records</summary>
            Burial,

            /// <summary>Census of the population</summary>
            Census,
            /// <summary>Notarial deeds</summary>
            Notarial,
            /// <summary>Military numbers</summary>
            Military
        }
        [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))] public List<Type> Types { get; set; } = new List<Type>();
        public string Notes { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")] public DateTime? From { get; set; }
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")] public DateTime? To { get; set; }
        [JsonIgnore]
        public string Name
        {
            get
            {
                var name = Types.Any() ? string.Join(", ", Types.Select(t => Enum.GetName(typeof(Type), t))) : ""; //Type

                //Dates
                const string Date = "dd/MM/yyyy";
                const string Month = "MM/yyyy";
                const string Year = "yyyy";
                if (From.HasValue && To.HasValue && From.Value.Date == To.Value.Date) name += $" ({From.Value.ToString(Date)})";
                else if (From.HasValue && To.HasValue)
                {
                    var from = From.Value;
                    var to = To.Value;
                    var format = Year;
                    if (from.Day != to.Day || from.Day != 1) format = Date;
                    else if (from.Month != to.Month || from.Month != 1) format = Month;
                    name += $" ({from.ToString(format)} - {to.ToString(format)})";
                }
                else if (From.HasValue) name += $" ({From.Value.ToString(Date)} - ?)";
                else if (To.HasValue) name += $" (? - {From.Value.ToString(Date)})";

                if (!string.IsNullOrEmpty(Notes)) name += $" - {Notes}"; //Notes

                return name;
            }
        }
        public override string ToString() => Name;

        public RPage[] Pages { get; set; }


        public bool Equals(Registry other) => ID == other?.ID;
        public override bool Equals(object obj) => Equals(obj as Registry);
        public static bool operator ==(Registry one, Registry two) => one?.ID == two?.ID;
        public static bool operator !=(Registry one, Registry two) => !(one == two);
        public override int GetHashCode() => ID.GetHashCode();
    }
    public class DateFormatConverter : Newtonsoft.Json.Converters.IsoDateTimeConverter { public DateFormatConverter(string format) => DateTimeFormat = format; }

    /// <summary>Data on the page of the registry</summary>
    public class RPage
    {
        public int Number { get; set; }
        public override string ToString() => Number.ToString();
        public string URL { get; set; }
        [JsonIgnore] public SixLabors.ImageSharp.Image Image { get; set; }

        //public int[] Zoom { get; set; } //Remove? Image layers instead?
        public SixLabors.ImageSharp.Point Tiles { get; set; }
        public Grabber.Args Args { get; set; }
    }
}
