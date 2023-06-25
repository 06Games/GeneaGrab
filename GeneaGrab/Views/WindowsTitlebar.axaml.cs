using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;

namespace GeneaGrab.Views;

public partial class WindowsTitleBar : UserControl
{

    public WindowsTitleBar()
    {
        InitializeComponent();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            IsVisible = false;
            return;
        }

        MinimizeButton.Click += MinimizeWindow;
        MaximizeButton.Click += MaximizeWindow;
        CloseButton.Click += CloseWindow;

        _ = SubscribeToWindowStateAsync();
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is Window hostWindow) hostWindow.Close();
    }

    private void MaximizeWindow(object? sender, RoutedEventArgs e)
    {
        if(VisualRoot is not Window hostWindow) return;
        hostWindow.WindowState = hostWindow.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is Window hostWindow) hostWindow.WindowState = WindowState.Minimized;
    }

    private async Task SubscribeToWindowStateAsync()
    {
        Window? hostWindow;
        while ((hostWindow = VisualRoot as Window) == null) await Task.Delay(50);

        hostWindow.GetObservable(Window.WindowStateProperty).Subscribe(s =>
        {
            if (s != WindowState.Maximized)
            {
                if (MaximizeIcon != null) MaximizeIcon.Symbol = Symbol.FullScreenMaximize;
                hostWindow.Padding = new Thickness(0,0,0,0);
                if (MaximizeToolTip != null) MaximizeToolTip.Content = "Maximize";
            }
            else
            {
                if (MaximizeIcon != null) MaximizeIcon.Symbol = Symbol.FullScreenMinimize;
                hostWindow.Padding = new Thickness(7, 7, 7, 7);
                if (MaximizeToolTip != null) MaximizeToolTip.Content = "Restore Down";
            }
        });
    }
}