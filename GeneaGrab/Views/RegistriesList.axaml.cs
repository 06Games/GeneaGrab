using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using GeneaGrab.Core.Models;
using GeneaGrab.Services;
using GeneaGrab.Strings;

namespace GeneaGrab.Views
{
    public partial class RegistriesPage : Page, ITabPage
    {
        public Symbol IconSource => Symbol.Library;
        public string? DynaTabHeader => Provider?.Name;
        public string? Identifier => Provider?.ID;
        private Provider? Provider { get; set; }

        public RegistriesPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected ObservableCollection<RegistriesTreeStructure> Items { get; } = new();

        public override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not Provider provider) return;
            Provider = provider;
            MainWindow.UpdateSelectedTitle();

            Items.Clear();
            foreach (var registry in Provider.Registries.Values)
            {
                var parent = Items;
                foreach (var location in Array.Empty<string?>().Append(registry.Location).Append(registry.District)) // To add LocationDetails in the structure, replace the empty array
                {
                    if (string.IsNullOrWhiteSpace(location)) continue;
                    var container = parent.FirstOrDefault(c => c.Title == location);
                    if (container is null)
                    {
                        container = new RegistriesTreeStructure(location,
                            location != registry.Location || registry.LocationDetails == null ? null
                                : string.Join(", ", registry.LocationDetails)); // We uses LocationDetails as subtitle of the Location
                        InsertInPlace(parent, container);
                    }
                    parent = container.Children;
                }
                InsertInPlace(parent, registry);
            }

            void InsertInPlace<T>(IList<T> list, T item) where T : IComparable<T>
            {
                var index = list.BinarySearch(item);
                if (index < 0) index = ~index;
                list.Insert(index, item);
            }
        }

        private void RegisterList_ItemInvoked(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1 || sender is not TreeView treeView || e.AddedItems[0] is not RegistriesTreeStructure data) return;
            if (treeView.TreeContainerFromItem(data) is not TreeViewItem node) return;
            if (data.Children.Any()) node.IsExpanded = !node.IsExpanded;
            else if (data.Registry != null) NavigationService.Navigate(typeof(RegistryViewer), new RegistryInfo(data.Registry));
        }
    }

    public class RegistriesTreeStructure : IComparable<RegistriesTreeStructure>
    {
        public string Title { get; }
        public string? Subtitle { get; }
        public ObservableCollection<RegistriesTreeStructure> Children { get; } = new();

        public Registry? Registry { get; }

        public RegistriesTreeStructure(string title, string? subtitle = null)
        {
            Title = title;
            Subtitle = subtitle;
        }
        private RegistriesTreeStructure(Registry registry) : this(registry.Name, string.Format(UI.Registry_PageCount, registry.Pages.Length))
        {
            Registry = registry;
        }

        public static implicit operator RegistriesTreeStructure(Registry registry) => new(registry);

        public int CompareTo(RegistriesTreeStructure? other)
        {
            if (other is null) return 1;

            var compare = 0;
            if (Registry?.From != null && other.Registry?.From != null) compare += Registry.From.CompareTo(other.Registry.From) * 10;
            else if (Registry == null && other.Registry != null) compare += -10;
            else if (Registry != null && other.Registry == null) compare += 10;
            return compare + string.Compare(Title, other.Title, StringComparison.CurrentCulture);
        }
    }
}
