using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Search;

namespace GeneaGrab
{
    public static class LocalData
    {
        public const int THUMBNAIL_SIZE = 512;

        public static bool Loaded { get; private set; }
        public static async Task LoadData(bool bypassLoadedCheck = false)
        {
            if (Loaded && !bypassLoadedCheck) return;
            Log.Information("Loading data");
            var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            var queryOptions = new QueryOptions
            {
                FolderDepth = FolderDepth.Deep,
                IndexerOption = IndexerOption.UseIndexerWhenAvailable,
                ApplicationSearchFilter = "Registry.json"
            };

            foreach (var provider in Data.Providers)
            {
                var folder = await dataFolder.CreateFolder(provider.Key);
                foreach (var reg in await folder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync())
                {
                    var registry = JsonConvert.DeserializeObject<Registry>(await (await reg.GetParentAsync()).ReadFile(reg.Name));
                    if (registry != null) provider.Value.Registries.Add(registry.ID, registry);
                    else Log.Warning("Registry file is empty", registry);
                }
            }
            Log.Information("Data loaded");
            Loaded = true;
        }
        public static async Task SaveDataAsync()
        {
            try
            {
                var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                foreach (var provider in Data.Providers)
                {
                    var folder = await dataFolder.CreateFolder(provider.Key);
                    foreach (var registry in provider.Value.Registries) await SaveRegistryAsync(registry.Value, folder);
                }
            }
            catch (Exception e) { Log.Error(e.Message, e); }
        }
        public static async Task SaveRegistryAsync(Registry registry)
        {
            var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            var folder = await dataFolder.CreateFolder(registry.ProviderID);
            await SaveRegistryAsync(registry, folder);
        }
        public static Task SaveRegistryAsync(Registry registry, Windows.Storage.StorageFolder folder) => folder.CreateFolderPath(registry.ID).WriteFile("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));


        public static async Task<Windows.Storage.StorageFile> GetFile(Registry registry, RPage page, bool write = false, bool thumbnail = false)
        {
            Windows.Storage.StorageFolder folder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderPath(registry.ProviderID, registry.ID);
            if (thumbnail) folder = await folder.CreateFolder(".thumbnails");
            return write ? await folder.CreateFileAsync($"p{page.Number}.jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting) : await folder.TryGetItemAsync($"p{page.Number}.jpg") as Windows.Storage.StorageFile;
        }


        public static async Task<Image> GetImageAsync(Registry registry, RPage page, bool thumbnail = false)
        {
            try
            {
                var file = await GetFile(registry, page, thumbnail: thumbnail).ConfigureAwait(false);
                return file is null ? null : await Image.LoadAsync(await file.OpenStreamForReadAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return null;
            }
        }
        public static async Task<string> SaveImageAsync(Registry registry, RPage page, Image img, bool thumbnail = false)
        {
            string path = null;
            if (!thumbnail) path = await _SaveImageAsync(registry, page, img).ConfigureAwait(false);
            var thumb = await _SaveImageAsync(registry, page, img, true).ConfigureAwait(false);
            return thumbnail ? thumb : path;
        }
        private static async Task<string> _SaveImageAsync(Registry registry, RPage page, Image img, bool thumbnail = false)
        {
            try
            {
                var file = await GetFile(registry, page, true, thumbnail).ConfigureAwait(false);
                if (thumbnail)
                {
                    int w = THUMBNAIL_SIZE;
                    int h = THUMBNAIL_SIZE;
                    if (img.Width < img.Height) h = THUMBNAIL_SIZE / img.Width * img.Height;
                    else w = THUMBNAIL_SIZE / img.Height * img.Width;
                    img.Mutate(x => x.Resize(w, h));
                }
                await img.SaveAsJpegAsync(await file.OpenStreamForWriteAsync().ConfigureAwait(false)).ConfigureAwait(false);
                return file.Path;
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, Extensions.GetValidFilename(registry.ProviderID), Extensions.GetValidFilename(registry.ID), $"p{page.Number}.jpg");
            }
        }
    }
}
