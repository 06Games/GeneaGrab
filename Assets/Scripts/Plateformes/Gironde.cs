using FileFormat;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.Networking;


namespace Plateformes
{
    public class Gironde : Plateforme
    {
        readonly string url;
        public ref Infos GetInfos() => throw new System.NotImplementedException();
        public Gironde(System.Uri URL) { url = "https://" + URL.Host + URL.AbsolutePath; }

        public static bool CheckURL(System.Uri URL) => URL.Host == "archives.gironde.fr";

        public IEnumerator Infos(System.Action<Infos> onComplete)
        {
            var getBin = UnityWebRequest.Get(url);
            getBin.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
            yield return getBin.SendWebRequest();
            var binResp = getBin.downloadHandler.text
                .Replace("\t", "").Replace("\r", "").Replace("\n", "")
                .Split(new[] { "<script type=\"text/javascript\">//<![CDATA[var binocle;require(['binocle'], function(Binocle) {binocle = new Binocle(" }, System.StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(new[] { ");});//]]></script>" }, System.StringSplitOptions.RemoveEmptyEntries)[0];

            var bin = new JSON(binResp).Value<string>("source");
            var pages = UnityWebRequest.Get(bin);
            pages.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
            yield return pages.SendWebRequest();
            var pagesResp = new JSON(pages.downloadHandler.text);
            var page = pagesResp.jToken.Value<JArray>("items").ElementAtOrDefault(int.TryParse(Path.GetFileName(url), out var pageIndex) ? pageIndex - 1 : 0).Value<string>("source");
            //yield return Grabber.Zoomify($"https://archives.gironde.fr/cgi-bin/iipsrv.fcgi?zoomify={page}/", pagesResp.GetCategory("batch").Value<string>("title") + "_" + pageIndex);
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
