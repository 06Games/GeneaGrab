using System.Collections;
using System.Xml.Serialization;

public interface Plateforme
{
    IEnumerator Infos(System.Action<Infos> onComplete);
    IEnumerator GetTile(int zoom, System.Action<Infos> onComplete);
    IEnumerator Download(System.Action<Infos> onComplete);

    ref Infos GetInfos();
}

public class Infos
{
    public bool Error { get; set; }

    public string Plateforme { get; set; }
    public string URL { get; set; }
    public System.Uri Uri { get => System.Uri.TryCreate(URL, System.UriKind.Absolute, out var uri) ? uri : null; }
    public string ID { get; set; }
    public string Name { get; set; }

    public int Page { get; set; }
    [XmlIgnore]
    public ref Page CurrentPage
    {
        get
        {
            for (int i = 0; i < Pages.Length; i++) { if (Pages[i].Number == Page) return ref Pages[i]; }
            return ref Pages[0];
        }
    }

    public Page[] Pages { get; set; }
}
public class Page
{
    public int Number { get; set; }
    public string URL { get; set; }
    [XmlIgnore] public UnityEngine.Texture2D Image { get; set; }

    public int[] Zoom { get; set; }
    public UnityEngine.Vector2Int Tiles { get; set; }
    public Grabber.Args Args { get; set; }
}
