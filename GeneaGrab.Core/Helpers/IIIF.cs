using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneaGrab.IIIF
{
    public static class IIIF
    {
        public static Uri GenerateImageRequestUri(string imageURL, string region = "full", string size = "max", string rotation = "0", string quality = "default", string format = "jpg")
            => new Uri($"{imageURL}/{region}/{size}/{rotation}/{quality}.{format}");
    }

    internal class Manifest
    {
        public Manifest(string manifest) => Parse(JObject.Parse(manifest));
        internal Manifest(JObject manifest) => Parse(manifest);
        private void Parse(JObject manifest)
        {
            Json = manifest;
            MetaData = manifest["metadata"].ToDictionary(m => m.Value<string>("label"), m => m.Value<string>("value"));
            Sequences = manifest["sequences"].Select(s => new Sequence(s));
        }

        public JToken Json { get; private set; }
        public Dictionary<string, string> MetaData { get; private set; }
        public IEnumerable<Sequence> Sequences { get; private set; }
    }

    internal class Sequence
    {
        internal Sequence(JToken sequence)
        {
            Json = sequence;
            Id = sequence.Value<string>("@id");
            Label = sequence.Value<string>("@label") ?? sequence.Value<string>("label");
            Canvases = sequence["canvases"].Select(s => new Canvas(s));
        }

        public JToken Json { get; }
        public string Id { get; }
        public string Label { get; }
        public IEnumerable<Canvas> Canvases { get; }
    }

    internal class Canvas
    {
        internal Canvas(JToken canvas)
        {
            Json = canvas;
            Id = canvas.Value<string>("@id");
            Label = canvas.Value<string>("label") ?? canvas.Value<string>("@label");
            Ark = canvas.Value<string>("ligeoPermalink");
            Thumbnail = canvas["thumbnail"].HasValues ? canvas["thumbnail"].Value<string>("@id") : canvas.Value<string>("thumbnail");
            Images = canvas["images"].Select(s => new Image(s));
        }

        public JToken Json { get; }
        public string Id { get; }
        public string Label { get; }
        public string Ark { get; }
        public string Thumbnail { get; }
        public IEnumerable<Image> Images { get; }
    }

    internal class Image
    {
        internal Image(JToken image)
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
