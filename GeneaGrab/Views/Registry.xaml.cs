using GeneaGrab.Activation;
using GeneaGrab.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;

namespace GeneaGrab.Views
{
    public sealed partial class Registry : Page, INotifyPropertyChanged, ITabPage, ISchemeSupport
    {
        public Symbol IconSource => Symbol.Pictures;
        public string DynaTabHeader => Info is null ? null : $"{Info.Registry.Location ?? Info.Registry.LocationID}: {Info.Registry?.Name ?? Info.RegistryID}";
        public string Identifier => Info?.RegistryID;



        public string UrlPath => "registry";
        string ISchemeSupport.GetIdFromParameters(Dictionary<string, string> param)
        {
            if (param.ContainsKey("url") && Uri.TryCreate(param.GetValueOrDefault("url"), UriKind.Absolute, out var uri))
            {
                foreach (var provider in Data.Providers.Values)
                    if (provider.API.TryGetRegistryID(uri, out var info)) return info.RegistryID;
            }
            return null;
        }
        async void ISchemeSupport.Load(Dictionary<string, string> param)
        {
            if (param.ContainsKey("url") && Uri.TryCreate(param.GetValueOrDefault("url"), UriKind.Absolute, out var uri))
            {
                foreach (var provider in Data.Providers.Values)
                    if (provider.API.TryGetRegistryID(uri, out var info)) await ChangePage(Pages[info.PageNumber - 1]);
            }
        }



        private string DownloadText => ResourceExtensions.GetLocalized(Resource.Res, "Registry/Download");
        private string OpenFolderText => ResourceExtensions.GetLocalized(Resource.Res, "Registry/OpenFolder");
        private string ArkText => ResourceExtensions.GetLocalized(Resource.Res, "Registry/Ark");
        private string NotesText => ResourceExtensions.GetLocalized(Resource.Res, "Registry/Notes");

        public Registry()
        {
            InitializeComponent();
            PageNumber.ValueChanged += async (ns, ne) =>
            {
                if (ne.NewValue <= Pages.Count) await ChangePage(Pages[(int)ne.NewValue - 1]);
            };
            PageNotes.TextChanged += (s, e) =>
            {
                if (Info.PageNumber <= Info.Registry.Pages.Length) Info.Registry.Pages[Info.PageNumber - 1].Notes = string.IsNullOrWhiteSpace(PageNotes.Text) ? null : PageNotes.Text;
                Pages[Info.PageNumber - 1] = Pages[Info.PageNumber - 1].Refresh();
            };
        }

        public RegistryInfo Info { get; set; }
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (success, inRam) = await LoadRegistry(e.Parameter).ConfigureAwait(false);
            if (success) await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                RefreshView();
                ShellPage.UpdateSelectedTitle();
                if (inRam) return;

                Pages.Clear();
                PageNumber.Maximum = Info.Registry.Pages.Length;
                foreach (var page in Info.Registry.Pages) Pages.Add(page);
                _ = Task.Run(async () =>
                {
                    await LoadImage(Info.PageNumber - 1, (page) => Info.Provider.API.Preview(Info.Registry, page, TrackProgress)).ContinueWith(async (t) => await Dispatcher.RunAsync(CoreDispatcherPriority.Low, RefreshView));

                    List<Task> tasks = new List<Task>();
                    for (int i = 0; i < Pages.Count; i++)
                    {
                        if (i == Info.PageNumber - 1) continue;
                        if (tasks.Count >= 5) tasks.Remove(await Task.WhenAny(tasks).ConfigureAwait(false));
                        tasks.Add(LoadImage(i, (page) => Info.Provider.API.Thumbnail(Info.Registry, page, null)));
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    async Task LoadImage(int i, Func<RPage, Task<RPage>> func)
                    {
                        var page = Pages[i];
                        var img = await func?.Invoke(page.Page);
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            page.Thumbnail = img.Image.ToImageSource();
                            Pages[i] = page;
                        });
                    }
                }).ContinueWith((task) => throw task.Exception, TaskContinuationOptions.OnlyOnFaulted);
                GetIndex().ContinueWith(async (t) => await Dispatcher.RunAsync(CoreDispatcherPriority.Low, RefreshView));
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

            if (Parameter is RegistryInfo infos) Info = infos;
            else if (Parameter is Dictionary<string, string> param)
            {
                if (param.ContainsKey("url") && Uri.TryCreate(param.GetValueOrDefault("url"), UriKind.Absolute, out var uri))
                {
                    await LocalData.LoadData().ConfigureAwait(false);
                    Info = await TryGetFromProviders(uri).ConfigureAwait(false);
                }
            }
            else if (Parameter is Uri url) Info = await TryGetFromProviders(url).ConfigureAwait(false);
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
            if (page is null) return;
            Info.PageNumber = page.Number;
            await Info.Provider.API.Preview(Info.Registry, page.Page, TrackProgress);
            if ((page.Thumbnail is null || page.Thumbnail.PixelWidth == 0 || page.Thumbnail.PixelHeight == 0) && await Data.TryGetImageFromDrive(Info.Registry, page.Page, 0))
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    page.Thumbnail = page.Page.Image.ToImageSource();
                    Pages[page.Number - 1] = page;
                });
            await GetIndex();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, RefreshView);
        }
        public void RefreshView()
        {
            PageNumber.Value = Info.PageNumber;
            PageTotal.Text = $"/ {Info.Registry.Pages.Length}";
            SetInfo(Info_LocationCity, Info.Registry?.Location ?? Info.Registry?.LocationID);
            SetInfo(Info_LocationDistrict, Info.Registry?.District ?? Info.Registry?.DistrictID);
            SetInfo(Info_RegistryType, Info.Registry?.TypeToString);
            SetInfo(Info_RegistryDate, Info.Registry?.Dates);
            SetInfo(Info_RegistryNotes, Info.Registry?.Notes);
            SetInfo(Info_RegistryID, Info.Registry?.ID);
            void SetInfo(TextBlock block, string text)
            {
                block.Text = text ?? "";
                block.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
            }

            PageNotes.Text = Info.Page?.Notes ?? "";
            imageCanvas.Children.Clear();
            foreach (var index in Index.Where(i => i.Page == Info.PageNumber)) DisplayIndexRectangle(index);

            image.Source = Info.Page?.Image?.ToImageSource();
            PageList.SelectedIndex = Info.PageNumber - 1;
            PageList.ScrollIntoView(PageList.SelectedItem);
            imagePanel.Reset();
            OnPropertyChanged(nameof(image));
            LocalData.SaveData();
        }

        private async void Download(object sender, RoutedEventArgs e) => await Download().ConfigureAwait(false);
        async Task Download()
        {
            await Info.Provider.API.Download(Info.Registry, Info.Page, TrackProgress);
            RefreshView();
        }
        private async void OpenFolder(object sender, RoutedEventArgs e)
        {
            var page = await LocalData.GetFile(Info.Registry, Info.Page);
            var options = new Windows.System.FolderLauncherOptions();
            options.ItemsToSelect.Add(page);
            await Windows.System.Launcher.LaunchFolderAsync(await page.GetParentAsync(), options);
        }
        private async void Ark(object sender, RoutedEventArgs e)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage { RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy };
            dataPackage.SetText(await Info.Provider.API.Ark(Info.Registry, Info.Page));
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public async void TrackProgress(Progress progress) => await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        {
            imageProgress.Visibility = progress.Done ? Visibility.Collapsed : Visibility.Visible;
            imageProgress.IsIndeterminate = progress.Undetermined;
            imageProgress.Value = progress.Value;
        });

        #region Index
        public ObservableCollection<Index> Index = new ObservableCollection<Index>();
        private async Task GetIndex()
        {
            if (!Info.Provider.API.IndexSupport) { IndexPanel.Visibility = Visibility.Collapsed; return; }
            else IndexPanel.Visibility = Visibility.Visible;
            var indexAPI = Info.Provider.API as IndexAPI;
            var index = await indexAPI.GetIndex(Info.Registry, Info.Page);
            if (index is null) Index.Clear();
            else Index = new ObservableCollection<Index>(index.Cast<Index>());
        }
        private void AddIndex(object sender, RoutedEventArgs e)
        {
            if (!Info.Provider.API.IndexSupport) return;
            /* TODO */
        }
        private void DisplayIndexRectangle(Index index)
        {
            if (index is null || index.Position.IsEmpty) return;

            var pos = index.Position;
            var btn = new Windows.UI.Xaml.Shapes.Rectangle
            {
                Fill = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255))),
                Opacity = .25,
                Width = pos.Width,
                Height = pos.Height
            };

            var tt = new ToolTip { Content = $"{index.FormatedDate} ({index.FormatedType}): {index.District}\n{index.Notes}" };
            ToolTipService.SetToolTip(btn, tt);

            imageCanvas.Children.Add(btn);
            Canvas.SetTop(btn, pos.X);
            Canvas.SetLeft(btn, pos.Y);
        }
        #endregion
    }

    public class Index : GeneaGrab.Index
    {
        public string FormatedDate => Date.ToString("d");
        public string FormatedType
        {
            get
            {
                var typeName = Enum.GetName(typeof(RegistryType), Type);
                return Data.Translate($"Registry/Type/{typeName}", typeName);
            }
        }
    }
    public class PageList
    {
        public static implicit operator PageList(RPage page) => new PageList { Page = page }.Refresh();
        public RPage Page { get; set; }
        public PageList Refresh()
        {
            Number = Page.Number;
            Notes = Page.Notes?.Split('\n', '\r')?.FirstOrDefault() ?? "";
            return this;
        }

        public BitmapImage Thumbnail { get; set; }
        public int Number { get; private set; }
        public string Notes { get; private set; }
    }
}
