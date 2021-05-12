using GeneaGrab.Helpers;
using GeneaGrab.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace GeneaGrab.Views
{
    // TODO WTS: Change the icons and titles for all NavigationViewItems in ShellPage.xaml.
    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
        private readonly KeyboardAccelerator _backKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);

        private WinUI.NavigationViewItem _selected;
        private bool _isBackEnabled;
        public bool IsBackEnabled
        {
            get => _isBackEnabled;
            set => Set(ref _isBackEnabled, value);
        }
        private bool _isRegistryLoaded;
        public bool IsRegistryLoaded
        {
            get => _isRegistryLoaded;
            set => Set(ref _isRegistryLoaded, value);
        }

        private string RegistryText => ResourceExtensions.GetLocalized(Resource.Core, "Registry/Name");

        public WinUI.NavigationViewItem Selected
        {
            get => _selected;
            set => Set(ref _selected, value);
        }

        public ShellPage()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();
        }

        private void Initialize()
        {
            NavigationService.Frame = shellFrame;
            NavigationService.NavigationFailed += Frame_NavigationFailed;
            NavigationService.Navigated += Frame_Navigated;
            navigationView.BackRequested += OnBackRequested;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            KeyboardAccelerators.Add(_backKeyboardAccelerator);
            await Task.CompletedTask;
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e) => throw e.Exception;

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            IsBackEnabled = NavigationService.CanGoBack;
            IsRegistryLoaded = Registry.Info != null;

            if (e.SourcePageType == typeof(SettingsPage))
            {
                Selected = navigationView.SettingsItem as WinUI.NavigationViewItem;
                return;
            }

            var selectedItem = GetSelectedItem(navigationView.MenuItems, e.SourcePageType);
            if (selectedItem != null) Selected = selectedItem;

        }

        private WinUI.NavigationViewItem GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
        {
            foreach (var item in menuItems.OfType<WinUI.NavigationViewItem>())
            {
                if (IsMenuItemFoPageType(item, pageType)) return item;
                var selectedChild = GetSelectedItem(item.MenuItems, pageType);
                if (selectedChild != null) return selectedChild;
            }

            return null;
        }

        private bool IsMenuItemFoPageType(WinUI.NavigationViewItem menuItem, Type sourcePageType)
        {
            var pageType = menuItem.GetValue(NavHelper.NavigateToProperty) as Type;
            return pageType == sourcePageType;
        }

        private void OnItemInvoked(WinUI.NavigationView sender, WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked) NavigationService.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            else if (args.InvokedItemContainer is WinUI.NavigationViewItem selectedItem)
            {
                var pageType = selectedItem.GetValue(NavHelper.NavigateToProperty) as Type;
                NavigationService.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnBackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args) => NavigationService.GoBack();

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator { Key = key };
            if (modifiers.HasValue) keyboardAccelerator.Modifiers = modifiers.Value;
            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
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
            IEnumerable<Result> GetRegistries(Func<GeneaGrab.Registry, string> contains)
                => Data.Providers.Values.SelectMany(p => p.Registries.Values).Where(r => contains?.Invoke(r)?.Contains(query, StringComparison.InvariantCultureIgnoreCase) ?? false).Select(r => new Result { Text = r.Name, Value = new RegistryInfo(r) });

            if (!Uri.TryCreate(query, UriKind.Absolute, out var uri)) return GetRegistries(r => r.Name);

            var registries = GetRegistries(r => r.URL).ToList() ?? new List<Result>();
            if (!registries.Any()) foreach (var provider in Data.Providers) if (provider.Value.API.CheckURL(uri)) registries.Add(new Result { Text = $"Online Match: {provider.Key}", Value = uri });
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
            var result = args.SelectedItem as Result;
            if (result != null) NavigationService.Navigate(typeof(Registry), result.Value);
            sender.Text = string.Empty;
        }
    }
}
