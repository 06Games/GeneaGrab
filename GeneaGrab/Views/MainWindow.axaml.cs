using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DiscordRPC;
using FluentAvalonia.UI.Controls;
using GeneaGrab.Core.Models;
using GeneaGrab.Helpers;
using GeneaGrab.Services;
using Serilog;

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

    protected string? RegistryText => ResourceExtensions.GetLocalized("Registry.Name");

    public MainWindow()
    {
        if (IsWindows)
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            ExtendClientAreaTitleBarHeightHint = -1;
        }
        else if (IsMacOS) ExtendClientAreaToDecorationsHint = true;

        InitializeComponent();
        DataContext = this;
        Initialize();
    }

    public bool IsBackEnabled => NavigationService.CanGoBack;
    public bool IsForwardEnabled => NavigationService.CanGoForward;

    private void Initialize()
    {
        NavigationService.TabView = TabView;

        Dispatcher.UIThread.Post(() =>
        {
            if (NavigationService.TabView.GetVisualDescendants().FirstOrDefault(d => d.Name == "TabContainerGrid") is not InputElement titlebar) return;
            titlebar.PointerMoved += InputElement_OnPointerMoved;
            titlebar.PointerPressed += InputElement_OnPointerPressed;
            titlebar.PointerReleased += InputElement_OnPointerReleased;
        });
        
        NavigationService.Navigated += (_, e) =>
        {
            FrameChanged();
            if (e.Content is Page page) page.OnNavigatedTo(e);
        };
        NavigationService.NavigationFailed += (_, e) => throw e.Exception;
        NavigationService.TabAdded += UpdateTitle;
        NavigationService.TabRemoved += _ =>
        {
            if (NavigationService.TabCount <= 1) NewTab();
        };
        NavigationService.SelectionChanged += (_, _) => FrameChanged();

        NavigationService.NewTab(typeof(SettingsPage)).IsClosable = false;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Startup += (_, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (NavigationService.TabCount <= 1) NavigationService.OpenTab(NewTab());
                });
            };
        else NavigationService.OpenTab(NewTab());
    }

    private TabViewItem NewTab() => NavigationService.NewTab(typeof(ProviderList));

    /// <summary>Add a new Tab to the TabView</summary>
    private void AddTab(TabView sender, EventArgs args) => NewTab();
    /// <summary>Remove the requested tab from the TabView</summary>
    private void CloseTab(TabView sender, TabViewTabCloseRequestedEventArgs args) => NavigationService.CloseTab(args.Tab);
    private void GoBack(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    private void GoForward(object sender, RoutedEventArgs e) => NavigationService.GoForward();

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void FrameChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBackEnabled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsForwardEnabled)));
        UpdateSelectedTitle();
    }
    public static void UpdateSelectedTitle() => UpdateTitle(NavigationService.TabView?.SelectedItem as TabViewItem);
    private static void UpdateTitle(TabViewItem? tab)
    {
        if (tab is null) return;
        var frame = tab.Content as Frame;
        var frameData = frame?.Content as ITabPage;
        var defaultName = ResourceExtensions.GetLocalized($"Tab.{frame?.SourcePageType?.Name}", ResourceExtensions.Resource.UI) ?? frame?.SourcePageType?.Name;

        tab.IconSource = frameData is null ? null : new SymbolIconSource { Symbol = frameData.IconSource };
        tab.Header = frameData?.DynaTabHeader ?? defaultName;
        tab.Tag = frameData?.Identifier;
        Debug.WriteLine(tab.Tag);

        try
        {
            (Application.Current as App)?.Discord.SetPresence(new RichPresence
            {
                Details = defaultName?[..Math.Min(defaultName.Length, 96)],
                State = frameData?.DynaTabHeader?[..Math.Min(frameData.DynaTabHeader.Length, 96)],
                Assets = new Assets
                {
                    LargeImageKey = "logo",
                    SmallImageKey = frameData is null ? null : Enum.GetName(frameData.IconSource)?.ToLower()
                }
            });
        }
        catch(Exception e) { Log.Warning(e, "Couldn't update Discord RPC"); }
    }

    
    #region Search

    protected void RegistrySearch_TextChanged(object? sender, TextChangedEventArgs _)
    {
        if (sender is not AutoCompleteBox searchBar) return;
        if (searchBar.Text != searchBar.SelectedItem?.ToString() && !string.IsNullOrWhiteSpace(searchBar.Text)) searchBar.ItemsSource = Search(searchBar.Text);
        searchBar.FilterMode = AutoCompleteFilterMode.None;
    }
    private IEnumerable<Result> Search(string query)
    {
        IEnumerable<Result> GetRegistries(Func<Registry, string?>? contains) => Data.Providers.Values.SelectMany(p => p.Registries.Values
            .Where(r => contains?.Invoke(r)?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
            .Select(r => new Result { Text = $"{r.Location ?? r.LocationID}: {r.Name}", Value = new RegistryInfo(r) }));

        if (!Uri.TryCreate(query, UriKind.Absolute, out var uri)) return GetRegistries(r => $"{r.Location ?? r.LocationID}: {r.Name}");

        var registries = GetRegistries(r => r.URL).ToList();
        if (!registries.Any())
            foreach (var (key, value) in Data.Providers)
                if (value.Api.TryGetRegistryID(uri, out var _))
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
        if (args.AddedItems.Count == 0) return;
        if (args.AddedItems[0] is Result result) NavigationService.Navigate(typeof(RegistryViewer), result.Value);
        if (sender is AutoCompleteBox searchBar) searchBar.Text = string.Empty;
    }

    #endregion


    #region MoveWindow

    private bool _mouseDownForWindowMoving;
    private PointerPoint _originalPoint;

    private void InputElement_OnPointerMoved(object? _, PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving) return;

        var currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X), Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
    }

    private void InputElement_OnPointerPressed(object? _, PointerEventArgs e)
    {
        if (WindowState is WindowState.Maximized or WindowState.FullScreen) return;

        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }

    private void InputElement_OnPointerReleased(object? _, PointerReleasedEventArgs e) => _mouseDownForWindowMoving = false;

    #endregion
}
