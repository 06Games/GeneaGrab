﻿using GeneaGrab.Services;
using Newtonsoft.Json;
using Serilog;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage.Search;
using Windows.UI.Xaml;

namespace GeneaGrab
{
    public sealed partial class App : Application
    {
        private Lazy<ActivationService> _activationService;
        private ActivationService ActivationService => _activationService.Value;
        public App()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(), $@"{Windows.Storage.ApplicationData.Current.TemporaryFolder.Path}\Logs\{DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss")}.json").CreateLogger();
            Data.Translate = (id, fallback) => Helpers.ResourceExtensions.GetLocalized(Helpers.Resource.Core, id) ?? fallback;
            Data.GetImage = async (registry, page) =>
            {
                try
                {
                    var file = await GetFile(registry, page, false).ConfigureAwait(false);
                    return file is null ? null : await Image.LoadAsync(await file.OpenStreamForReadAsync().ConfigureAwait(false)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error(e.Message, e);
                    return null;
                }
            };
            Data.SaveImage = async (registry, page) =>
            {
                var file = await GetFile(registry, page, true).ConfigureAwait(false);
                try { await page.Image.SaveAsJpegAsync(await file.OpenStreamForWriteAsync().ConfigureAwait(false)).ConfigureAwait(false); } catch (Exception e) { Log.Error(e.Message, e); }
                return file.Path;
            };

            InitializeComponent();
            UnhandledException += OnAppUnhandledException;
            _activationService = new Lazy<ActivationService>(CreateActivationService); // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
        }
        protected override async void OnActivated(IActivatedEventArgs args) => await ActivationService.ActivateAsync(args);
        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) => Log.Fatal(e.Message, e.Exception);
        private ActivationService CreateActivationService() => new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(CreateShell));
        private UIElement CreateShell() => new Views.ShellPage();

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated) await ActivationService.ActivateAsync(args);
            await LoadData().ConfigureAwait(false);
        }

        public static async Task LoadData()
        {
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
                var _l = await folder.ReadFile("Locations.json");
                foreach (var loc in JsonConvert.DeserializeObject<Dictionary<string, Location>>(_l)) provider.Value.Locations.Add(loc.Key, loc.Value);
                foreach (var reg in await folder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync())
                {
                    var kv = JsonConvert.DeserializeObject<KeyValuePair<string, Registry>>(await (await reg.GetParentAsync()).ReadFile(reg.Name));
                    provider.Value.Registries.Add(kv.Key, kv.Value);
                }
            }
            Log.Information("Data loaded");
        }
        public static void SaveData() => Task.Run(SaveDataAsync);
        public static async Task SaveDataAsync()
        {
            var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            foreach (var provider in Data.Providers)
            {
                var folder = await dataFolder.CreateFolder(provider.Key);
                await folder.WriteFile("Locations.json", JsonConvert.SerializeObject(provider.Value.Locations, Formatting.Indented));
                foreach (var registry in provider.Value.Registries) await folder.CreateFolderPath(registry.Value.ID).WriteFile("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));
            }
        }

        public static async Task<Windows.Storage.StorageFile> GetFile(Registry registry, RPage page, bool write = false)
        {
            Windows.Storage.StorageFolder folder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderPath(registry.ProviderID, registry.ID);
            return write ? await folder.CreateFileAsync($"p{page.Number}.jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting) : await folder.TryGetItemAsync($"p{page.Number}.jpg") as Windows.Storage.StorageFile;
        }
    }
}
