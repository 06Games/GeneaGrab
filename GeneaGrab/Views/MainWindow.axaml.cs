using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
    
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Initialize();
    }
        
    private bool _isBackEnabled;
    public bool IsBackEnabled
    {
        get => _isBackEnabled;
        set => Set(ref _isBackEnabled, value);
    }
    private bool _isForwardEnabled;
    public bool IsForwardEnabled
    {
        get => _isForwardEnabled;
        set => Set(ref _isForwardEnabled, value);
    }

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


        /*var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
        coreTitleBar.ExtendViewIntoTitleBar = true;
        coreTitleBar.LayoutMetricsChanged += (sender, _) =>
        {
            CustomDragRegion.MinWidth = FlowDirection == FlowDirection.LeftToRight ? sender.SystemOverlayRightInset : sender.SystemOverlayLeftInset;
            ShellTitlebarInset.MinWidth = FlowDirection == FlowDirection.LeftToRight ? sender.SystemOverlayLeftInset : sender.SystemOverlayRightInset;
            CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
        };
        Window.Current.SetTitleBar(CustomDragRegion);*/
    }

    private TabViewItem NewTab() => NavigationService.NewTab(typeof(ProviderList));

    /// <summary>Add a new Tab to the TabView</summary>
    private void AddTab(TabView sender, EventArgs args) => NewTab();
    /// <summary>Remove the requested tab from the TabView</summary>
    private void CloseTab(TabView sender, TabViewTabCloseRequestedEventArgs args) => NavigationService.CloseTab(args.Tab);
    private void BackRequested(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    private void ForwardRequested(object sender, RoutedEventArgs e) => NavigationService.GoForward();

    private void FrameChanged()
    {
        IsBackEnabled = NavigationService.CanGoBack;
        IsForwardEnabled = NavigationService.CanGoForward;
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
        System.Diagnostics.Debug.WriteLine(tab.Tag);
    }



    private void Set<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return;
        storage = value;
    }

    protected void RegistrySearch_TextChanged(object? sender, EventArgs _) { if(sender is AutoCompleteBox searchBar) searchBar.Items = Search(searchBar.Text); }
    IEnumerable<Result> Search(string query)
    {
        IEnumerable<Result> GetRegistries(Func<GeneaGrab.Registry, string> contains) => Data.Providers.Values.SelectMany(p => p.Registries.Values
            .Where(r => contains?.Invoke(r)?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .Select(r => new Result { Text = $"{r.Location ?? r.LocationID}: {r.Name}", Value = new RegistryInfo(r) }));

        if (!Uri.TryCreate(query, UriKind.Absolute, out var uri)) return GetRegistries((r) => $"{r.Location ?? r.LocationID}: {r.Name}");

        var registries = GetRegistries((r) => r.URL).ToList() ?? new List<Result>();
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