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
                IndexerOption = IndexerOption.UseIndexerWhenAvailable
            };

            queryOptions.ApplicationSearchFilter = "Locations.json";
            foreach (var loc in await dataFolder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync())
                foreach (var kv in JsonConvert.DeserializeObject<List<KeyValuePair<string, Location>>>(await (await loc.GetParentAsync()).ReadFile(loc.Name)))
                    Data.Locations.Add(kv.Key, kv.Value);

            queryOptions.ApplicationSearchFilter = "Registry.json";
            foreach (var reg in await dataFolder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync())
            {
                var kv = JsonConvert.DeserializeObject<KeyValuePair<string, GeneaGrab.Registry>>(await (await reg.GetParentAsync()).ReadFile(reg.Name));
                Data.Registries.Add(kv.Key, kv.Value);
            }
        }


        public static void SaveData() => Task.Run(SaveDataAsync);
        public static async Task SaveDataAsync()
        {
            var dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            foreach (var providers in Data.Providers)
                await dataFolder.CreateFolder(providers.Key).WriteFile("Locations.json", JsonConvert.SerializeObject(Data.Locations.Where(l => l.Value.ProviderID == providers.Key), Formatting.Indented));
            foreach (var registry in Data.Registries)
                await dataFolder.CreateFolderPath(registry.Value.ProviderID, registry.Value.ID).WriteFile("Registry.json", JsonConvert.SerializeObject(registry, Formatting.Indented));
        }
    }
}
