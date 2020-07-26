using FileFormat.XML;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Plateformes
{
    public class Cantal : Plateforme
    {
        readonly string url;
        public ref Infos GetInfos() => throw new System.NotImplementedException();
        public Cantal(System.Uri URL) { url = "http://" + URL.Host + URL.AbsolutePath.TrimEnd('/'); }

        public static bool CheckURL(System.Uri URL) => URL.Host == "archives.cantal.fr";

        public IEnumerator Infos(System.Action<Infos> onComplete)
        {
            var ark = UnityWebRequest.Get(url);
            yield return ark.SendWebRequest();
            var arkResp = ark.downloadHandler.text
                .Replace("\t", "").Replace("\r", "").Replace("\n", "")
                .Split(new[] { "\"flashVars\", \"" }, System.StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(new[] { "\"" }, System.StringSplitOptions.RemoveEmptyEntries)[0];
            var args = arkResp.Split('&').Select(q => q.Split('=')).ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());

            var pages = UnityWebRequest.Get(args["__server_name"] + args["playlist"]);
            yield return pages.SendWebRequest();
            var pagesResp = new XML(pages.downloadHandler.text);
            var page = pagesResp.RootElement.GetItem("g").GetItems("i").ElementAtOrDefault(int.TryParse(Path.GetFileName(url), out var pageIndex) ? pageIndex - 1 : 0);
            var pageURL = "http://archives.cantal.fr/accounts/mnesys_ad15/datas/medias/" + page.GetItem("a").Value.Replace(".", "_") + "_";

            var data = UnityWebRequest.Get(pageURL + "/p.xml");
            yield return data.SendWebRequest();
            var dataResp = new XML(data.downloadHandler.text);
            var betterLayer = dataResp.RootElement.GetItems("layer").OrderByDescending(l => l.Attribute("w")).FirstOrDefault();
            var index = 3;

            int.TryParse(betterLayer.Attribute("t"), out var tileSize);
            var xTiles = int.TryParse(betterLayer.Attribute("w"), out var w) ? Mathf.CeilToInt(w / (float)tileSize) : 0;
            var yTiles = int.TryParse(betterLayer.Attribute("h"), out var h) ? Mathf.CeilToInt(h / (float)tileSize) : 0;

            //yield return Manager.GetTiles(w, h, xTiles, yTiles, tileSize, (x, y) => $"{pageURL}/{index}_{x + (y * xTiles)}.jpg");
            //Manager.SetVariables(Path.GetFileName(pageURL), w, h);
        }

        public IEnumerator GetTile(int zoom, System.Action<Infos> onComplete)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator Download(System.Action<Infos> onComplete)
        {
            throw new System.NotImplementedException();
        }
    }
}
