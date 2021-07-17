﻿using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace GeneaGrab.Services
{
    public static class NavigationService
    {
        public static event SelectionChangedEventHandler SelectionChanged;
        public static event Action<TabViewItem> TabAdded;
        public static event Action<TabViewItem> TabRemoved;

        public static event NavigatedEventHandler Navigated;
        public static event NavigationFailedEventHandler NavigationFailed;

        private static TabView _tabView;
        private static Frame _frame;
        private static object _lastParamUsed;

        public static TabView TabView
        {
            get => _tabView;
            set
            {
                UnregisterTabViewEvents();
                _tabView = value;
                RegisterTabViewEvents();
            }
        }
        static void RegisterTabViewEvents()
        {
            if (_tabView is null) return;
            _tabView.SelectionChanged += TabView_SelectionChanged;
        }
        static void UnregisterTabViewEvents()
        {
            if (_tabView is null) return;
            _tabView.SelectionChanged -= TabView_SelectionChanged;
        }
        static void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabOpenned();
            SelectionChanged?.Invoke(sender, e);
        }
        static void TabOpenned() { if (TabView.SelectedItem is TabViewItem tab) Frame = tab.Content as Frame; }


        public static TabViewItem NewTab(Type page, object parameter = null)
        {
            Frame frame = new Frame();
            var tab = new TabViewItem { Content = frame, Header = page.Name };
            frame.Navigate(page, parameter);

            TabView.TabItems.Add(tab);
            TabView.SelectedItem = tab;
            Frame = frame;
            TabAdded?.Invoke(tab);
            return tab;
        }
        public static bool CloseTab(TabViewItem tab)
        {
            if (!tab.IsClosable) return false;
            TabView.TabItems.Remove(tab);
            TabRemoved?.Invoke(tab);
            return true;
        }
        public static TabViewItem OpenTab(TabViewItem tab) { TabView.SelectedItem = tab; TabOpenned(); return tab; }
        public static bool TryGetTabWithId(string id, out TabViewItem tab)
        {
            tab = null;
            if (id is null) return false;

            foreach (var t in TabView.TabItems)
                if (t is TabViewItem _tab && _tab.Tag as string == id) { tab = _tab; return true; }
            return false;
        }



        public static Frame Frame
        {
            get => _frame;
            set
            {
                UnregisterFrameEvents();
                _frame = value;
                RegisterFrameEvents();
            }
        }
        private static void RegisterFrameEvents()
        {
            if (_frame == null) return;
            _frame.Navigated += Frame_Navigated;
            _frame.NavigationFailed += Frame_NavigationFailed;
        }
        private static void UnregisterFrameEvents()
        {
            if (_frame == null) return;
            _frame.Navigated -= Frame_Navigated;
            _frame.NavigationFailed -= Frame_NavigationFailed;
        }
        private static void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e) => NavigationFailed?.Invoke(sender, e);
        private static void Frame_Navigated(object sender, NavigationEventArgs e) => Navigated?.Invoke(sender, e);


        public static bool Navigate<T>(object parameter = null, NavigationTransitionInfo infoOverride = null) where T : Page => Navigate(typeof(T), parameter, infoOverride);
        public static bool Navigate(Type pageType, object parameter = null, NavigationTransitionInfo infoOverride = null)
        {
            if (pageType == null || !pageType.IsSubclassOf(typeof(Page))) throw new ArgumentException($"Invalid pageType '{pageType}', please provide a valid pageType.", nameof(pageType));

            // Don't open the same page multiple times
            if (Frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParamUsed)))
            {
                var navigationResult = Frame.Navigate(pageType, parameter, infoOverride);
                if (navigationResult) _lastParamUsed = parameter;
                return navigationResult;
            }
            else return false;
        }

        public static bool CanGoBack => Frame.CanGoBack;
        public static bool GoBack()
        {
            if (!CanGoBack) return false;
            Frame.GoBack();
            return true;
        }
        public static bool CanGoForward => Frame.CanGoForward;
        public static bool GoForward()
        {
            if (!CanGoForward) return false;
            Frame.GoForward();
            return true;
        }
    }
}
