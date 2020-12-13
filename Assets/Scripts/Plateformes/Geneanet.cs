using FileFormat;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Plateformes
{
    public class Geneanet : Plateforme
    {
        Infos Informations;
        public ref Infos GetInfos() => ref Informations;
        public Geneanet(System.Uri URL) { Informations = new Infos { URL = URL.OriginalString, Plateforme = "Geneanet" }; }

        public static bool CheckURL(System.Uri URL) => URL.Host == "www.geneanet.org" && URL.AbsolutePath.StartsWith("/archives");

        public IEnumerator Infos(System.Action<Infos> onComplete)
        {
            var regex = Regex.Match(Informations.URL, "(?:idcollection=(?<col>\\d*).*page=(?<page>\\d*))|(?:\\/(?<col>\\d+)(?:\\z|\\/(?<page>\\d*)))");
            Informations.ID = regex.Groups["col"]?.Value;
            if (string.IsNullOrEmpty(Informations.ID)) { Informations.Error = true; onComplete(Informations); yield break; }

            var pages = UnityWebRequest.Get($"https://www.geneanet.org/archives/registres/api/?idcollection={Informations.ID}");
            pages.redirectLimit = 0;
            pages.SendWebRequest();
            while (!pages.isDone)
            {
                Manager.SetProgress(pages.downloadProgress);
                yield return new WaitForEndOfFrame();
            }
            if (pages.isHttpError || pages.isNetworkError) { Informations.Error = true; onComplete(Informations); yield break; }

            Informations.URL = $"https://www.geneanet.org/archives/registres/view/{Informations.ID}";
            var page = UnityWebRequest.Get(Informations.URL);
            yield return page.SendWebRequest();
            var infos = Regex.Match(page.downloadHandler.text, "<h3>(?<location>.*)\\(.*\\| (?<dates>.*)<\\/h3>\\n.*<div class=\"note\">\\n\\s*.*- (?<note>.*)\\n");
            Informations.Name = $"{infos.Groups["location"]} ({infos.Groups["dates"]}) - {infos.Groups["note"]} - {Informations.ID}";

            Informations.Page = int.TryParse(regex.Groups["page"].Success ? regex.Groups["page"].Value : "1", out var _p) ? _p : 1;
            Informations.Pages = new JSON($"{{results: {pages.downloadHandler.text}}}").jToken.Value<JArray>("results")
                .Select(p => new Page { Number = p.Value<int>("page"), URL = p.Value<string>("chemin_image") }).ToArray();
            onComplete(Informations);
        }

        public IEnumerator GetTile(int zoom, System.Action<Infos> onComplete) => GetTiles(zoom, onComplete, false);
        public IEnumerator GetTiles(int zoom, System.Action<Infos> onComplete, bool progress)
        {
            var current = Informations.CurrentPage;
            var chemin_image = System.Uri.EscapeDataString($"doc/{Informations.CurrentPage.URL}");
            var baseURL = $"https://www.geneanet.org/zoomify/?path={chemin_image}/";

            if (current.Args == null) yield return Grabber.Zoomify(baseURL, (a) => current.Args = a);

            var data = Grabber.NbTiles(current.Args, zoom);
            var maxZoom = Grabber.CalculateIndex(current.Args);
            var zoomIndex = zoom < maxZoom ? Mathf.CeilToInt(zoom) : maxZoom;

            if (current.Zoom == null)
            {
                current.Tiles = Grabber.NbTiles(current.Args, maxZoom).Item1;
                current.Zoom = new int[current.Tiles.x * current.Tiles.y];
            }
            if (current.Image == null) current.Image = new Texture2D(current.Args.w, current.Args.h);
            for (int y = 0; y < data.Item1.y; y++)
            {
                for (int x = 0; x < data.Item1.x; x++)
                {
                    var xFactor = current.Tiles.x / data.Item1.x;
                    var yFactor = current.Tiles.y / data.Item1.y;
                    var minI = (x + (y * data.Item1.x * yFactor)) * xFactor;
                    if (current.Zoom[minI] > zoomIndex) continue;

                    if (progress) Manager.SetProgress((x + (y * data.Item1.x)) / (float)(data.Item1.x * data.Item1.y));
                    yield return Grabber.GetTile(current.Args, data.Item2, new Vector2Int(x, y), $"{baseURL}TileGroup0/{zoomIndex}-{x}-{y}.jpg", current.Image, (tex) =>
                    {
                        if (Manager.wantedZoom != zoom) return;
                        for (var yTile = 0; yTile < yFactor; yTile++)
                        {
                            for (var xTile = 0; xTile < xFactor; xTile++) current.Zoom[minI + xTile + yTile * data.Item2] = zoomIndex;
                        }

                        current.Image = tex;
                        Informations.CurrentPage = current;
                        onComplete(Informations);
                    });
                }
            }
            onComplete(Informations);
            if (progress) Manager.SetProgress(1);
        }

        public IEnumerator Download(System.Action<Infos> onComplete) => GetTiles(Grabber.CalculateIndex(Informations.CurrentPage.Args), onComplete, true);
    }
}
