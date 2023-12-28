#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using GeneaGrab.Core.Models.Dates;

namespace GeneaGrab.Core.Models
{
    /// <summary>Data on the registry</summary>
    public sealed class Registry : IEquatable<Registry>
    {
        public Registry(Provider provider) : this(provider.Id) { }
        public Registry(string providerId) => ProviderId = providerId;

        public string Id { get; private init; } = null!;
        public string ProviderId { get; private init; }

        public string? RemoteId { get; set; }

        /// <summary>Call number of the document (if applicable)</summary>
        public string? CallNumber { get; set; }
        public string? URL { get; set; }
        public string? ArkURL { get; set; }
        public IList<RegistryType> Types { get; set; } = Array.Empty<RegistryType>();

        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Author { get; set; }
        public string[] Location { get; set; } = Array.Empty<string>();
        public string? Notes { get; set; }


        /// <summary>Any additional information that might be needed</summary>
        [NotMapped] public object? Extra { get; set; }

        [NotMapped] public Date? From { get; set; } // TODO: Map this
        [NotMapped] public Date? To { get; set; }


        public override string ToString()
        {
            /*// Type
            var name = TypeToString ?? "";

            // Dates
            var dates = Dates;
            if (!string.IsNullOrEmpty(dates)) name += $" ({dates})";*/
            var name = "";

            // Title
            if (!string.IsNullOrEmpty(Title)) name += $"\n{Title}";
            else if (!string.IsNullOrEmpty(Notes)) name += $"\n{Notes.Split('\n').FirstOrDefault()}";

            if (!string.IsNullOrEmpty(Subtitle)) name += $" ({Subtitle})";
            if (!string.IsNullOrEmpty(Author)) name += $"\n{Author}";

            return name;
        }

        public IEnumerable<Frame> Frames { get; set; } = Array.Empty<Frame>();


        public bool Equals(Registry? other) => Id == other?.Id;
        public override bool Equals(object? obj) => Equals(obj as Registry);
        public static bool operator ==(Registry? one, Registry? two) => one?.Id == two?.Id;
        public static bool operator !=(Registry? one, Registry? two) => !(one == two);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
