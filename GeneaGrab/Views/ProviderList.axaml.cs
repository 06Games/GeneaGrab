using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using GeneaGrab.Services;

namespace GeneaGrab.Views;

public partial class ProviderList : UserControl, ITabPage
{
    public Symbol IconSource => Symbol.World;
    public string? DynaTabHeader => null;
    public string? Identifier => null;
    
    public ProviderList() => InitializeComponent();

    private void InitializeComponent()
    {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    public ObservableCollection<Provider> Providers => new(Data.Providers.Values);
    protected void ProvidersList_OnSelectionChanged(object? _, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count < 1) return;

        var provider = e.AddedItems[0] as Provider;
        NavigationService.Navigate(typeof(RegistriesPage), provider);
    }
}

