using System;
using Avalonia;
using FluentAvalonia.Styling;

namespace GeneaGrab.Services
{
    public enum Theme { System, Light, Dark, HighContrast }
    public static class ThemeSelectorService
    {
        public static Theme Theme { get; private set; } = Theme.System;

        public static void Initialize()
        {
            Theme = SettingsService.SettingsData.Theme;
            SetRequestedTheme();
        }

        public static void SetTheme(Theme theme)
        {
            Theme = theme;

            SetRequestedTheme();
            SaveThemeInSettings(Theme);
        }

        public static void SetRequestedTheme()
        {
            var faTheme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
            if(faTheme == null) return;

            faTheme.PreferSystemTheme = Theme == Theme.System;
            faTheme.RequestedTheme = Theme == Theme.System ? null : Enum.GetName(Theme);
            if(Theme == Theme.System) faTheme.InvalidateThemingFromSystemThemeChanged();
        }

        private static void SaveThemeInSettings(Theme theme) => SettingsService.SettingsData.Theme = theme;
    }
}
