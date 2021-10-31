using SixLabors.ImageSharp;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeneaGrab
{
    public static class DeepZoom
    {
        /// <summary>Returns the maximum zoom level</summary>
        public static int CalculateIndex(RPage page) => (int)Math.Ceiling(Math.Log(Math.Max(page.Width, page.Height), 2));

        /// <summary>Returns the properties of the image</summary>
        /// <param name="baseURL">Image root url</param>
        /// <param name="client">HTTP client to use</param>
        public static async Task<(int w, int h, int tileSize, string format)> ImageData(string baseURL, HttpClient client)
        {
            string data = await client.GetStringAsync($"{baseURL}/image.xml");
            var dataResp = new FileFormat.XML.XML(data);
            var layer = dataResp.RootElement.node;
            var size = layer.FirstChild;

            return (
                int.TryParse(size.Attributes["Width"].Value, out var w) ? w : 0,
                int.TryParse(size.Attributes["Height"].Value, out var h) ? h : 0,
                int.TryParse(layer.Attributes["TileSize"]?.Value ?? "256", out var tileSize) ? tileSize : 256,
                layer.Attributes["Format"]?.Value ?? "jpg"
            );
        }

        public static (Point tiles, int diviser) NbTiles(RPage page, double multiplier)
        {
            var diviser = Math.Pow(2, CalculateIndex(page) - multiplier);
            int NbTiles(int val) => (int)Math.Ceiling(val / diviser / page.TileSize.GetValueOrDefault(256));
            return (new Point(NbTiles(page.Width), NbTiles(page.Height)), (int)diviser);
        }
    }
}
