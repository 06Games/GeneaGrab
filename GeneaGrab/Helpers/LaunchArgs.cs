using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using GeneaGrab.Services;
using GeneaGrab.Views;
using PowerArgs;

namespace GeneaGrab.Helpers;

[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public class LaunchArgs
{
    [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
    public bool Help { get; set; }

    [ArgShortcut("--custom-scheme")]
    public string? UrlData { get; set; }

    [ArgShortcut("-u")]
    public string? Url { get; set; }

    public void Main()
    {
        if (Uri.TryCreate(UrlData, UriKind.Absolute, out var uri))
        {
            var query = uri.ParseQueryString();
            Url ??= query["url"];
        }

        if (Application.Current is App app && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            if (!app.SingleInstance.IsFirstInstance && !string.IsNullOrWhiteSpace(Url))
            {
                desktopApp.Shutdown();
                return;
            }
        }

        NavigationService.OpenTab(NavigationService.NewTab<RegistryViewer>(this));
    }
}
