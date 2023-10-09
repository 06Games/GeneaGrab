using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GeneaGrab.Core.Helpers
{
    public class Iiif : IiifManifest<IiifSequence<IiifCanvas<IiifImage>>>
    {
        public Iiif(string manifest) : base(manifest) { }
        public static Uri GenerateImageRequestUri(string imageURL, string region = "full", string size = "max", string rotation = "0", string quality = "default", string format = "jpg")
            => new($"{imageURL}/{region}/{size}/{rotation}/{quality}.{format}");
    }

    public class IiifManifest<TSequence>
    {
        public IiifManifest(string manifest) : this(JObject.Parse(manifest)) { }
        internal IiifManifest(JToken manifest)
        {
            MetaData = manifest["metadata"].ToDictionary(m => m.Value<string>("label"), m => m.Value<string>("value"));
            Sequences = manifest["sequences"].Select(s => (TSequence)Activator.CreateInstance(typeof(TSequence), s));
        }

        public Dictionary<string, string> MetaData { get; }
        public IEnumerable<TSequence> Sequences { get; }
    }

    public class IiifSequence<TCanvas>
    {
        public IiifSequence(JToken sequence)
        {
            Id = sequence.Value<string>("@id");
            Label = sequence.Value<string>("@label") ?? sequence.Value<string>("label");
            Canvases = sequence["canvases"].Select(s => (TCanvas)Activator.CreateInstance(typeof(TCanvas), s));
        }

        public string Id { get; }
        public string Label { get; }
        public IEnumerable<TCanvas> Canvases { get; }
    }

    public class IiifCanvas<TImage>
    {
        public IiifCanvas(JToken canvas)
        {
            Id = canvas.Value<string>("@id");
            Label = canvas.Value<string>("label") ?? canvas.Value<string>("@label");
            Thumbnail = canvas["thumbnail"].HasValues ? canvas["thumbnail"].Value<string>("@id") : canvas.Value<string>("thumbnail");
            Images = canvas["images"].Select(s => (TImage)Activator.CreateInstance(typeof(TImage), s));
        }

        public string Id { get; }
        public string Label { get; }
        public string Thumbnail { get; }
        public IEnumerable<TImage> Images { get; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class IiifImage
    {
        public IiifImage(JToken image)
        {
            Id = image.Value<string>("@id");
            Format = image["resource"].Value<string>("format");
            Width = image["resource"].Value<int?>("width") ?? -1;
            Height = image["resource"].Value<int?>("height") ?? -1;
            ServiceId = image["resource"]["service"]?.Value<string>("@id");
        }

        public string Id { get; }
        public string Format { get; }
        public int Width { get; }
        public int Height { get; }
        public string ServiceId { get; }
    }
}
