using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;

namespace GeneaGrab.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage() => InitializeComponent();

        public ObservableCollection<Provider> Providers { get; } = new ObservableCollection<Provider>(Data.Providers.Values);
        private void ProvidersList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var Provider = e.ClickedItem as Provider;
            Frame.Navigate(typeof(RegistriesPage), Provider);
        }

        public static async Task LoadData()
        {
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
                    var kv = JsonConvert.DeserializeObject<KeyValuePair<string, GeneaGrab.Registry>>(await (await reg.GetParentAsync()).ReadFile(reg.Name));
                    provider.Value.Registries.Add(kv.Key, kv.Value);
                }
            }
        }


        public static void SaveData() => Task.Run(SaveDataAsync);
        public static async Task SaveDataAsync()
        {
            var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            foreach (var provider in Data.Providers)
            {
                var folder = await dataFolder.CreateFolder(provider.Key);
                await folder.WriteFile("Locations.json", JsonConvert.SerializeObject(provider.Value.Locations, Formatting.Indented));
                foreach(var registry in provider.Value.Registries) await folder.CreateFolderPath(registry.Value.ID).WriteFile("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));
            }
        }
    }
}
