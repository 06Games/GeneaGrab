using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Button = Avalonia.Controls.Button;

namespace GeneaGrab.Views;

public partial class WindowsTitleBar : UserControl
{
    private readonly SymbolIcon? maximizeIcon;
    private readonly ToolTip? maximizeToolTip;

    public WindowsTitleBar()
    {
        InitializeComponent();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            IsVisible = false;
            return;
        }

        var minimizeButton = this.FindControl<Button>("MinimizeButton");
        var maximizeButton = this.FindControl<Button>("MaximizeButton");
        maximizeIcon = this.FindControl<SymbolIcon>("MaximizeIcon");
        maximizeToolTip = this.FindControl<ToolTip>("MaximizeToolTip");
        var closeButton = this.FindControl<Button>("CloseButton");

        minimizeButton.Click += MinimizeWindow;
        maximizeButton.Click += MaximizeWindow;
        closeButton.Click += CloseWindow;

        this.FindControl<DockPanel>("TitleBar");
        this.FindControl<DockPanel>("TitleBarBackground");

        SubscribeToWindowState();
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

    private async void SubscribeToWindowState()
    {
        var hostWindow = VisualRoot as Window;

        while (hostWindow == null)
        {
            hostWindow = VisualRoot as Window;
            await Task.Delay(50);
        }

        hostWindow.GetObservable(Window.WindowStateProperty).Subscribe(s =>
        {
            if (s != WindowState.Maximized)
            {
                if (maximizeIcon != null) maximizeIcon.Symbol = Symbol.FullScreenMaximize;
                hostWindow.Padding = new Thickness(0,0,0,0);
                if (maximizeToolTip != null) maximizeToolTip.Content = "Maximize";
            }
            else
            {
                if (maximizeIcon != null) maximizeIcon.Symbol = Symbol.FullScreenMinimize;
                hostWindow.Padding = new Thickness(7, 7, 7, 7);
                if (maximizeToolTip != null) maximizeToolTip.Content = "Restore Down";
            }
        });
    }
}