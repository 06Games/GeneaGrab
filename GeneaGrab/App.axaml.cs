using System;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using DiscordRPC;
using GeneaGrab.Core.Models;
using GeneaGrab.Helpers;
using GeneaGrab.Services;
using GeneaGrab.Views;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Compact;
using SingleInstance;
using URIScheme;

// IApplicationPlatformEvents.RaiseUrlsOpened is not obsolete, it's just an unstable API
#pragma warning disable CS0618

namespace GeneaGrab;

public partial class App : Application
{
    public DiscordRpcClient Discord { get; } = new("1120393636455129229");

    public override void Initialize()
    {
        using (var client = new DatabaseContext())
            client.Database.Migrate();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(new RenderedCompactJsonFormatter(), Path.Combine(LocalData.LogFolder, $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}.ndjson"))
            .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Area} {Source}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        Data.SetLogger(Log.Logger);
        Logger.Sink = new SerilogSink();

        Data.GetImage = LocalData.GetImage;
        Data.SaveImage = LocalData.SaveImageAsync;
        Data.ToThumbnail = LocalData.ToThumbnailAsync;

        if (Current?.TryGetFeature<IActivatableLifetime>() is { } activationFeature)
            activationFeature.Activated += (_, a) => OnActivation(a);

        var desktop = ApplicationLifetime as ClassicDesktopStyleApplicationLifetime;
        using var singleInstance = new SingleInstanceService("GeneaGrab");
        singleInstance.TryStartSingleInstance();
        if (singleInstance.IsFirstInstance) RegisterFirstInstance(singleInstance);
        else if (SendToFirstInstance(singleInstance, desktop?.Args))
            return;
        AvaloniaXamlLoader.Load(this);

        if (TryExtractUriFromArgs(desktop?.Args, out var uri) && uri != null) OnActivation(new ProtocolActivatedEventArgs(uri));

        Discord.Initialize(); //Connect to the RPC
        // Refresh Discord RPC every 5s
        var timer = new Timer(5000);
        timer.Elapsed += (_, _) =>
        {
            if (!Discord.IsDisposed) Discord.Invoke();
            else timer.Stop();
        };
        timer.Start();

        if (desktop != null) desktop.MainWindow = new MainWindow();
        try { desktop?.Start(desktop.Args ?? []); }
        catch (Exception e) { Log.Fatal(e, "A fatal error occured"); }
        OnExit();
    }

    private static bool TryExtractUriFromArgs(string[]? args, out Uri? url)
    {
        if (args?.Length == 1) return Uri.TryCreate(args[0], UriKind.RelativeOrAbsolute, out url);
        url = null;
        return false;
    }

    private static void RegisterFirstInstance(SingleInstanceService singleInstance)
    {
        singleInstance.StartListenServer();
        Log.Information("This is the first instance");
        singleInstance.Received.SelectMany(ServerResponseAsync).Subscribe();

        Task<Unit> ServerResponseAsync((string, Action<string>) receive) => Task.Run(() =>
        {
            var (message, response) = receive;
            Log.Information("Received message: {Message}", message);
            var args = JsonConvert.DeserializeObject<string[]>(message);
            if (TryExtractUriFromArgs(args, out var uri) && uri != null) OnActivation(new ProtocolActivatedEventArgs(uri));
            else OnActivation(new ActivatedEventArgs(ActivationKind.Reopen));
            response("success"); // Send response
            return Unit.Default;
        });
    }

    private static bool SendToFirstInstance(SingleInstanceService singleInstance, string[]? args)
    {
        Log.Information("This is not the first instance");
        var task = Task.Run(async () =>
        {
            try
            {
                var msg = JsonConvert.SerializeObject(args);
                var response = await singleInstance.SendMessageToFirstInstanceAsync(msg);
                if (response != "success") throw new IOException(response);
                Log.Information("Sent message to first instance: {Message}", msg);
                return true;
            }
            catch (Exception e) { Log.Error(e, "Communication error with first instance"); }
            return false;
        });

#pragma warning disable VSTHRD002
        return task.GetAwaiter().GetResult(); // This instance is not intended to be displayed, so it's not really a problem if the main thread freezes for a while
#pragma warning restore
    }

    private static void OnActivation(ActivatedEventArgs activatedEvent)
    {
        if (activatedEvent is not ProtocolActivatedEventArgs { Kind: ActivationKind.OpenUri } protocol) return;

        Log.Information("Opened by Url: {Urls}", protocol.Uri);
        var uri = protocol.Uri;
        if (uri.Scheme == "geneagrab")
        {
            var query = uri.ParseQueryString();
            if (!Uri.TryCreate(query["url"], UriKind.Absolute, out uri))
            {
                Log.Error("Invalid URI");
                return;
            }
        }
        Dispatcher.UIThread.Post(() => NavigationService.OpenTab(NavigationService.NewTab<RegistryViewer>(uri)));
    }

    private static void RegisterScheme()
    {
        const string scheme = @"geneagrab";
        var path = Environment.ProcessPath;
        if (path is null) return;
        var service = URISchemeServiceFactory.GetURISchemeSerivce(scheme, @$"URL:{scheme} Protocol", path);
#if DEBUG
        if (!service.CheckAny()) // Check if the protocol is registered to any application.
#else
        if (!service.Check()) // Check if the protocol is registered to the current application.
#endif
            service.Set(); // Register the service.
    }

    private void OnExit()
    {
        Discord.Dispose();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // On macOS this is done by the Info.plist file
            try { RegisterScheme(); }
            catch (PlatformNotSupportedException e) { Log.Warning(e, "Couldn't register Uri Scheme"); }

        base.OnFrameworkInitializationCompleted();
    }
}
