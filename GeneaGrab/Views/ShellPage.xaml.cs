using GeneaGrab.Helpers;
using GeneaGrab.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace GeneaGrab.Views
{
    public interface ITabPage
    {
        Symbol IconSource { get; }
        string DynaTabHeader { get; }
        string Identifier { get; }
    }

    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
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
        private string RegistryText => ResourceExtensions.GetLocalized(Resource.Core, "Registry/Name");

        public ShellPage()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();
        }

        private void Initialize()
        {
            NavigationService.TabView = tabView;
            NavigationService.Navigated += (s, e) => FrameChanged();
            NavigationService.NavigationFailed += (s, e) => throw e.Exception;
            NavigationService.TabAdded += (tab) => UpdateTitle(tab);
            NavigationService.TabRemoved += (tab) => { if (NavigationService.TabView.TabItems.Count <= 1) NewTab(); };
            NavigationService.SelectionChanged += (s, e) => FrameChanged();

            NavigationService.NewTab(typeof(SettingsPage)).IsClosable = false;
            NavigationService.Frame = NewTab().Content as Frame;


            var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += (sender, _) =>
            {
                CustomDragRegion.MinWidth = FlowDirection == FlowDirection.LeftToRight ? sender.SystemOverlayRightInset : sender.SystemOverlayLeftInset;
                ShellTitlebarInset.MinWidth = FlowDirection == FlowDirection.LeftToRight ? sender.SystemOverlayLeftInset : sender.SystemOverlayRightInset;
                CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
            };
            Window.Current.SetTitleBar(CustomDragRegion);
        }

        private WinUI.TabViewItem NewTab() => NavigationService.NewTab(typeof(MainPage));

        /// <summary>Add a new Tab to the TabView</summary>
        private void AddTab(WinUI.TabView sender, object args) => NewTab();
        /// <summary>Remove the requested tab from the TabView</summary>
        private void CloseTab(WinUI.TabView sender, WinUI.TabViewTabCloseRequestedEventArgs args) => NavigationService.CloseTab(args.Tab);
        private void BackRequested(object sender, RoutedEventArgs e) => NavigationService.GoBack();
        private void ForwardRequested(object sender, RoutedEventArgs e) => NavigationService.GoForward();

        private void FrameChanged()
        {
            IsBackEnabled = NavigationService.CanGoBack;
            IsForwardEnabled = NavigationService.CanGoForward;
            UpdateSelectedTitle();
        }
        public static void UpdateSelectedTitle() => UpdateTitle(NavigationService.TabView.SelectedItem as WinUI.TabViewItem);
        static void UpdateTitle(WinUI.TabViewItem tab)
        {
            if (tab is null) return;
            var frame = tab.Content as Frame;
            var frameData = frame.Content as ITabPage;

            tab.IconSource = frameData is null ? null : new WinUI.SymbolIconSource { Symbol = frameData.IconSource };
            tab.Header = frameData is null ? frame.SourcePageType.Name : frameData.DynaTabHeader ?? ResourceExtensions.GetLocalized($"Shell/{frame.SourcePageType.Name}");
            tab.Tag = frameData.Identifier;
            System.Diagnostics.Debug.WriteLine(tab.Tag);
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            OnPropertyChanged(propertyName);
        }
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));



        private void RegistrySearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) { if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) sender.ItemsSource = Search(sender.Text); }
        IEnumerable<Result> Search(string query)
        {
            IEnumerable<Result> GetRegistries(Func<Location, GeneaGrab.Registry, string> contains) => Data.Providers.Values.SelectMany(p => p.Locations.Values).SelectMany(l => l.Registers
                .Where(r => contains?.Invoke(l, r)?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false)
                .Select(r => new Result { Text = $"{l.Name}: {r.Name}", Value = new RegistryInfo(r) }));

            if (!Uri.TryCreate(query, UriKind.Absolute, out var uri)) return GetRegistries((l, r) => $"{l.Name}: {r.Name}");

            var registries = GetRegistries((l, r) => r.URL).ToList() ?? new List<Result>();
            if (!registries.Any()) foreach (var provider in Data.Providers) if (provider.Value.API.TryGetRegistryID(uri, out var _)) registries.Add(new Result { Text = $"Online Match: {provider.Key}", Value = uri });
            return registries;
        }
        class Result
        {
            public object Value { get; set; }
            public string Text { get; set; }
            public override string ToString() => Text;
        }
        private void RegistrySearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is Result result) NavigationService.Navigate(typeof(Registry), result.Value);
            sender.Text = string.Empty;
        }
    }
}
