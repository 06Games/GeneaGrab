using GeneaGrab.Helpers;
using GeneaGrab.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace GeneaGrab.Views
{
    public interface TabPage
    {
        Symbol IconSource { get; }
        string DynaTabHeader { get; }
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

        static WinUI.TabView TabView;
        public ShellPage()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();
            TabView = tabView;
        }

        private void Initialize()
        {
            NewTab(tabView, typeof(SettingsPage)).IsClosable = false;
            NavigationService.Frame = NewTab(tabView).Content as Frame;
            tabView.SelectionChanged += (s, e) => FrameChanged();


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

        /// <summary>Add a new Tab to the TabView</summary>
        private void AddTab(WinUI.TabView sender, object args) => NewTab(sender);
        private WinUI.TabViewItem NewTab(WinUI.TabView view) => NewTab(view, typeof(MainPage));
        private WinUI.TabViewItem NewTab(WinUI.TabView view, Type page)
        {
            Frame frame = new Frame();
            var newTab = new WinUI.TabViewItem { Content = frame };
            frame.Navigate(page);
            frame.Navigated += (s,e) => FrameChanged();
            frame.NavigationFailed += (s, e) => throw e.Exception;

            view.TabItems.Add(newTab);
            UpdateTitle(newTab);
            view.SelectedItem = newTab;
            return newTab;
        }
        /// <summary>Remove the requested tab from the TabView</summary>
        private void CloseTab(WinUI.TabView sender, WinUI.TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
            if (sender.TabItems.Count <= 1) NewTab(sender);
        }

        private void FrameChanged()
        {
            if (!(tabView.SelectedItem is WinUI.TabViewItem tab)) return;
            var frame = NavigationService.Frame = tab.Content as Frame;
            IsBackEnabled = frame.CanGoBack;
            IsForwardEnabled = frame.CanGoForward;
            UpdateTitle(tab);
        }
        public static void UpdateSelectedTitle() => UpdateTitle(TabView.SelectedItem as WinUI.TabViewItem);
        static void UpdateTitle(WinUI.TabViewItem tab)
        {
            var frame = tab.Content as Frame;
            var frameData = frame.Content as TabPage;

            tab.IconSource = frameData is null ? null : new WinUI.SymbolIconSource { Symbol = frameData.IconSource };
            tab.Header = frameData is null ? frame.SourcePageType.Name : frameData.DynaTabHeader ?? ResourceExtensions.GetLocalized($"Shell/{frame.SourcePageType.Name}");
        }



        private void BackRequested(object sender, RoutedEventArgs e)
        {
            var frame = (tabView.SelectedItem as WinUI.TabViewItem).Content as Frame;
            if (frame.CanGoBack) frame.GoBack();
        }
        private void ForwardRequested(object sender, RoutedEventArgs e)
        {
            var frame = (tabView.SelectedItem as WinUI.TabViewItem).Content as Frame;
            if (frame.CanGoForward) frame.GoForward();
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
