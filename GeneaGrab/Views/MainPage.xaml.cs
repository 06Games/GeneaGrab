using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace GeneaGrab.Views
{
    public sealed partial class MainPage : Page, ITabPage
    {
        public Symbol IconSource => Symbol.World;
        public string DynaTabHeader => null;
        public string Identifier => null;

        public MainPage() => InitializeComponent();

        public ObservableCollection<Provider> Providers { get; } = new ObservableCollection<Provider>(Data.Providers.Values);

        private void ProvidersList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var Provider = e.ClickedItem as Provider;
            Frame.Navigate(typeof(RegistriesPage), Provider);
        }
    }
}
