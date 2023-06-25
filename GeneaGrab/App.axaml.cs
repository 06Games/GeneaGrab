using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DiscordRPC;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;
using GeneaGrab.Helpers;
using GeneaGrab.Views;
using PowerArgs;
using Serilog;
using Serilog.Formatting.Compact;
using SingleInstance;
using URIScheme;

namespace GeneaGrab
{
    public partial class App : Application
    {
        public ISingleInstanceService SingleInstance { get; private set; } = null!;
        public Version Version { get; } = new(2, 0);
        public DiscordRpcClient Discord { get; } = new ("1120393636455129229");
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(new RenderedCompactJsonFormatter(), Path.Combine(LocalData.LogFolder, $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}.json"))
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
            Data.GetImage = LocalData.GetImage;
            Data.SaveImage = LocalData.SaveImageAsync;
            Data.ToThumbnail = LocalData.ToThumbnailAsync;
            
            
            
            SingleInstance = new SingleInstanceService("GeneaGrab");
            SingleInstance.TryStartSingleInstance();
            if(SingleInstance.IsFirstInstance)
            {
                SingleInstance.StartListenServer();
                Log.Information("This is the first instance");

                SingleInstance.Received.Subscribe(receive => Task.Run(async () =>
                {
                    var (message, response) = receive;
                    var args = await Json.ToObjectAsync<string[]>(message);
                    Dispatcher.UIThread.Post(() => Args.InvokeMain<LaunchArgs>(args));
                    
                    response("success"); // Send response
                }));
            }
            else {
                Log.Information("This is not the first instance");
                if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) 
                    _ = Task.Run(async () => await SingleInstance.SendMessageToFirstInstanceAsync(await Json.StringifyAsync(desktop.Args)));
            }
            
            
            Discord.Initialize(); //Connect to the RPC
            // Refresh Discord RPC every 500 ms
            var timer = new System.Timers.Timer(500); 
            timer.Elapsed += (_, _) => { if (!Discord.IsDisposed) Discord.Invoke(); };
            timer.Start();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.DataContext = desktop.MainWindow;
                desktop.Exit += (_, _) =>
                {
                    Discord.Dispose();
                    SingleInstance.Dispose();
                };
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    const string scheme = @"geneagrab";
                    const string args = "--custom-scheme";
                    var path = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
                    var service = URISchemeServiceFactory.GetURISchemeSerivce(scheme, @$"URL:{scheme} Protocol",
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $@"{path}"" ""{args}" : $@"{path} {args}");
 #if DEBUG
                    if (!service.CheckAny()) // Check if the protocol is registered to any application.
#else
                    if (!service.Check()) // Check if the protocol is registered to the current application.
#endif
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
