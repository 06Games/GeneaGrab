using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GeneaGrab.Views;

public partial class ProviderList : UserControl
{
    public ProviderList()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        DataContext = this;
        AvaloniaXamlLoader.Load(this);
    }

    public Collection<Provider> Providers => new(Data.Providers.Values.ToList());
    protected void ProvidersList_OnSelectionChanged(object? _, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count < 1) return;

        var provider = e.AddedItems[0] as Provider;
            
    }
}

