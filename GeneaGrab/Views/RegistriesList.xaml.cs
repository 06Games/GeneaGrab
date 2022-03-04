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

        private ObservableCollection<RegistriesTreeStructure> _items = new ObservableCollection<RegistriesTreeStructure>();
        public ObservableCollection<RegistriesTreeStructure> Items => _items;
        public Provider provider { get; private set; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            provider = e.Parameter as Provider;
            if (provider is null) return;
            ShellPage.UpdateSelectedTitle();

            _items.Clear();
            foreach (var location in provider.Registries.Values.OrderBy(r => r.From).GroupBy(r => new Location { ID = r.LocationID, Name = r.Location }).OrderBy(l => l.Key ?? "zZzZ"))
            {
                RegistriesTreeStructure loc = (string)location.Key;
                foreach (var district in location.GroupBy(r => r.DistrictID ?? r.District).OrderBy(d => d.Key ?? "zZzZ"))
                {
                    RegistriesTreeStructure dis = district.Key;
                    foreach (var registry in district.OrderBy(r => r.From))
                    {
                        if (dis is null && loc is null) _items.Add(registry);
                        else if (dis is null) loc.Children.Add(registry);
                        else dis.Children.Add(registry);
                    }
                    if (dis != null) loc.Children.Add(dis);
                }
                if (loc != null) _items.Add(loc);
            }
        }
        class Location : System.Collections.Generic.IEqualityComparer<Location>
        {
            public string ID;
            public string Name;
            public static implicit operator string(Location l) => l.Name ?? l.ID;

            public override bool Equals(object obj) => Equals(this, obj as Location);
            public bool Equals(Location x, Location y) => (!string.IsNullOrEmpty(x.ID) && !string.IsNullOrEmpty(y.ID)) ? x.ID == y.ID : x.Name == y.Name;
            public override int GetHashCode() => (ID ?? Name ?? "").GetHashCode();
            public int GetHashCode(Location obj) => obj.GetHashCode();
        }

        private void RegisterList_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs e)
        {
            var data = e.InvokedItem as RegistriesTreeStructure;
            var node = sender.NodeFromContainer(sender.ContainerFromItem(data));
            if (node.HasChildren) node.IsExpanded = !node.IsExpanded;
            else if (data.Register != null) Frame.Navigate(typeof(Registry), new RegistryInfo(data.Register));
        }
    }

    public class RegistriesTreeStructure
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public ObservableCollection<RegistriesTreeStructure> Children { get; set; } = new ObservableCollection<RegistriesTreeStructure>();

        public GeneaGrab.Registry Register { get; set; }

        public static implicit operator RegistriesTreeStructure(string title) => title is null ? null : new RegistriesTreeStructure(title);
        public RegistriesTreeStructure(string title, string subtitle = null)
        {
            Title = title;
            SubTitle = subtitle;
        }

        public static implicit operator RegistriesTreeStructure(GeneaGrab.Registry registry) => new RegistriesTreeStructure(registry);
        public RegistriesTreeStructure(GeneaGrab.Registry register)
        {
            Title = register.Name;
            SubTitle = PageCount(register);
            Register = register;
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
