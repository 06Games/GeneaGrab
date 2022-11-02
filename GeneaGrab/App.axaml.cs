using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GeneaGrab.Helpers;
using GeneaGrab.Views;
using Serilog;

namespace GeneaGrab
{
    public partial class App : Application
    {

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            
            Log.Logger = new LoggerConfiguration().WriteTo
                .File(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(), Path.Combine(LocalData.LogFolder, $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}.json"))
                .CreateLogger();
            
            Data.Log = (l, d) => { if (d is null) Log.Information(l); else Log.Information(d, l); };
            Data.Warn = (l,d) => { if (d is null) Log.Warning(l); else Log.Warning(d, l); };
            Data.Error = (l,d) => { if (d is null) Log.Error(l); else Log.Error(d, l); };
            Data.Translate = (id, fallback) => ResourceExtensions.GetLocalized(id) ?? fallback;
            Data.GetImage = LocalData.GetImageAsync;
            Data.SaveImage = LocalData.SaveImageAsync;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            LocalData.LoadData();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.DataContext = desktop.MainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
