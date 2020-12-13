using FileFormat;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Plateformes
{
    public class AD06 : Plateforme
    {
        Infos Informations;
        public ref Infos GetInfos() => ref Informations;
        public AD06(System.Uri URL) { Informations = new Infos { URL = URL.OriginalString, Plateforme = "AD06" }; }

        public static bool CheckURL(System.Uri URL) => URL.Host == "www.basesdocumentaires-cg06.fr" && URL.AbsolutePath.StartsWith("/archives/ImageZoomViewerEC.php");

        public IEnumerator Infos(System.Action<Infos> onComplete)
        {
            Informations.URL = System.Web.HttpUtility.UrlDecode(Informations.URL, System.Text.Encoding.GetEncoding("iso-8859-1"));
            var query = System.Web.HttpUtility.ParseQueryString(Informations.Uri.Query);
            Informations.ID = query["IDDOC"];

            var pagesRequest = UnityWebRequest.Get(Informations.URL);
            pagesRequest.SendWebRequest();
            while (!pagesRequest.isDone)
            {
                Manager.SetProgress(pagesRequest.downloadProgress);
                yield return new WaitForEndOfFrame();
            }
            if (pagesRequest.isHttpError || pagesRequest.isNetworkError) { Informations.Error = true; onComplete(Informations); yield break; }
            var regex = Regex.Matches(pagesRequest.downloadHandler.text, "imagesListe\\.push\\('(?<page>.*?)'\\)");
            var pages = regex.Cast<Match>().Select(m => m.Groups["page"]?.Value).ToArray();
            var Pages = new List<Page>();
            for (int i = 1; i <= pages.Length; i++)
                Pages.Add(new Page { Number = i, URL = $"http://www.basesdocumentaires-cg06.fr/archives/ImageViewerTargetJP2.php?imagePath={pages[i - 1]}" });
            Informations.Pages = Pages.ToArray();

            var commune = query["COMMUNE"];
            if (!string.IsNullOrWhiteSpace(query["PAROISSE"])) commune += $" {query["PAROISSE"]}";
            Informations.Name = $"{commune} ({query["DATE"]}) - {query["TYPEACTE"]} - {Informations.ID}";
            Informations.Page = int.TryParse(query["page"], out var _p) ? _p : 1;

            onComplete(Informations);
        }

        public IEnumerator GetTile(int _, System.Action<Infos> onComplete) => GetTiles(onComplete, true); //The zoom parameter isn't supported
        public IEnumerator Download(System.Action<Infos> onComplete) => GetTiles(onComplete, true);
        public IEnumerator GetTiles(System.Action<Infos> onComplete, bool progress)
        {
            if(Informations.CurrentPage.Image != null)
            {
                onComplete(Informations);
                yield break;
            }

            var urlRequest = UnityWebRequest.Get(Informations.CurrentPage.URL);
            yield return urlRequest.SendWebRequest();
            urlRequest = UnityWebRequest.Get(urlRequest.downloadHandler.text);
            yield return urlRequest.SendWebRequest();
            var id = Regex.Match(urlRequest.downloadHandler.text, "location\\.replace\\(\"Fullscreen\\.ics\\?id=(?<id>.*?)&").Groups["id"]?.Value;
            if (string.IsNullOrWhiteSpace(id)) yield break;

            var download = UnityWebRequestTexture.GetTexture($"http://www.basesdocumentaires-cg06.fr:8080/ics/Converter?id={id}");
            Manager.SetProgress(0.5F); //We can't track the progress because we don't know the final size
            yield return download.SendWebRequest();
            Informations.CurrentPage.Image = DownloadHandlerTexture.GetContent(download);

            onComplete(Informations);
            if (progress) Manager.SetProgress(1);
        }
    }
}
