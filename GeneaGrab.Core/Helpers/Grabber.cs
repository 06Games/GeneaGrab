using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Http;

namespace GeneaGrab
{
    public static class Grabber
    {
        public static async System.Threading.Tasks.Task<Image> GetImage(string url, HttpClient client = null)
        {
            if (client is null) client = new HttpClient();
            try { return await Image.LoadAsync(await client.GetStreamAsync(url).ConfigureAwait(false)).ConfigureAwait(false); }
            catch (HttpRequestException e)
            {
                Data.Error($"Failed to retrieve image at {url}", e);
                return new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(1, 1, Color.Black);
            }
        }
        public static Image MergeTile(this Image tex, Image tile, (int tileSize, int scale, Point pos) a) => MergeTile(tex, tile, a.tileSize, a.scale, a.pos);
        public static Image MergeTile(this Image tex, Image tile, int tileSize, int scale, Point pos)
        {
            if (tile is null) { Data.Warn($"The tile at {pos} is null", null); return tex; }
            tile.Mutate(x => x.Resize(tile.Width * scale, tile.Height * scale));
            var point = new Point(pos.X * tileSize * scale, pos.Y * tileSize * scale);
            tex.Mutate(x => x.DrawImage(tile, point, 1));
            return tex;
        }
    }
}
