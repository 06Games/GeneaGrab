using GeneaGrab.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace GeneaGrab.Views
{
    public sealed partial class Registry : Page, INotifyPropertyChanged
    {
        private string DownloadText => ResourceExtensions.GetLocalized(Resource.Res, "Registry/Download");
        private string OpenFolderText => ResourceExtensions.GetLocalized(Resource.Res, "Registry/OpenFolder");

        public Registry()
        {
            InitializeComponent();
            PageNumber.ValueChanged += async (ns, ne) =>
            {
                if (ne.NewValue <= Pages.Count) await ChangePage(Pages[(int)ne.NewValue - 1]);
            };
        }

        public static RegistryInfo Info { get; set; }
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (success, inRam) = await LoadRegistry(e.Parameter).ConfigureAwait(false);
            if (success) await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                RefreshView();
                if (inRam) return;

                Pages.Clear();
                PageNumber.Maximum = Info.Registry.Pages.Length;
                foreach (var page in Info.Registry.Pages) Pages.Add(new PageList { Number = page.Number, Page = page });
                _ = Task.Run(async () =>
                {
                    for (int i = 0; i < Pages.Count; i++)
                    {
                        var page = Pages[i];
                        var img = i == Info.PageNumber - 1 ? await Info.Provider.API.Preview(Info.Registry, page.Page, TrackProgress) : await Info.Provider.API.Thumbnail(Info.Registry, page.Page, null);
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            page.Thumbnail = img.Image.ToImageSource();
                            Pages[i] = page;
                            if (i == Info.PageNumber - 1) RefreshView();
                        });
                    }
                }).ContinueWith((task) => throw task.Exception, TaskContinuationOptions.OnlyOnFaulted);
            });
            else await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (Frame.CanGoBack) Frame.GoBack();
                else Frame.Navigate(typeof(MainPage));
            });
        }

        public ObservableCollection<PageList> Pages = new ObservableCollection<PageList>();
        public async Task<(bool success, bool inRam)> LoadRegistry(object Parameter)
        {
            var inRam = false;

            if (Parameter is RegistryInfo) Info = Parameter as RegistryInfo;
            else if (Parameter is Dictionary<string, string>)
            {
                var param = Parameter as Dictionary<string, string>;
                if (param.ContainsKey("url") && Uri.TryCreate(param.GetValueOrDefault("url"), UriKind.Absolute, out var uri))
                {
                    await LocalData.LoadData().ConfigureAwait(false);
                    Info = await TryGetFromProviders(uri).ConfigureAwait(false);
                }
            }
            else if (Parameter is Uri) Info = await TryGetFromProviders(Parameter as Uri).ConfigureAwait(false);
            else inRam = true;

            async Task<RegistryInfo> TryGetFromProviders(Uri uri)
            {
                foreach (var provider in Data.Providers.Values)
                    if (provider.API.TryGetRegistryID(uri, out var info))
                        return provider.Registries.ContainsKey(info.RegistryID) ? info : await provider.API.Infos(uri);
                return null;
            }

            return (Info != null, inRam);
        }

        private async void ChangePage(object sender, ItemClickEventArgs e) => await ChangePage(e.ClickedItem as PageList).ConfigureAwait(false);
        public async Task ChangePage(PageList page)
        {
            Info.PageNumber = page.Number;
            await Info.Provider.API.Preview(Info.Registry, page.Page, TrackProgress);
            if ((page.Thumbnail is null || page.Thumbnail.PixelWidth == 0 || page.Thumbnail.PixelHeight == 0) && await Data.TryGetImageFromDrive(Info.Registry, page.Page, 0))
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    page.Thumbnail = page.Page.Image.ToImageSource();
                    Pages[page.Number - 1] = page;
                });
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, RefreshView);
        }
        public void RefreshView()
        {
            PageNumber.Value = Info.PageNumber;
            PageTotal.Text = $"/ {Info.Registry.Pages.Length}";
            SetInfo(Info_LocationCity, Info.Location?.Name);
            SetInfo(Info_LocationDistrict, Info.Location?.District);
            SetInfo(Info_RegistryType, Info.Registry?.TypeToString);
            SetInfo(Info_RegistryDate, Info.Registry?.Dates);
            SetInfo(Info_RegistryNotes, Info.Registry?.Notes);
            SetInfo(Info_RegistryID, Info.Registry?.ID);
            void SetInfo(TextBlock block, string text)
            {
                block.Text = text ?? "";
                block.Visibility = string.IsNullOrWhiteSpace(text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            }

            image.Source = Info.Page?.Image?.ToImageSource();
            PageList.SelectedIndex = Info.Page?.Number - 1 ?? 0;
            PageList.ScrollIntoView(PageList.SelectedItem);
            imagePanel.Reset();
            OnPropertyChanged(nameof(image));
            LocalData.SaveData();
        }

        private async void Download(object sender, Windows.UI.Xaml.RoutedEventArgs e) => await Download().ConfigureAwait(false);
        async Task<string> Download()
        {
            var page = await Info.Provider.API.Download(Info.Registry, Info.Page, TrackProgress);
            RefreshView();
            return await Data.SaveImage(Info.Registry, page);
        }
        private async void OpenFolder(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var page = await LocalData.GetFile(Info.Registry, Info.Page);
            var options = new Windows.System.FolderLauncherOptions();
            options.ItemsToSelect.Add(page);
            await Windows.System.Launcher.LaunchFolderAsync(await page.GetParentAsync(), options);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public async void TrackProgress(Progress progress) => await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        {
            imageProgress.Visibility = progress.Done ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            imageProgress.IsIndeterminate = progress.Undetermined;
            imageProgress.Value = progress.Value;
        });
    }

    public class PageList
    {
        public int Number { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public RPage Page { get; set; }
    }
}
