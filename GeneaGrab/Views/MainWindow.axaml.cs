using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using GeneaGrab.Helpers;
using GeneaGrab.Services;

namespace GeneaGrab.Views;

public interface ITabPage
{
    Symbol IconSource { get; }
    string? DynaTabHeader { get; }
    string? Identifier { get; }
}
    
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public GridLength WindowsTitleBarWidth => new(IsWindows ? 150 : 15);
    
    public MainWindow()
    {
        if (IsWindows || IsMacOS)
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            ExtendClientAreaTitleBarHeightHint = -1;
        }
        
        InitializeComponent();
        DataContext = this;
        Initialize();
    }
        
    public bool IsBackEnabled => NavigationService.CanGoBack;
    public bool IsForwardEnabled => NavigationService.CanGoForward;

    private void Initialize()
    {
        NavigationService.TabView = this.FindControl<TabView>("TabView");
        NavigationService.Navigated += (s, e) =>
        {
            FrameChanged();
            if (e.Content is Page page) page.OnNavigatedTo(e);
        };
        NavigationService.NavigationFailed += (s, e) => throw e.Exception;
        NavigationService.TabAdded += UpdateTitle;
        NavigationService.TabRemoved += _ => { if (NavigationService.TabView.TabItems.Count() <= 1) NewTab(); };
        NavigationService.SelectionChanged += (s, e) => FrameChanged();

        //NavigationService.NewTab(typeof(SettingsPage)).IsClosable = false;
        NavigationService.Frame = NewTab().Content as Frame;
    }

    private TabViewItem NewTab() => NavigationService.NewTab(typeof(ProviderList));

    /// <summary>Add a new Tab to the TabView</summary>
    private void AddTab(TabView sender, EventArgs args) => NewTab();
    /// <summary>Remove the requested tab from the TabView</summary>
    private void CloseTab(TabView sender, TabViewTabCloseRequestedEventArgs args) => NavigationService.CloseTab(args.Tab);
    private void BackRequested(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    private void ForwardRequested(object sender, RoutedEventArgs e) => NavigationService.GoForward();

    public new event PropertyChangedEventHandler PropertyChanged;
    private void FrameChanged()
    {
        PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsBackEnabled)));
        PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsForwardEnabled)));
        UpdateSelectedTitle();
    }
    public static void UpdateSelectedTitle() => UpdateTitle(NavigationService.TabView?.SelectedItem as TabViewItem);
    static void UpdateTitle(TabViewItem? tab)
    {
        if (tab is null) return;
        var frame = tab.Content as Frame;
        var frameData = frame?.Content as ITabPage;

        tab.IconSource = frameData is null ? null : new SymbolIconSource { Symbol = frameData.IconSource };
        tab.Header = frameData is null ? frame?.SourcePageType.Name : frameData.DynaTabHeader ?? ResourceExtensions.GetLocalized($"Tab.{frame?.SourcePageType.Name}", ResourceExtensions.Resource.UI);
        tab.Tag = frameData?.Identifier;
        Debug.WriteLine(tab.Tag);
    }

    protected void RegistrySearch_TextChanged(object? sender, EventArgs _) { if(sender is AutoCompleteBox searchBar) searchBar.Items = Search(searchBar.Text); }
    IEnumerable<Result> Search(string query)
    {
        IEnumerable<Result> GetRegistries(Func<Registry, string> contains) => Data.Providers.Values.SelectMany(p => p.Registries.Values
            .Where(r => contains?.Invoke(r)?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .Select(r => new Result { Text = $"{r.Location ?? r.LocationID}: {r.Name}", Value = new RegistryInfo(r) }));

        if (!Uri.TryCreate(query, UriKind.Absolute, out var uri)) return GetRegistries(r => $"{r.Location ?? r.LocationID}: {r.Name}");

        var registries = GetRegistries(r => r.URL).ToList() ?? new List<Result>();
        if (!registries.Any()) 
            foreach (var (key, value) in Data.Providers) 
                if (value.API.TryGetRegistryID(uri, out var _)) 
                    registries.Add(new Result { Text = $"Online Match: {key}", Value = uri });
        return registries;
    }
    private class Result
    {
        public object? Value { get; init; }
        public string? Text { get; init; }
        public override string? ToString() => Text;
    }
    private void RegistrySearch_SuggestionChosen(object? sender, SelectionChangedEventArgs args)
    {
        if (args.AddedItems[0] is Result result) NavigationService.Navigate(typeof(Registry), result.Value);
        if(sender is AutoCompleteBox searchBar) searchBar.Text = string.Empty;
    }
}