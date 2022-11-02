using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using GeneaGrab.Services;

namespace GeneaGrab.Views
{
    public partial class RegistriesPage : Page, ITabPage
    {
        public Symbol IconSource => Symbol.Library;
        public string? DynaTabHeader => Provider?.Name;
        public string? Identifier => Provider?.ID;
        private Provider? Provider { get; set; }

        public RegistriesPage() => InitializeComponent();

        private void InitializeComponent()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }

        protected ObservableCollection<RegistriesTreeStructure> Items { get; } = new();

        public override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not Provider provider) return;
            Provider = provider;
            MainWindow.UpdateSelectedTitle();

            Items.Clear();
            foreach (var location in Provider.Registries.Values.OrderBy(r => r.From).GroupBy(r => new Location(r.LocationID, r.Location)).OrderBy(l => l.Key ?? "zZzZ"))
            {
                RegistriesTreeStructure? loc = (string)location.Key;
                foreach (var district in location.GroupBy(r => r.DistrictID ?? r.District).OrderBy(d => d.Key ?? "zZzZ"))
                {
                    RegistriesTreeStructure? dis = district.Key;
                    foreach (var registry in district.OrderBy(r => r.From))
                    {
                        if (dis is null && loc is null) Items.Add(registry);
                        else if (dis is null) loc!.Children.Add(registry);
                        else dis.Children.Add(registry);
                    }
                    if (dis != null) loc?.Children.Add(dis);
                }
                if (loc != null) Items.Add(loc);
            }
        }
        class Location : System.Collections.Generic.IEqualityComparer<Location>
        {
            public readonly string Id;
            public readonly string? Name;
            public Location(string id, string? name)
            {
                Id = id;
                Name = name;
            }
            public static implicit operator string(Location l) => l.Name ?? l.Id;

            public override bool Equals(object? obj) => Equals(this, obj as Location);
            public bool Equals(Location? x, Location? y) => !string.IsNullOrEmpty(x?.Id) && !string.IsNullOrEmpty(y?.Id) ? x.Id == y.Id : x?.Name == y?.Name;
            public override int GetHashCode() => Id.GetHashCode();
            public int GetHashCode(Location obj) => obj.GetHashCode();
        }

        private void RegisterList_ItemInvoked(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1 || sender is not TreeView treeView || e.AddedItems[0] is not RegistriesTreeStructure data) return;

            var node = (TreeViewItem)treeView.ItemContainerGenerator.Index.ContainerFromItem(data);
            if (data.Children.Any()) node.IsExpanded = !node.IsExpanded;
            else if (data.Register != null) NavigationService.Navigate(typeof(RegistryViewer), new RegistryInfo(data.Register));
        }
    }

    public class RegistriesTreeStructure
    {
        public string Title { get; }
        public string? SubTitle { get; }
        public ObservableCollection<RegistriesTreeStructure> Children { get; } = new();

        public Registry? Register { get; }

        public static implicit operator RegistriesTreeStructure?(string? title) => title is null ? null : new RegistriesTreeStructure(title);
        public RegistriesTreeStructure(string title, string? subtitle = null)
        {
            Title = title;
            SubTitle = subtitle;
        }

        public static implicit operator RegistriesTreeStructure(Registry registry) => new(registry);
        public RegistriesTreeStructure(Registry register)
        {
            Title = register.Name;
            SubTitle = PageCount(register);
            Register = register;
        }
        string PageCount(Registry register)
        {
            var count = register.Pages.Length;
            if (count == 0) return "Aucune page";
            if (count == 1) return "1 page";
            return $"{count} pages";
        }
    }
}
