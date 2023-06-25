using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GeneaGrab.Core.Helpers
{
    public static class Iiif
    {
        public static Uri GenerateImageRequestUri(string imageURL, string region = "full", string size = "max", string rotation = "0", string quality = "default", string format = "jpg")
            => new Uri($"{imageURL}/{region}/{size}/{rotation}/{quality}.{format}");
    }

    internal class IiifManifest
    {
        public IiifManifest(string manifest) : this(JObject.Parse(manifest)) { }
        private IiifManifest(JObject manifest)
        {
            Json = manifest;
            MetaData = manifest["metadata"].ToDictionary(m => m.Value<string>("label"), m => m.Value<string>("value"));
            Sequences = manifest["sequences"].Select(s => new IiifSequence(s));
        }

        public JToken Json { get; }
        public Dictionary<string, string> MetaData { get; }
        public IEnumerable<IiifSequence> Sequences { get; }
    }

    internal class IiifSequence
    {
        internal IiifSequence(JToken sequence)
        {
            Json = sequence;
            Id = sequence.Value<string>("@id");
            Label = sequence.Value<string>("@label") ?? sequence.Value<string>("label");
            Canvases = sequence["canvases"].Select(s => new IiifCanvas(s));
        }

        public JToken Json { get; }
        public string Id { get; }
        public string Label { get; }
        public IEnumerable<IiifCanvas> Canvases { get; }
    }

    internal class IiifCanvas
    {
        internal IiifCanvas(JToken canvas)
        {
            Json = canvas;
            Id = canvas.Value<string>("@id");
            Label = canvas.Value<string>("label") ?? canvas.Value<string>("@label");
            Ark = canvas.Value<string>("ligeoPermalink");
            Thumbnail = canvas["thumbnail"].HasValues ? canvas["thumbnail"].Value<string>("@id") : canvas.Value<string>("thumbnail");
            Images = canvas["images"].Select(s => new IiifImage(s));
        }

        public JToken Json { get; }
        public string Id { get; }
        public string Label { get; }
        public string Ark { get; }
        public string Thumbnail { get; }
        public IEnumerable<IiifImage> Images { get; }
    }

    internal class IiifImage
    {
        internal IiifImage(JToken image)
        {
            Json = image;
            Id = image.Value<string>("@id");
            Format = image["resource"].Value<string>("format");
            ServiceId = image["resource"]["service"]?.Value<string>("@id");
        }

        public JToken Json { get; }
        public string Id { get; }
        public string Format { get; }
        public string ServiceId { get; }
    }
}
