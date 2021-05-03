using GeneaGrab;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Net;
using System.Threading.Tasks;

public static class Grabber
{
    public static int CalculateIndex(RPage page)
    {
        // Retourne le zoomlevel maximum
        // Chaque zoomlevel multiplie la taille de l'image par deux. Le zoomlevel 0 correspond à l'image entière contenue dans une seule tile.
        return (int)Math.Ceiling(Math.Log(Math.Max(page.Width, page.Height) / page.TileSize.GetValueOrDefault(1)) / Math.Log(2));
    }

    public static async Task<(int w, int h, int tileSize)> Zoomify(string baseURL)
    {
        var client = new WebClient();
        client.Headers.Set(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");

        string data = null;
        await Task.Run(() => data = client.DownloadString(new Uri($"{baseURL}ImageProperties.xml")));

        var dataResp = new FileFormat.XML.XML($"<r>{data}</r>");
        var layer = dataResp.RootElement.GetItem("IMAGE_PROPERTIES");

        return (
            int.TryParse(layer.Attribute("WIDTH"), out var w) ? w : 0,
            int.TryParse(layer.Attribute("HEIGHT"), out var h) ? h : 0,
            int.TryParse(layer.Attribute("TILESIZE"), out var tileSize) ? tileSize : 0
        );
    }

    public static (Point tiles, int diviser) NbTiles(RPage page, double multiplier)
    {
        var diviser = Math.Pow(2, CalculateIndex(page) - multiplier);
        int NbTiles(int val) => (int)Math.Ceiling(val / diviser / page.TileSize.GetValueOrDefault(1));
        return (new Point(NbTiles(page.Width), NbTiles(page.Height)), (int)diviser);
    }
    public static async Task<Image> GetTile(string url)
    {
        var client = new WebClient();
        client.Headers.Set(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        try { return Image.Load(await client.OpenReadTaskAsync(new Uri(url))); }
        catch (WebException) { return new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(1, 1, Color.Black); }
    }
    public static Image MergeTile(this Image tex, Image tile, (int tileSize, int scale, Point pos) a) => MergeTile(tex, tile, a.tileSize, a.scale, a.pos);
    public static Image MergeTile(this Image tex, Image tile, int tileSize, int scale, Point pos)
    {
        tile.Mutate(x => x.Resize(tile.Width * scale, tile.Height * scale));
        var point = new Point(pos.X * tileSize * scale, pos.Y * tileSize * scale);
        tex.Mutate(x => x.DrawImage(tile, point, 1));
        return tex;
    }
}
