using GeneaGrab.Helpers;
using GeneaGrab.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GeneaGrab.Views
{
    // https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/UWP/pages/settings-codebehind.md
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged, ITabPage
    {
        public Symbol IconSource => Symbol.Setting;
        public string DynaTabHeader => null;
        public string Identifier => null;

        private ElementTheme _elementTheme = ThemeSelectorService.Theme;
        public ElementTheme ElementTheme
        {
            get => _elementTheme;
            set => Set(ref _elementTheme, value);
        }

        private string _versionDescription;
        public string VersionDescription
        {
            get => _versionDescription;
            set => Set(ref _versionDescription, value);
        }

        private string _geneanetUsername;
        public string GeneanetUsername { get => _geneanetUsername; set => Set(ref _geneanetUsername, value); }
        private string _geneanetPassword;
        public string GeneanetPassword { get => _geneanetPassword; set => Set(ref _geneanetPassword, value); }
        private string _familysearchUsername;
        public string FamilysearchUsername { get => _familysearchUsername; set => Set(ref _familysearchUsername, value); }
        private string _familysearchPassword;
        public string FamilysearchPassword { get => _familysearchPassword; set => Set(ref _familysearchPassword, value); }

        public SettingsPage() => InitializeComponent();
        protected override async void OnNavigatedTo(NavigationEventArgs e) => await InitializeAsync();
        private async Task InitializeAsync()
        {
            VersionDescription = GetVersionDescription();
            (GeneanetUsername, GeneanetPassword) = await GetAccountInfo("Geneanet");
            (FamilysearchUsername, FamilysearchPassword) = await GetAccountInfo("FamilySearch");
        }

        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;
            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async Task<(string username, string password)> GetAccountInfo(string provider)
            => (await ApplicationData.Current.LocalSettings.ReadAsync<string>($"{provider}.Username"), await ApplicationData.Current.LocalSettings.ReadAsync<string>($"{provider}.Password"));
        private async void AccountInfoChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox text)
                await ApplicationData.Current.LocalSettings.SaveAsync(text.Name.Replace('_', '.'), text.Text);
            else if (sender is PasswordBox pass)
                await ApplicationData.Current.LocalSettings.SaveAsync(pass.Name.Replace('_', '.'), pass.Password);
        }
        private async Task SetAccountInfo(string provider, string username, string password)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync($"{provider}.Username", username);
            await ApplicationData.Current.LocalSettings.SaveAsync($"{provider}.Password", password);
        }

        private async void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;
            if (param != null) await ThemeSelectorService.SetThemeAsync((ElementTheme)param);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            OnPropertyChanged(propertyName);
        }
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
