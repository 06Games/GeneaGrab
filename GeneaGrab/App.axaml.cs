using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GeneaGrab.Helpers;
using GeneaGrab.Views;
using Serilog;
using URIScheme;

namespace GeneaGrab
{
    public partial class App : Application
    {
        public Version Version { get; } = new(2, 0);
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Log.Logger = new LoggerConfiguration().WriteTo
                .File(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(), Path.Combine(LocalData.LogFolder, $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}.json"))
                .CreateLogger();

            Data.Log = (l, d) =>
            {
                if (d is null) Log.Information(l);
                else Log.Information(d, l);
            };
            Data.Warn = (l, d) =>
            {
                if (d is null) Log.Warning(l);
                else Log.Warning(d, l);
            };
            Data.Error = (l, d) =>
            {
                if (d is null) Log.Error(l);
                else Log.Error(d, l);
            };
            Data.Translate = (id, fallback) => ResourceExtensions.GetLocalized(id) ?? fallback;
            Data.GetImage = LocalData.GetImageAsync;
            Data.SaveImage = LocalData.SaveImageAsync;
            Data.ToThumbnail = LocalData.ToThumbnail;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            _ = LocalData.LoadData();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.DataContext = desktop.MainWindow;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    const string scheme = @"geneagrab";
                    const string args = "--custom-scheme";
                    var path = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var service = URISchemeServiceFactory.GetURISchemeSerivce(scheme, @$"URL:{scheme} Protocol",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $@"{path}"" ""{args}" : $@"{path} {args}");
                    if (!service.Check()) // Check if the protocol is registered to the current application.
                        service.Set(); // Register the service.
                }
                catch (PlatformNotSupportedException e)
                {
                    Log.Warning(e, "Couldn't register Uri Scheme");
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
