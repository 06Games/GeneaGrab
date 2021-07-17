using System.Collections.ObjectModel;
using System.Linq;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GeneaGrab.Views
{
    public sealed partial class RegistriesPage : Page, ITabPage
    {
        public Symbol IconSource => Symbol.Library;
        public string DynaTabHeader => provider?.Name;
        public string Identifier => provider?.ID;

        public RegistriesPage() => InitializeComponent();

        private ObservableCollection<LocationOrRegisterItem> _items = new ObservableCollection<LocationOrRegisterItem>();
        public ObservableCollection<LocationOrRegisterItem> Items => _items;
        public Provider provider { get; private set; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            provider = e.Parameter as Provider;
            if (provider is null) return;
            ShellPage.UpdateSelectedTitle();

            _items.Clear();
            foreach (var registry in provider.Registries.Values)
            {
                if (string.IsNullOrWhiteSpace(registry.LocationID)) { _items.Add(registry); continue; }
                if (provider.Locations.ContainsKey(registry.LocationID ?? "")) continue;

                var loc = _items.FirstOrDefault(i => i.Location?.ID == registry.LocationID);
                if (loc is null)
                {
                    loc = new Location(provider) { ID = registry.LocationID, Name = registry.LocationID };
                    _items.Add(loc);
                }
                loc.Children.Add(registry);
            }
            foreach (var location in provider.Locations.Values) _items.Add(location);
        }

        private void RegisterList_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs e)
        {
            var data = e.InvokedItem as LocationOrRegisterItem;
            var node = sender.NodeFromContainer(sender.ContainerFromItem(data));
            if (node.HasChildren) node.IsExpanded = !node.IsExpanded;
            else if (data.Register != null) Frame.Navigate(typeof(Registry), new RegistryInfo { ProviderID = provider.ID, LocationID = data.Location?.ID, RegistryID = data.Register.ID });
        }
    }

    public class LocationOrRegisterItem
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public ObservableCollection<LocationOrRegisterItem> Children { get; set; } = new ObservableCollection<LocationOrRegisterItem>();

        public Location Location { get; set; }
        public GeneaGrab.Registry Register { get; set; }


        public static implicit operator LocationOrRegisterItem(Location location) => new LocationOrRegisterItem(location);
        public LocationOrRegisterItem(Location location)
        {
            Title = location.Name;
            SubTitle = location.District;
            Location = location;
            Children = new ObservableCollection<LocationOrRegisterItem>(location.Registers.Select(r => new LocationOrRegisterItem(r, location)));
        }

        public static implicit operator LocationOrRegisterItem(GeneaGrab.Registry registry) => new LocationOrRegisterItem(registry, null);
        public LocationOrRegisterItem(GeneaGrab.Registry register, Location location)
        {
            Title = register.Name;
            SubTitle = PageCount(register);
            Register = register;
            Location = location;
        }
        string PageCount(GeneaGrab.Registry register)
        {
            var count = register.Pages.Length;
            if (count == 0) return "Aucune page";
            else if (count == 1) return "1 page";
            else return $"{count} pages";
        }
    }
}
