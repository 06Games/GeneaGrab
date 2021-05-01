using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace GeneaGrab.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage() => InitializeComponent();

        public ObservableCollection<Provider> Providers => new ObservableCollection<Provider>(Data.Providers.Values);
        private void ProvidersList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var Provider = e.ClickedItem as Provider;
            Frame.Navigate(typeof(RegistriesPage), Provider);
        }

        public static async Task LoadData()
        {
            var dataFolder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolder("Data");
            foreach (var loc in JsonConvert.DeserializeObject<Dictionary<string, Location>>(await dataFolder.ReadFile("Locations.json"))) Data.Locations.Add(loc.Key, loc.Value);
            foreach (var reg in JsonConvert.DeserializeObject<Dictionary<string, GeneaGrab.Registry>>(await dataFolder.ReadFile("Registries.json"))) Data.Registries.Add(reg.Key, reg.Value);
        }


        public static void SaveData() => Task.Run(SaveDataAsync);
        public static async Task SaveDataAsync()
        {
            var dataFolder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolder("Data");
            await dataFolder.WriteFile("Locations.json", JsonConvert.SerializeObject(Data.Locations, Formatting.Indented));
            await dataFolder.WriteFile("Registries.json", JsonConvert.SerializeObject(Data.Registries, Formatting.Indented));
        }
    }
}
