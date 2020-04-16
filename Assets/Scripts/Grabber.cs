using FileFormat;
using FileFormat.XML;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/*
 Sample URLs:
 - Cantal: http://archives.cantal.fr/accounts/mnesys_ad15/datas/medias/Etat civil et registres paroissiaux/Saint-Remy-de-Salers/BMS 1706-1790/AD015_5Mi387_01331_jpg_
 - Gironde: https://archives.gironde.fr/ark:/25651/vta9239124a2e2ba619/daogrp/0/8
*/

public class Grabber : MonoBehaviour
{
    [Header("Grab")]
    public InputField url;
    public Text progress;

    [Header("Preview")]
    public GameObject previewGO;
    public RawImage preview;
    Texture2D tex;

    [Header("Export")]
    public GameObject exportGO;
    public InputField path;
    string Name;

    public void Download(string departement)
    {
        var URL = url.text;
        if (departement == "Cantal") StartCoroutine(cantal(URL.TrimEnd('/')));
        else if (departement == "Gironde") StartCoroutine(gironde(URL));
    }

    IEnumerator cantal(string url)
    {
        var data = UnityWebRequest.Get(url + "/p.xml");
        yield return data.SendWebRequest();
        var dataResp = new XML(data.downloadHandler.text);
        var betterLayer = dataResp.RootElement.GetItems("layer").OrderByDescending(l => l.Attribute("w")).FirstOrDefault();
        var index = 3;

        int.TryParse(betterLayer.Attribute("t"), out var tileSize);
        var xTiles = int.TryParse(betterLayer.Attribute("w"), out var w) ? Mathf.CeilToInt(w / (float)tileSize) : 0;
        var yTiles = int.TryParse(betterLayer.Attribute("h"), out var h) ? Mathf.CeilToInt(h / (float)tileSize) : 0;

        tex = new Texture2D(w, h);
        progress.gameObject.SetActive(true);
        for (int y = 0; y < yTiles; y++)
        {
            for (int x = 0; x < xTiles; x++)
            {
                var tile = UnityWebRequestTexture.GetTexture($"{url}/{index}_{x + (y * xTiles)}.jpg");
                progress.text = ((x + (y * xTiles)) / (float)(xTiles * yTiles) * 100F).ToString("0") + "%";
                tile.SendWebRequest();
                while (!tile.isDone) yield return new WaitForEndOfFrame();
                var tileResp = DownloadHandlerTexture.GetContent(tile);
                tex.SetPixels(x * tileSize, h - (y * tileSize) - tileResp.height, tileResp.width, tileResp.height, tileResp.GetPixels());
            }
        }

        progress.gameObject.SetActive(false);
        tex.Apply();
        preview.texture = tex;
        preview.GetComponent<AspectRatioFitter>().aspectRatio = w / (float)h;
        previewGO.SetActive(true);
        Name = Path.GetFileName(url);
        exportGO.SetActive(true);
    }

    IEnumerator gironde(string url)
    {
        previewGO.SetActive(false);
        exportGO.SetActive(false);

        var getBin = UnityWebRequest.Get(url);
        getBin.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        yield return getBin.SendWebRequest();
        var binResp = getBin.downloadHandler.text
            .Replace("\t", "").Replace("\r", "").Replace("\n", "")
            .Split(new string[] { "<script type=\"text/javascript\">//<![CDATA[var binocle;require(['binocle'], function(Binocle) {binocle = new Binocle(" }, System.StringSplitOptions.RemoveEmptyEntries)[1]
            .Split(new string[] { ");});//]]></script>" }, System.StringSplitOptions.RemoveEmptyEntries)[0];

        var bin = new JSON(binResp).Value<string>("source");
        var pages = UnityWebRequest.Get(bin);
        pages.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        yield return pages.SendWebRequest();
        var pagesResp = new JSON(pages.downloadHandler.text);
        var page = pagesResp.jToken.Value<JArray>("items").ElementAtOrDefault(int.TryParse(Path.GetFileName(url), out var pageIndex) ? pageIndex - 1 : 0).Value<string>("source");

        var data = UnityWebRequest.Get($"https://archives.gironde.fr/cgi-bin/iipsrv.fcgi?zoomify={page}/ImageProperties.xml");
        data.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
        yield return data.SendWebRequest();
        var dataResp = new XML($"<r>{data.downloadHandler.text}</r>");
        var layer = dataResp.RootElement.GetItem("IMAGE_PROPERTIES");
        var index = 4;

        int.TryParse(layer.Attribute("TILESIZE"), out var tileSize);
        var xTiles = int.TryParse(layer.Attribute("WIDTH"), out var w) ? Mathf.CeilToInt(w / (float)tileSize) : 0;
        var yTiles = int.TryParse(layer.Attribute("HEIGHT"), out var h) ? Mathf.CeilToInt(h / (float)tileSize) : 0;

        tex = new Texture2D(w, h);
        progress.gameObject.SetActive(true);
        for (int y = 0; y < yTiles; y++)
        {
            for (int x = 0; x < xTiles; x++)
            {
                var tile = UnityWebRequestTexture.GetTexture($"https://archives.gironde.fr/cgi-bin/iipsrv.fcgi?zoomify={page}/TileGroup0/{index}-{x}-{y}.jpg");
                tile.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
                progress.text = ((x + (y * xTiles)) / (float)(xTiles * yTiles) * 100F).ToString("0") + "%";
                tile.SendWebRequest();
                while (!tile.isDone) yield return new WaitForEndOfFrame();
                var tileResp = DownloadHandlerTexture.GetContent(tile);
                tex.SetPixels(x * tileSize, h - (y * tileSize) - tileResp.height, tileResp.width, tileResp.height, tileResp.GetPixels());
            }
        }

        progress.gameObject.SetActive(false);
        tex.Apply();
        preview.texture = tex;
        preview.GetComponent<AspectRatioFitter>().aspectRatio = w / (float)h;
        previewGO.SetActive(true);
        Name = pagesResp.GetCategory("batch").Value<string>("title") + "_" + pageIndex;
        exportGO.SetActive(true);
    }

    public void Export() => File.WriteAllBytes(path.text + "/" + Name + ".jpg", tex.EncodeToJPG());
}
