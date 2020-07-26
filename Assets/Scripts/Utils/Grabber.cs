using FileFormat.XML;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class Grabber
{
    public static int CalculateIndex(Args args)
    {
        // Retourne le zoomlevel maximum
        // Chaque zoomlevel multiplie la taille de l'image par deux. Le zoomlevel 0 correspond à l'image entière contenue dans une seule tile.
        return Mathf.CeilToInt(Mathf.Log(Mathf.Max(args.w, args.h) / args.tileSize) / Mathf.Log(2));
    }

    public class Args { public int w, h, tileSize; }

    public static IEnumerator Zoomify(string baseURL, System.Action<Args> onComplete)
    {
        var data = UnityWebRequest.Get($"{baseURL}ImageProperties.xml");
        data.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        yield return data.SendWebRequest();
        var dataResp = new XML($"<r>{data.downloadHandler.text}</r>");
        var layer = dataResp.RootElement.GetItem("IMAGE_PROPERTIES");

        onComplete(new Args()
        {
            tileSize = int.TryParse(layer.Attribute("TILESIZE"), out var tileSize) ? tileSize : 0,
            w = int.TryParse(layer.Attribute("WIDTH"), out var w) ? w : 0,
            h = int.TryParse(layer.Attribute("HEIGHT"), out var h) ? h : 0
        });
    }

    public static (Vector2Int, int) NbTiles(Args args, float multiplier)
    {
        var diviser = Mathf.Pow(2, CalculateIndex(args) - multiplier);
        int NbTiles(int val) => Mathf.CeilToInt(val / diviser / args.tileSize);
        return (new Vector2Int(NbTiles(args.w), NbTiles(args.h)), (int)diviser);
    }
    public static IEnumerator GetTile(Args args, int scale, Vector2Int pos, string url, Texture2D tex, System.Action<Texture2D> onComplete)
    {
        var tile = UnityWebRequestTexture.GetTexture(url);
        tile.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        yield return tile.SendWebRequest();
        while (!tile.isDone) yield return new WaitForEndOfFrame();
        try
        {
            var tileResp = DownloadHandlerTexture.GetContent(tile);
            TextureScale.Bilinear(tileResp, tileResp.width * scale, tileResp.height * scale);
            tex.SetPixels(pos.x * args.tileSize * scale, args.h - (pos.y * args.tileSize * scale) - tileResp.height, tileResp.width, tileResp.height, tileResp.GetPixels());
        }
        catch (System.Exception e) { Debug.LogError(tile.url + "\n" + e); }
        onComplete(tex);
    }


    /*void SetVariables(string name, int w, int h)
    {
        progress.gameObject.SetActive(false);
        tex.Apply();
        preview.texture = tex;
        preview.GetComponent<AspectRatioFitter>().aspectRatio = w / (float)h;
        previewGO.SetActive(true);
        Name = name;
        exportGO.SetActive(true);
        grab.interactable = true;
    }*/
}
