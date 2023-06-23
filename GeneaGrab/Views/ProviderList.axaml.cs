﻿using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using FluentAvalonia.UI.Controls;
using GeneaGrab.Core.Models;
using GeneaGrab.Helpers;
using GeneaGrab.Services;

namespace GeneaGrab.Views;

public partial class ProviderList : Page, ITabPage
{
    public Symbol IconSource => Symbol.World;
    public string? DynaTabHeader => null;
    public string? Identifier => null;

    public ProviderList()
    {
        InitializeComponent();
        DataContext = this;
        
        LocalData.LoadData().ContinueWith(_ => Dispatcher.UIThread.Post(() =>
        {
            Providers.Clear();
            Providers.Add(Data.Providers.Values);
        }));
    }

    public ObservableCollection<Provider> Providers { get; } = new(Data.Providers.Values);
    protected void ProvidersList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count < 1) return;
        if (sender is ListBox listBox) listBox.UnselectAll();

        var provider = e.AddedItems[0] as Provider;
        NavigationService.Navigate(typeof(RegistriesPage), provider);
    }
}
