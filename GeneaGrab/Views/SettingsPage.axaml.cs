using System;
using GeneaGrab.Helpers;
using GeneaGrab.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;

namespace GeneaGrab.Views
{
    public partial class SettingsPage : Page, INotifyPropertyChanged, ITabPage
    {
        public Symbol IconSource => Symbol.Setting;
        public string DynaTabHeader => null;
        public string Identifier => null;

        
        public string? Personalization => ResourceExtensions.GetLocalized("Settings.Personalization", ResourceExtensions.Resource.UI);
        public string? Theme => ResourceExtensions.GetLocalized("Settings.Theme", ResourceExtensions.Resource.UI);
        public string? LightTheme => ResourceExtensions.GetLocalized("Settings.Theme.Light", ResourceExtensions.Resource.UI);
        public string? DarkTheme => ResourceExtensions.GetLocalized("Settings.Theme.Dark", ResourceExtensions.Resource.UI);
        public string? HighContrastTheme => ResourceExtensions.GetLocalized("Settings.Theme.HighContrast", ResourceExtensions.Resource.UI);
        public string? SystemTheme => ResourceExtensions.GetLocalized("Settings.Theme.System", ResourceExtensions.Resource.UI);
        public string? About => ResourceExtensions.GetLocalized("Settings.About", ResourceExtensions.Resource.UI);
        public string? AboutDescription => ResourceExtensions.GetLocalized("Settings.About.Description", ResourceExtensions.Resource.UI);
        public string? AboutSourceCode => ResourceExtensions.GetLocalized("Settings.About.SourceCode", ResourceExtensions.Resource.UI);


        public Theme ElementTheme => ThemeSelectorService.Theme;

        private string? _versionDescription;
        public string? VersionDescription
        {
            get => _versionDescription;
            set => Set(ref _versionDescription, value);
        }

        public SettingsPage() => InitializeComponent();
        private void InitializeComponent()
        {
            ThemeSelectorService.Initialize();
            VersionDescription = GetVersionDescription();
            
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }

        private string? GetVersionDescription()
        {
            var appName = ResourceExtensions.GetLocalized("AppDisplayName", ResourceExtensions.Resource.UI);
            if (Application.Current is not App package) return null;
            var version = package.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}.{Math.Max(version.Revision, 0)}";
        }

        private void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;
            if (param != null) ThemeSelectorService.SetTheme((Theme)param);
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            OnPropertyChanged(propertyName);
        }
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
