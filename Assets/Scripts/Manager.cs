using FileFormat.XML;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


/*
 Sample URLs:
 - Cantal: http://archives.cantal.fr/ark:/16075/a011324371641xzvnaS/1/183
 - Gironde: https://archives.gironde.fr/ark:/25651/vta9239124a2e2ba619/daogrp/0/8
 - Geneanet: https://www.geneanet.org/archives/registres/view/?idcollection=17580&page=2 or https://www.geneanet.org/archives/registres/view/17580/2 or https://www.geneanet.org/archives/registres/view/17580
*/

public class Settings
{
    public string Path = System.Environment.ExpandEnvironmentVariables("%USERPROFILE%/Documents/Geneagrab/".Replace('/', System.IO.Path.DirectorySeparatorChar));
}

public class Manager : MonoBehaviour
{
    Plateforme Plateforme;
    static Manager instance { get; set; }

    [Header("Settings")]
    public Transform SettingsContent;

    [Header("Loading")]
    public GameObject LoadingPanel;
    public Text LoadingTitle;
    public Text LoadingProgress;

    [Header("Infos")]
    public InputField URL;
    public InputField Page;

    [Header("Preview")]
    public ScrollRect PreviewRect;
    [HideInInspector] public Preview PreviewController;
    public RawImage Renderer;

    [Header("Toolbar")]
    public Text Name;

    /*************
     *  Settings *
     *************/
    public Settings Config { get; private set; }
    string ConfigPath => $"{Application.persistentDataPath}/config.xml";
    private void Start()
    {
        instance = this;
        PreviewController = Renderer.GetComponent<Preview>();
        PreviewController.onZoomChanged += RefreshView;

        Config = File.Exists(ConfigPath) ? Utils.XMLtoClass<Settings>(File.ReadAllText(ConfigPath)) : new Settings();
        foreach (Transform parameter in SettingsContent) parameter.GetComponentInChildren<InputField>().text = (string)(typeof(Settings).GetField(parameter.name)?.GetValue(Config) ?? "");
    }
    public void SaveSettings()
    {
        var xml = new XML().CreateRootElement("Settings");
        foreach (Transform parameter in SettingsContent) xml.CreateItem(parameter.name).Value = parameter.GetComponentInChildren<InputField>().text;
        Config = Utils.XMLtoClass<Settings>(xml.xmlFile.ToString());
        File.WriteAllText(ConfigPath, Utils.ClassToXML(Config, false));
    }


    /*************
     *  Loading  *
     *************/
    public static void SetProgress(float value)
    {
        instance.LoadingPanel.SetActive(value < 1);
        instance.LoadingTitle.text = "Veuillez patienter";
        instance.LoadingProgress.text = $"{(value * 100).ToString("0.0")}%";
    }
    IEnumerator Error(string message)
    {
        LoadingPanel.SetActive(true);
        LoadingTitle.text = "<color=red>Erreur</color>";
        LoadingProgress.text = message;
        Debug.LogError(message);
        yield return new WaitForSeconds(2);
        LoadingPanel.SetActive(false);
    }


    /*************
     *  Buttons  *
     *************/
    public void GetInfos()
    {
        Plateforme = null;
        if (!System.Uri.TryCreate(URL.text, System.UriKind.Absolute, out var url)) { StartCoroutine(Error("URL non valide")); return; }

        var plateformes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(t => string.Equals(t.Namespace, "Plateformes", System.StringComparison.Ordinal));
        foreach (var plateforme in plateformes)
        {
            if ((bool)plateforme.GetMethod("CheckURL").Invoke(null, new object[] { url }))
            {
                Plateforme = (Plateforme)System.Activator.CreateInstance(plateforme, url);
                break;
            }
        }

        if (Plateforme == null) { StartCoroutine(Error("URL non reconnue")); return; }

        SetProgress(0);
        StartCoroutine(Plateforme.Infos((infos) =>
        {
            if (infos.Error) { StartCoroutine(Error("Une erreur inattendue est survenu")); return; }
            URL.text = infos.URL;
            Page.text = infos.Page.ToString();
            Name.text = $"<b>{infos.Plateforme}:</b> {infos.Name}";
            RefreshView();
            SetProgress(1);
        }));
    }

    public void ChangePage()
    {
        if (int.TryParse(Page.text, out var p))
        {
            Plateforme.GetInfos().Page = p;
            PreviewController.Reset();
            UnityThread.executeInLateUpdate(RefreshView);
        }
    }
    public void RefreshView() => RefreshView(PreviewController._thisTransform.localScale);
    Coroutine previewLoading;
    public static int wantedZoom { get; private set; }
    public void RefreshView(Vector3 scale)
    {
        if (Plateforme == null) return;
        //Debug.Log(RectRelativeTo(PreviewController._thisTransform, PreviewRect.transform));
        if (previewLoading != null) StopCoroutine(previewLoading);
        wantedZoom = (int)scale.x;
        previewLoading = StartCoroutine(Plateforme.GetTile(wantedZoom, (infos) =>
        {
            if (wantedZoom != (int)scale.x) return;
            var tex = infos.CurrentPage.Image;
            tex.Apply();
            Renderer.texture = tex;
            Renderer.GetComponent<AspectRatioFitter>().aspectRatio = tex.width / (float)tex.height;
        }));
    }

    public void Export()
    {
        StartCoroutine(Plateforme.Download((infos) =>
        {
            var name = infos.Name;
            foreach (var Char in Path.GetInvalidFileNameChars()) name = name.Replace(Char, '_'); //Remove invalid chars
            var dir = $"{Config.Path}/{Plateforme.GetInfos().Plateforme}";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes($"{dir}/{name} - p{infos.Page}.jpg", infos.CurrentPage.Image.EncodeToJPG()); //Save file
            RefreshView();
        }));
    }
    public static Rect RectRelativeTo(RectTransform transform, Transform to)
    {
        Matrix4x4 matrix = to.worldToLocalMatrix * transform.localToWorldMatrix;
        Rect rect = transform.rect;

        Vector3 p1 = new Vector2(rect.xMin, rect.yMin);
        Vector3 p2 = new Vector2(rect.xMax, rect.yMax);

        p1 = matrix.MultiplyPoint(p1);
        p2 = matrix.MultiplyPoint(p2);

        rect.xMin = p1.x * -1;
        rect.yMin = p1.y;
        rect.xMax = p2.x;
        rect.yMax = p2.y;

        return rect;
    }

}
