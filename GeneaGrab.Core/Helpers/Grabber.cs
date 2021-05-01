using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Net;
using System.Threading.Tasks;

public static class Grabber
{
    public static int CalculateIndex(Args args)
    {
        // Retourne le zoomlevel maximum
        // Chaque zoomlevel multiplie la taille de l'image par deux. Le zoomlevel 0 correspond à l'image entière contenue dans une seule tile.
        return (int)Math.Ceiling(Math.Log(Math.Max(args.w, args.h) / args.tileSize) / Math.Log(2));
    }

    public class Args { public int w, h, tileSize; }

    public static async Task<Args> Zoomify(string baseURL)
    {
        var client = new WebClient();
        client.Headers.Set(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");

        string data = null;
        await Task.Run(() => data = client.DownloadString(new Uri($"{baseURL}ImageProperties.xml")));

        var dataResp = new FileFormat.XML.XML($"<r>{data}</r>");
        var layer = dataResp.RootElement.GetItem("IMAGE_PROPERTIES");

        return new Args
        {
            tileSize = int.TryParse(layer.Attribute("TILESIZE"), out var tileSize) ? tileSize : 0,
            w = int.TryParse(layer.Attribute("WIDTH"), out var w) ? w : 0,
            h = int.TryParse(layer.Attribute("HEIGHT"), out var h) ? h : 0
        };
    }

    public static (Point tiles, int diviser) NbTiles(Args args, double multiplier)
    {
        var diviser = Math.Pow(2, CalculateIndex(args) - multiplier);
        int NbTiles(int val) => (int)Math.Ceiling(val / diviser / args.tileSize);
        return (new Point(NbTiles(args.w), NbTiles(args.h)), (int)diviser);
    }
    public static async Task<Image> GetTile(string url)
    {
        var client = new WebClient();
        client.Headers.Set(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        try { return Image.Load(await client.OpenReadTaskAsync(new Uri(url))); }
        catch (WebException) { return new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(1, 1, Color.Black); }
    }
    public static Image MergeTile(this Image tex, Image tile, (Args args, int scale, Point pos) a) => MergeTile(tex, tile, a.args, a.scale, a.pos);
    public static Image MergeTile(this Image tex, Image tile, Args args, int scale, Point pos)
    {
        tile.Mutate(x => x.Resize(tile.Width * scale, tile.Height * scale));
        var point = new Point(pos.X * args.tileSize * scale, pos.Y * args.tileSize * scale);
        tex.Mutate(x => x.DrawImage(tile, point, 1));
        return tex;
    }
}
