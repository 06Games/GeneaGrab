using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeneaGrab.Core.Models;
using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GeneaGrab.Helpers;

public static class LocalData
{
    private static string AppName => "GeneaGrab";

    /// <summary>Application appdata folder</summary>
    /// <returns>
    /// On Windows: %localappdata%\GeneaGrab
    /// On MacOS and Linux: ~/.local/share/GeneaGrab
    /// </returns>
    public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
    public static readonly string LogFolder = Path.Combine(AppData, "Logs");
    private static readonly DirectoryInfo RegistriesFolder = new(Path.Combine(AppData, "Registries"));

    private const int ThumbnailSize = 512;

    private static bool Loaded { get; set; }
    public static async Task LoadDataAsync(bool bypassLoadedCheck = false)
    {
        if (Loaded && !bypassLoadedCheck) return;
        Log.Information("Loading data");
        Loaded = true;

        foreach (var (providerId, provider) in Data.Providers)
        {
            var folder = RegistriesFolder.CreateFolder(providerId);
            foreach (var reg in Directory.EnumerateFiles(folder.FullName, "Registry.json", SearchOption.AllDirectories).AsParallel())
            {
                var data = await File.ReadAllTextAsync(reg);
                var registry = JsonConvert.DeserializeObject<Registry>(data);
                if (registry == null) Log.Warning("Registry {Registry} file is empty", registry);
                else if (provider.Registries.ContainsKey(registry.ID)) Log.Warning("An registry already has the id {ID}", registry.ID);
                else provider.Registries.Add(registry.ID, registry);
            }
        }
        Log.Information("Data loaded");
    }
    public static Task SaveRegistryAsync(Registry registry) => RegistriesFolder
        .CreateFolder(registry.ProviderID)
        .CreateFolder(registry.ID)
        .WriteFileAsync("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));


    public static FileInfo GetFile(Registry registry, RPage page, bool write = false, bool thumbnail = false)
    {
        var folder = RegistriesFolder.CreateFolderPath(registry.ProviderID, registry.ID);
        if (thumbnail) folder = folder.CreateFolder(".thumbnails");
        return new FileInfo(Path.Combine(folder.FullName, $"p{page.Number}.jpg"));
    }


    public static Stream? GetImage(Registry registry, RPage page, bool thumbnail = false)
    {
        try
        {
            var file = GetFile(registry, page, thumbnail: thumbnail);
            return !file.Exists ? null : file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't get image for {ID} page {Number}", registry.ID, page.Number);
            return null;
        }
    }
    public static async Task<string?> SaveImageAsync(Registry registry, RPage page, Image img, bool thumbnail = false)
    {
        var thumb = await _SaveImageAsync(registry, page, await ToThumbnailAsync(img), true).ConfigureAwait(false);
        return thumbnail ? thumb : await _SaveImageAsync(registry, page, img).ConfigureAwait(false);
    }

    public static async Task<Image> ToThumbnailAsync(this Image img) => await Task.Run(() =>
    {
        var wScale = (float)ThumbnailSize / img.Width;
        var hScale = (float)ThumbnailSize / img.Height;
        var scale = Math.Min(wScale, hScale);
        return img.Clone(x => x.Resize((int)(img.Width * scale), (int)(img.Height * scale)));
    });

    private static async Task<string?> _SaveImageAsync(Registry registry, RPage page, Image img, bool thumbnail = false)
    {
        try
        {
            var file = GetFile(registry, page, true, thumbnail);
            await using var stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);
            await img.SaveAsJpegAsync(stream).ConfigureAwait(false);
            return file.FullName;
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't save image for {ID} page {Number}", registry.ID, page.Number);
            return Path.Combine(RegistriesFolder.FullName, Extensions.GetValidFilename(registry.ProviderID), Extensions.GetValidFilename(registry.ID), $"p{page.Number}.jpg");
        }
    }
}
