using GeneaGrab.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using GeneaGrab.Services;

namespace GeneaGrab.Views
{
    public partial class RegistryViewer : Page, INotifyPropertyChanged, ITabPage //, ISchemeSupport
    {
        public Symbol IconSource => Symbol.Pictures;
        public string? DynaTabHeader
        {
            get
            {
                if (Info is null) return null;
                var location = Info.Registry.Location ?? Info.Registry.LocationID;
                var registry = Info.Registry?.Name ?? Info.RegistryID;
                return location is null ? registry : $"{location}: {registry}";
            }
        }
        public string? Identifier => Info?.RegistryID;



        /*public string UrlPath => "registry";
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
                    if (provider.API.TryGetRegistryID(uri, out var info)) await ChangePage(info.PageNumber);
            }
        }*/



        protected string? DownloadText => ResourceExtensions.GetLocalized("Registry.Download", ResourceExtensions.Resource.UI);
        protected string? OpenFolderText => ResourceExtensions.GetLocalized("Registry.OpenFolder", ResourceExtensions.Resource.UI);
        protected string? ArkText => ResourceExtensions.GetLocalized("Registry.Ark", ResourceExtensions.Resource.UI);
        protected string? NotesText => ResourceExtensions.GetLocalized("Registry.Notes", ResourceExtensions.Resource.UI);


        public RegistryViewer()
        {
            InitializeComponent();
            var pageNumber = this.FindControl<NumberBox>("PageNumber");
            if (pageNumber != null)
                pageNumber.ValueChanged += async (_, ne) =>
                {
                    if (PageNumbers.Contains((int)ne.NewValue)) await ChangePage((int)ne.NewValue);
                };

            var pageNotes = this.FindControl<TextBox>("PageNotes");
            if (pageNotes != null)
                pageNotes.TextChanging += (_, _) =>
                {
                    var text = pageNotes.Text;
                    if (Info == null || !PageNumbers.Contains(Info.PageNumber)) return;
                    Info.Page.Notes = string.IsNullOrWhiteSpace(text) ? null : text;
                    var index = PageNumbers.IndexOf(Info.PageNumber);
                    Pages[index] = Pages[index].Refresh();
                };
        }

        private void InitializeComponent()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }

        public RegistryInfo? Info { get; set; }
        public override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (success, inRam) = await LoadRegistry(e.Parameter).ConfigureAwait(false);
            if (!success)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (NavigationService.CanGoBack) NavigationService.GoBack();
                    else NavigationService.CloseTab();
                });
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RefreshView();
                MainWindow.UpdateSelectedTitle();
            });
            if (inRam || Info == null) return;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PageNumbers.Clear();
                Pages.Clear();

                var pageNumber = this.FindControl<NumberBox>("PageNumber");
                pageNumber.Minimum = Info.Registry.Pages.Any() ? Info.Registry.Pages.Min(p => p.Number) : 0;
                pageNumber.Maximum = Info.Registry.Pages.Any() ? Info.Registry.Pages.Max(p => p.Number) : 0;
                PageNumbers = Info.Registry.Pages.Select(p => p.Number).ToList();
                foreach (var page in Info.Registry.Pages) Pages.Add(page!);
            });

            _ = Task.Run(async () =>
            {
                var img = await LoadImage(Info.PageNumber, page => Info.Provider.API.Preview(Info.Registry, page, TrackProgress));
                await Dispatcher.UIThread.InvokeAsync(() => RefreshView(img));

                var tasks = new List<Task>();
                foreach (var page in Pages.ToList().Where(page => page.Number != Info.PageNumber))
                {
                    if (tasks.Count >= 5) tasks.Remove(await Task.WhenAny(tasks).ConfigureAwait(false));
                    tasks.Add(LoadImage(page.Number, _page => Info.Provider.API.Thumbnail(Info.Registry, _page, null)));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);

                async Task<Stream> LoadImage(int number, Func<RPage, Task<Stream>> func)
                {
                    var i = PageNumbers.IndexOf(number);
                    var page = Pages[i];
                    var thumbnail = await func.Invoke(page.Page);
                    page.Thumbnail = thumbnail.ToBitmap();
                    await Dispatcher.UIThread.InvokeAsync(() => Pages[i] = page);
                    return thumbnail;
                }
                await GetIndex();
                await Dispatcher.UIThread.InvokeAsync(() => RefreshView());
            });
        }

        public List<int> PageNumbers { get; set; } = new();
        public ObservableCollection<PageList> Pages { get; } = new();
        private async Task<(bool success, bool inRam)> LoadRegistry(object parameter)
        {
            var inRam = false;

            if (parameter is RegistryInfo infos) Info = infos;
            else if (parameter is LaunchArgs param)
            {
                if (!string.IsNullOrWhiteSpace(param.Url) && Uri.TryCreate(param.Url, UriKind.Absolute, out var uri))
                {
                    await LocalData.LoadData().ConfigureAwait(false);
                    Info = await TryGetFromProviders(uri).ConfigureAwait(false);
                }
            }
            else if (parameter is Uri url) Info = await TryGetFromProviders(url).ConfigureAwait(false);
            else inRam = true;

            async Task<RegistryInfo?> TryGetFromProviders(Uri uri)
            {
                foreach (var provider in Data.Providers.Values)
                    if (provider.API.TryGetRegistryID(uri, out var info))
                        return provider.Registries.ContainsKey(info.RegistryID) ? info : await provider.API.Infos(uri);
                return null;
            }
            return (Info != null, inRam);
        }

        private async void ChangePage(object _, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count >= 1 && e.AddedItems[0] is PageList page) await ChangePage(page).ConfigureAwait(false);
        }
        public Task ChangePage(int pageNumber) => ChangePage(Info?.GetPage(pageNumber));
        public async Task ChangePage(PageList? page)
        {
            if (page is null || Info is null) return;
            Info.PageNumber = page.Number;
            var image = await Info.Provider.API.Preview(Info.Registry, page.Page, TrackProgress);
            var (success, stream) = await Data.TryGetThumbnailFromDrive(Info.Registry, page.Page);
            if ((page.Thumbnail is null || page.Thumbnail.PixelSize.Width == 0 || page.Thumbnail.PixelSize.Height == 0) && success)
                Dispatcher.UIThread.Post(() =>
                {
                    page.Thumbnail = stream.ToBitmap();
                    Pages[PageNumbers.IndexOf(page.Number)] = page;
                });
            await GetIndex();
            RefreshView(image);
        }
        private void RefreshView(Stream? img = null)
        {
            if (Info?.Registry == null) return;
            this.FindControl<NumberBox>("PageNumber").Value = Info.PageNumber;
            this.FindControl<TextBlock>("PageTotal").Text = $"/ {(Info.Registry.Pages.Any() ? Info.Registry.Pages.Max(p => p.Number) : 0)}";
            SetInfo(this.FindControl<TextBlock>("Info_LocationCity"), Info.Registry.Location ?? Info.Registry.LocationID);
            SetInfo(this.FindControl<TextBlock>("Info_LocationDistrict"), Info.Registry!.District ?? Info.Registry.DistrictID);
            SetInfo(this.FindControl<TextBlock>("Info_RegistryType"), Info.Registry!.TypeToString);
            SetInfo(this.FindControl<TextBlock>("Info_RegistryDate"), Info.Registry.Dates);
            SetInfo(this.FindControl<TextBlock>("Info_RegistryNotes"), Info.Registry.Notes);
            SetInfo(this.FindControl<TextBlock>("Info_RegistryID"), Info.Registry.CallNumber ?? Info.Registry.ID);
            void SetInfo(TextBlock block, string? text)
            {
                block.Text = text ?? "";
                block.IsVisible = !string.IsNullOrWhiteSpace(text);
            }

            this.FindControl<TextBox>("PageNotes").Text = Info.Page?.Notes ?? "";
            this.FindControl<Canvas>("ImageCanvas").Children.Clear();
            foreach (var index in Index.Where(i => i.Page == Info.PageNumber)) DisplayIndexRectangle(index);

            var image = this.FindControl<Image>("Image");
            var pageList = this.FindControl<ListBox>("PageList");
            if (img != null) image.Source = img.ToBitmap();
            pageList.SelectedIndex = Info.PageIndex;
            pageList.ScrollIntoView(pageList.SelectedIndex);
            this.FindControl<ZoomPanel>("ImagePanel").Reset();
            OnPropertyChanged(nameof(image));
            Task.Run(async () => await LocalData.SaveRegistryAsync(Info.Registry));
        }

        private async void Download(object sender, RoutedEventArgs e) => await Download().ConfigureAwait(false);
        async Task Download()
        {
            if (Info == null) return;
            await Info.Provider.API.Download(Info.Registry, Info.Page, TrackProgress);
            RefreshView();
        }
        private async void OpenFolder(object sender, RoutedEventArgs e)
        {
            if (Info == null) return;
            var page = await LocalData.GetFile(Info.Registry, Info.Page);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try { WinExplorer.OpenFolderAndSelectItem(page.FullName); }
                catch { Process.Start("explorer.exe", "/select,\"" + page.FullName + "\""); }
            }
            else if (page.Directory != null && new BclStorageFolder(page.Directory).TryGetUri(out var uri))
                Process.Start(new ProcessStartInfo { FileName = uri.ToString(), UseShellExecute = true });
        }
        private async void Ark(object sender, RoutedEventArgs e)
        {
            if (Info == null) return;
            await Application.Current?.Clipboard?.SetTextAsync(await Info.Provider.API.Ark(Info.Registry, Info.Page))!;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void TrackProgress(Progress progress)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var imageProgress = this.FindControl<ProgressBar>("ImageProgress");
                imageProgress.IsVisible = !progress.Done;
                imageProgress.IsIndeterminate = progress.Undetermined;
                imageProgress.Value = progress.Value;
            });
        }

        #region Index

        public ObservableCollection<Index> Index { get; private set; } = new();
        private async Task GetIndex()
        {
            if (Info == null) return;
            var indexPanel = this.FindControl<StackPanel>("IndexPanel");
            if (!Info.Provider.API.IndexSupport)
            {
                indexPanel.IsVisible = false;
                return;
            }

            indexPanel.IsVisible = true;
            IEnumerable<GeneaGrab.Index>? index = null;
            if (Info.Provider.API is IndexAPI indexAPI)
                index = await indexAPI.GetIndex(Info.Registry, Info.Page);
            if (index is null) Index.Clear();
            else Index = new ObservableCollection<Index>(index.Cast<Index>());
        }
        private void AddIndex(object sender, RoutedEventArgs e)
        {
            if (!(Info?.Provider.API.IndexSupport ?? false)) return;
            /* TODO */
        }
        private void DisplayIndexRectangle(Index? index)
        {
            if (index is null || index.Position.IsEmpty) return;

            var pos = index.Position;
            var btn = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(255, (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255))),
                Opacity = .25,
                Width = pos.Width,
                Height = pos.Height
            };

            var tt = new ToolTip { Content = $"{index.FormatedDate} ({index.FormatedType}): {index.District}\n{index.Notes}" };
            ToolTip.SetTip(btn, tt);

            this.FindControl<Canvas>("ImageCanvas").Children.Add(btn);
            Canvas.SetTop(btn, pos.X);
            Canvas.SetLeft(btn, pos.Y);
        }

        #endregion
    }

    public class Index : GeneaGrab.Index
    {
        public string? FormatedDate => Date?.ToString("d");
        public string? FormatedType
        {
            get
            {
                var typeName = Enum.GetName(typeof(RegistryType), Type);
                return Data.Translate($"Registry.Type.{typeName}", typeName);
            }
        }
    }
    public class PageList
    {
        public static implicit operator PageList?(RPage? page) => page is null ? null : new PageList { Page = page }.Refresh();
        public RPage Page { get; init; } = null!;
        public PageList Refresh()
        {
            Number = Page.Number;
            Notes = Page.Notes?.Split('\n', '\r')?.FirstOrDefault() ?? "";
            return this;
        }

        public Bitmap? Thumbnail { get; set; }
        public int Number { get; private set; }
        public string Notes { get; private set; } = "";
    }
}
