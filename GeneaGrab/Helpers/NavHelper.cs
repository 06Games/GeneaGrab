using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml;

namespace GeneaGrab.Helpers
{
    /// <summary>This helper class allows to specify the page that will be shown when you click on a NavigationViewItem</summary>
    /// <example>NavHelper.SetNavigateTo(navigationViewItem, typeof(MainPage));</example>
    // Usage in xaml: <winui:NavigationViewItem x:Uid="Shell_Main" Icon="Document" helpers:NavHelper.NavigateTo="views:MainPage" />
    public static class NavHelper
    {
        public static Type GetNavigateTo(NavigationViewItem item) => (Type)item.GetValue(NavigateToProperty);
        public static void SetNavigateTo(NavigationViewItem item, Type value) => item.SetValue(NavigateToProperty, value);
        public static readonly DependencyProperty NavigateToProperty = DependencyProperty.RegisterAttached("NavigateTo", typeof(Type), typeof(NavHelper), new PropertyMetadata(null));
    }
}
