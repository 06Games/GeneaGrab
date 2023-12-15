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
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DiscordRPC;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using GeneaGrab.Core.Models;
using GeneaGrab.Helpers;
using GeneaGrab.Models.Indexing;
using GeneaGrab.Services;
using Button = DiscordRPC.Button;

namespace GeneaGrab.Views
{
    public partial class RegistryViewer : Page, INotifyPropertyChanged, ITabPage
    {
        public Symbol IconSource => Symbol.Pictures;
        public string? DynaTabHeader
        {
            get
            {
                if (Info is null) return null;
                var location = Info.Registry.Location ?? Info.Registry.LocationID;
                var registry = Info.Registry?.Name ?? Info.RegistryId;
                return location is null ? registry : $"{location}: {registry}";
            }
        }
        public string? Identifier => Info?.RegistryId;
        public async Task RichPresence(RichPresence richPresence)
        {
            if (Info is null) return;
            var url = await Info.Provider.Ark(Info.Registry, Info.Page);
            richPresence.Buttons = new[]
            {
                new Button
                {
                    Label = ResourceExtensions.GetLocalized("Discord.OpenRegistry", ResourceExtensions.Resource.UI),
                    Url = Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url : Info.Registry.URL
                }
            };
        }


        protected string? DownloadText => ResourceExtensions.GetLocalized("Registry.Download", ResourceExtensions.Resource.UI);
        protected string? OpenFolderText => ResourceExtensions.GetLocalized("Registry.OpenFolder", ResourceExtensions.Resource.UI);
        protected string? ArkText => ResourceExtensions.GetLocalized("Registry.Ark", ResourceExtensions.Resource.UI);
        protected string? NotesText => ResourceExtensions.GetLocalized("Registry.Notes", ResourceExtensions.Resource.UI);


        public RegistryViewer()
        {
            InitializeComponent();
            DataContext = this;

            var pageNumber = PageNumber;
            if (pageNumber != null)
                pageNumber.ValueChanged += async (_, ne) =>
                {
                    if (PageNumbers.Contains((int)ne.NewValue)) await ChangePage((int)ne.NewValue);
                };

            var pageNotes = PageNotes;
            if (pageNotes != null)
                pageNotes.TextChanging += (_, _) =>
                {
                    var text = pageNotes.Text;
                    if (Info == null || !PageNumbers.Contains(Info.PageNumber)) return;
                    Info.Page.Notes = string.IsNullOrWhiteSpace(text) ? null : text;
                    var index = PageNumbers.IndexOf(Info.PageNumber);
                    Pages[index] = Pages[index].Refresh();
                };

            Image.GetObservable(BoundsProperty).Subscribe(b =>
            {
                MainGrid.Width = b.Width;
                MainGrid.Height = b.Height;
            });
        }

        public RegistryInfo? Info { get; set; }
        public override async void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);
            var (keepTabOpened, noRefresh) = await LoadRegistry(args.Parameter).ConfigureAwait(false);
            if (!keepTabOpened)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (NavigationService.CanGoBack) NavigationService.GoBack();
                    else NavigationService.CloseTab();
                });
                return;
            }

            var tab = Info == null ? null : await Dispatcher.UIThread.InvokeAsync(() => NavigationService.TryGetTabWithId(Info.RegistryId, out var tab) ? tab : null);
            if (tab != null)
            {
                var currentTab = NavigationService.CurrentTab;
                NavigationService.OpenTab(tab);
                if (NavigationService.Frame?.Content is not RegistryViewer viewer) return;
                await viewer.ChangePage(Info!.PageNumber);
                if (!ReferenceEquals(viewer, this))
                {
                    await Dispatcher.UIThread.InvokeAsync(() => NavigationService.CloseTab(currentTab!));
                    return;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RefreshView();
                MainWindow.UpdateSelectedTitle();
            });
            if (noRefresh || Info == null) return;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PageNumbers.Clear();
                Pages.Clear();

                var pageNumber = PageNumber;
                pageNumber.Minimum = Info.Registry.Pages.Any() ? Info.Registry.Pages.Min(p => p.Number) : 0;
                pageNumber.Maximum = Info.Registry.Pages.Any() ? Info.Registry.Pages.Max(p => p.Number) : 0;
                PageNumbers = Info.Registry.Pages.Select(p => p.Number).ToList();
                foreach (var page in Info.Registry.Pages) Pages.Add(page!);
            });

            _ = Task.Run(async () =>
            {
                var img = await LoadImage(Info.PageNumber, page => Info.Provider.Preview(Info.Registry, page, TrackProgress), false);
                await Dispatcher.UIThread.InvokeAsync(() => RefreshView(img));

                var tasks = new List<Task>();
                foreach (var page in Pages.ToList().Where(page => page.Number != Info.PageNumber))
                {
                    if (tasks.Count >= 5) tasks.Remove(await Task.WhenAny(tasks).ConfigureAwait(false));
                    tasks.Add(LoadImage(page.Number, rPage => Info.Provider.Thumbnail(Info.Registry, rPage, null)));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);

                async Task<Stream> LoadImage(int number, Func<RPage, Task<Stream>> func, bool close = true)
                {
                    var i = PageNumbers.IndexOf(number);
                    var page = Pages[i];
                    var thumbnail = await func.Invoke(page.Page);
                    page.Thumbnail = thumbnail.ToBitmap(close);
                    await Dispatcher.UIThread.InvokeAsync(() => Pages[i] = page);
                    return thumbnail;
                }
                await Dispatcher.UIThread.InvokeAsync(() => RefreshView());
            });
        }

        public List<int> PageNumbers { get; set; } = new();
        public ObservableCollection<PageList> Pages { get; } = new();
        private async Task<(bool success, bool inRam)> LoadRegistry(object parameter)
        {
            var inRam = false;

            switch (parameter)
            {
                case RegistryInfo infos:
                    Info = infos;
                    break;
                case Uri url:
                    await LocalData.LoadDataAsync().ConfigureAwait(false);

                    RegistryInfo? info = null;
                    Provider? provider = null;
                    foreach (var p in Data.Providers.Values)
                        if ((info = await p.GetRegistryFromUrlAsync(url)) != null)
                        {
                            provider = p;
                            break;
                        }
                    if (provider != null && info != null) Info = provider.Registries.ContainsKey(info.RegistryId) ? info : await provider.Infos(url);
                    break;
                default:
                    inRam = true;
                    break;
            }
            return (Info != null, inRam);
        }


        private void GoToPreviousPage(object? _, RoutedEventArgs e) => ChangePage(Info?.PageNumber - 1 ?? -1);
        private void GoToNextPage(object? _, RoutedEventArgs e) => ChangePage(Info?.PageNumber + 1 ?? -1);
        private async void ChangePage(object _, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count >= 1 && e.AddedItems[0] is PageList page) await ChangePage(page).ConfigureAwait(false);
        }
        public Task ChangePage(int pageNumber) => ChangePage(Info?.GetPage(pageNumber));
        public async Task ChangePage(PageList? page)
        {
            if (page is null || Info is null || Info.PageNumber == page.Number) return;
            Info.PageNumber = page.Number;
            var image = await Info.Provider.Preview(Info.Registry, page.Page, TrackProgress);
            var (success, stream) = await Data.TryGetThumbnailFromDrive(Info.Registry, page.Page);
            if ((page.Thumbnail is null || page.Thumbnail.PixelSize.Width == 0 || page.Thumbnail.PixelSize.Height == 0) && success)
                Dispatcher.UIThread.Post(() =>
                {
                    page.Thumbnail = stream.ToBitmap(false);
                    Pages[PageNumbers.IndexOf(page.Number)] = page;
                });
            RefreshView(image);
        }
        private void RefreshView(Stream? img = null)
        {
            if (Info?.Registry == null) return;

            var pageTotal = Info.Registry.Pages.Any() ? Info.Registry.Pages.Max(p => p.Number) : 0;
            PageNumber.Value = Info.PageNumber;
            PageTotal.Text = $"/ {pageTotal}";
            PreviousPage.IsEnabled = Info.PageNumber > 1;
            NextPage.IsEnabled = Info.PageNumber < pageTotal;
            SetInfo(InfoLocationCity, Info.Registry.Location ?? Info.Registry.LocationID ?? Info.Registry.LocationDetails?.LastOrDefault());
            SetInfo(InfoLocationDistrict, Info.Registry!.District ?? Info.Registry.DistrictID);
            SetInfo(InfoRegistryType, Info.Registry!.TypeToString);
            SetInfo(InfoRegistryDate, Info.Registry.Dates);
            SetInfo(InfoRegistryTitle, Info.Registry.Title);
            SetInfo(InfoRegistrySubtitle, Info.Registry.Subtitle);
            SetInfo(InfoRegistryAuthor, Info.Registry.Author);
            SetInfo(InfoRegistryNotes, Info.Registry.Notes);
            SetInfo(InfoRegistryId, Info.Registry.CallNumber ?? Info.Registry.ID);
            void SetInfo(TextBlock block, string? text)
            {
                block.Text = text ?? "";
                block.IsVisible = !string.IsNullOrWhiteSpace(text);
            }

            PageNotes.Text = Info.Page?.Notes ?? "";
            DisplayIndex();

            var image = Image;
            var pageList = PageList;
            if (img != null) image.Source = img.ToBitmap();
            pageList.Selection.Select(Info.PageIndex);
            pageList.ScrollIntoView(Info.PageIndex);
            ImagePanel.Reset();
            OnPropertyChanged(nameof(image));
            _ = Task.Run(async () => await LocalData.SaveRegistryAsync(Info.Registry));
        }

        private async void Download(object sender, RoutedEventArgs e)
        {
            if (Info == null) return;
            var stream = await Info.Provider.Download(Info.Registry, Info.Page, TrackProgress);
            RefreshView(stream);
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            if (Info == null) return;
            var page = LocalData.GetFile(Info.Registry, Info.Page);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try { WinExplorer.OpenFolderAndSelectItem(page.FullName); }
                catch { Process.Start("explorer.exe", "/select,\"" + page.FullName + "\""); }
            }
            else if (page.DirectoryName != null)
            {
                var url = $"file://{page.DirectoryName.Replace(" ", "%20")}";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
        private async void Ark(object sender, RoutedEventArgs e)
        {
            if (Info == null) return;
            await TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(await Info.Provider.Ark(Info.Registry, Info.Page))!;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void TrackProgress(Progress progress) => Dispatcher.UIThread.Post(() =>
        {
            var imageProgress = ImageProgress;
            imageProgress.IsVisible = !progress.Done;
            imageProgress.IsIndeterminate = progress.Undetermined;
            imageProgress.Value = progress.Value;
        });

        #region Index

        private void AddIndex(object sender, RoutedEventArgs e)
        {
            if (Info is null) return;
            using (var db = new DatabaseContext())
            {
                db.Records.Add(new Record(Info.Registry, Info.Page)
                {
                    Position = new Rect(100, 75, 100, 50)
                });
                db.SaveChanges();
            }
            DisplayIndex();
        }
        private void DisplayIndex()
        {
            if (Info is null) return;
            ImageCanvas.Children.Clear();

            using var db = new DatabaseContext();
            var indexes = db.Records.Where(r => r.ProviderId == Info.ProviderId && r.RegistryId == Info.RegistryId && r.FrameNumber == Info.PageNumber);
            RecordList.ItemsSource = indexes.ToList();
            foreach (var index in indexes)
                DisplayIndexRectangle(index);
        }
        private void DisplayIndexRectangle(Record? index)
        {
            if (index?.Position is null) return;

            var pos = index.Position.Value;
            var btn = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromRgb((byte)(index.Id * 100 % 255), (byte)((index.Id + 2) * 50 % 255), (byte)((index.Id + 1) * 75 % 255))),
                Opacity = .25,
                Width = pos.Width,
                Height = pos.Height
            };

            var tt = new ToolTip { Content = index.ToString() };
            ToolTip.SetTip(btn, tt);

            ImageCanvas.Children.Add(btn);
            Canvas.SetLeft(btn, pos.X);
            Canvas.SetTop(btn, pos.Y);
        }

        #endregion
    }

    public class PageList
    {
        public static implicit operator PageList?(RPage? page) => page is null ? null : new PageList { Page = page }.Refresh();
        public RPage Page { get; private init; } = null!;
        public PageList Refresh()
        {
            Number = Page.Number;
            Notes = Page.Notes?.Split('\n').FirstOrDefault() ?? "";
            return this;
        }

        public Bitmap? Thumbnail { get; set; }
        public int Number { get; private set; }
        public string Notes { get; private set; } = "";
    }
}
