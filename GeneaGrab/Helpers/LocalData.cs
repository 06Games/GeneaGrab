﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace GeneaGrab.Helpers;

public static class LocalData
{
    public const string AppName = "GeneaGrab";
    public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
    public static readonly string LogFolder = Path.Combine(AppData, "Logs");
    public static readonly DirectoryInfo RegistriesFolder = new(Path.Combine(AppData, "Registries"));
    
    private const int ThumbnailSize = 512;

    private static bool Loaded { get; set; }
    public static async Task LoadData(bool bypassLoadedCheck = false)
    {
        if (Loaded && !bypassLoadedCheck) return;
        Log.Information("Loading data");

        foreach (var provider in Data.Providers)
        { 
            var folder = await RegistriesFolder.CreateFolder(provider.Key);
            foreach (var reg in Directory.EnumerateFiles(folder.FullName, "Registry.json", SearchOption.AllDirectories).AsParallel())
            {
                var data = await File.ReadAllTextAsync(reg);
                var registry = JsonConvert.DeserializeObject<Registry>(data);
                if (registry != null) provider.Value.Registries.Add(registry.ID, registry);
                else Log.Warning("Registry {0} file is empty", registry);
            }
        }
        Log.Information("Data loaded");
        Loaded = true;
    }
    public static async Task SaveDataAsync()
    {
        try
        {
            foreach (var provider in Data.Providers)
            {
                var folder = await RegistriesFolder.CreateFolder(provider.Key).ConfigureAwait(false);
                foreach (var registry in provider.Value.Registries) await SaveRegistryAsync(registry.Value, folder).ConfigureAwait(false);
            }
        }
        catch (Exception e) { Log.Error(e.Message, e); }
    }
    public static async Task SaveRegistryAsync(Registry registry)
    {
        var folder = await RegistriesFolder.CreateFolder(registry.ProviderID).ConfigureAwait(false);
        await SaveRegistryAsync(registry, folder).ConfigureAwait(false);
    }
    public static Task SaveRegistryAsync(Registry registry, DirectoryInfo folder) => folder.CreateFolder(registry.ID).WriteFile("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));


    public static async Task<FileInfo> GetFile(Registry registry, RPage page, bool write = false, bool thumbnail = false)
    { 
        var folder = await RegistriesFolder.CreateFolderPath(registry.ProviderID, registry.ID);
        if (thumbnail) folder = await folder.CreateFolder(".thumbnails");
        return new FileInfo(Path.Combine(folder.FullName, $"p{page.Number}.jpg"));
    }


    public static async Task<Stream?> GetImageAsync(Registry registry, RPage page, bool thumbnail = false)
    {
        try
        {
            var file = await GetFile(registry, page, thumbnail: thumbnail).ConfigureAwait(false);
            return !file.Exists ? null : file.OpenRead();
        }
        catch (Exception e)
        {
            Log.Error(e.Message, e);
            return null;
        }
    }
    public static async Task<string?> SaveImageAsync(Registry registry, RPage page, Image img, bool thumbnail = false)
    {
        var thumb = await _SaveImageAsync(registry, page, img, true).ConfigureAwait(false);
        return thumbnail ? thumb : await _SaveImageAsync(registry, page, img).ConfigureAwait(false);
    }
    private static async Task<string?> _SaveImageAsync(Registry registry, RPage page, Image img, bool thumbnail = false)
    {
        try
        {
            var file = await GetFile(registry, page, true, thumbnail).ConfigureAwait(false);

            if (!thumbnail) return await Save(img);
            
            int w = ThumbnailSize;
            int h = ThumbnailSize;
            if (img.Width < img.Height) h = ThumbnailSize / img.Width * img.Height;
            else w = ThumbnailSize / img.Height * img.Width;
            using var thumb = img.Clone(x => x.Resize(w, h));
            return await Save(thumb);


            async Task<string> Save(Image image)
            {
                await image.SaveAsJpegAsync(file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite)).ConfigureAwait(false);
                return file.FullName;
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message, e);
            return Path.Combine(RegistriesFolder.FullName, Extensions.GetValidFilename(registry.ProviderID), Extensions.GetValidFilename(registry.ID), $"p{page.Number}.jpg");
        }
    }
}