using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Search;

namespace GeneaGrab
{
    public static class LocalData
    {
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
                    var kv = JsonConvert.DeserializeObject<KeyValuePair<string, Registry>>(await (await reg.GetParentAsync()).ReadFile(reg.Name));
                    provider.Value.Registries.Add(kv.Key, kv.Value);
                }
            }
            Log.Information("Data loaded");
            Loaded = true;
        }
        public static void SaveData() => Task.Run(SaveDataAsync);
        public static async Task SaveDataAsync()
        {
            try
            {
                var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                foreach (var provider in Data.Providers)
                {
                    var folder = await dataFolder.CreateFolder(provider.Key);
                    foreach (var registry in provider.Value.Registries) await folder.CreateFolderPath(registry.Value.ID).WriteFile("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));
                }
            }
            catch (Exception e) { Log.Error(e.Message, e); }
        }

        public static async Task<Windows.Storage.StorageFile> GetFile(Registry registry, RPage page, bool write = false)
        {
            Windows.Storage.StorageFolder folder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderPath(registry.ProviderID, registry.ID);
            return write ? await folder.CreateFileAsync($"p{page.Number}.jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting) : await folder.TryGetItemAsync($"p{page.Number}.jpg") as Windows.Storage.StorageFile;
        }


        public static async Task<Image> GetImageAsync(Registry registry, RPage page)
        {
            try
            {
                var file = await GetFile(registry, page).ConfigureAwait(false);
                return file is null ? null : await Image.LoadAsync(await file.OpenStreamForReadAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return null;
            }
        }
        public static async Task<string> SaveImageAsync(Registry registry, RPage page)
        {
            try
            {
                var file = await GetFile(registry, page, true).ConfigureAwait(false);
                await page.Image.SaveAsJpegAsync(await file.OpenStreamForWriteAsync().ConfigureAwait(false)).ConfigureAwait(false);
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
