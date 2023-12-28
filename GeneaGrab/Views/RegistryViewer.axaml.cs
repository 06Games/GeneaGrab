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
using Frame = GeneaGrab.Core.Models.Frame;

namespace GeneaGrab.Views
{
    public partial class RegistryViewer : Page, INotifyPropertyChanged, ITabPage
    {
        public Symbol IconSource => Symbol.Pictures;
        public string? DynaTabHeader
        {
            get
            {
                if (Registry is null) return null;
                var location = Registry.Location.ToArray();
                var registry = Registry.ToString();
                return !location.Any() ? registry : $"{location}: {registry}";
            }
        }
        public string? Identifier => Registry?.Id;
        public async Task RichPresence(RichPresence richPresence)
        {
            var url = await Provider.Ark(Frame);
            richPresence.Buttons =
            [
                new Button
                {
                    Label = ResourceExtensions.GetLocalized("Discord.OpenRegistry", ResourceExtensions.Resource.UI),
                    Url = Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url : Registry?.URL
                }
            ];
        }


        protected string? DownloadText => ResourceExtensions.GetLocalized("Registry.Download", ResourceExtensions.Resource.UI);
        protected string? OpenFolderText => ResourceExtensions.GetLocalized("Registry.OpenFolder", ResourceExtensions.Resource.UI);
        protected string? ArkText => ResourceExtensions.GetLocalized("Registry.Ark", ResourceExtensions.Resource.UI);
        protected string? NotesText => ResourceExtensions.GetLocalized("Registry.Notes", ResourceExtensions.Resource.UI);


        public Provider Provider => Data.Providers[Registry?.ProviderId];
        public Registry? Registry { get; private set; }
        public Frame? Frame { get; private set; }

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
                    Frame.Notes = string.IsNullOrWhiteSpace(text) ? null : text;
                    var index = PageNumbers.IndexOf(Frame.FrameNumber);
                    Pages[index] = Pages[index].Refresh();
                };

            Image.GetObservable(BoundsProperty).Subscribe(b =>
            {
                MainGrid.Width = b.Width;
                MainGrid.Height = b.Height;
            });
        }

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

            var tab = await Dispatcher.UIThread.InvokeAsync(() => NavigationService.TryGetTabWithId(Registry.Id, out var tab) ? tab : null);
            if (tab != null)
            {
                var currentTab = NavigationService.CurrentTab;
                NavigationService.OpenTab(tab);
                if (NavigationService.Frame?.Content is not RegistryViewer viewer) return;
                await viewer.ChangePage(Frame.FrameNumber);
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
            if (noRefresh) return;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PageNumbers.Clear();
                Pages.Clear();

                var pageNumber = PageNumber;
                pageNumber.Minimum = Registry.Frames.Any() ? Registry.Frames.Min(p => p.FrameNumber) : 0;
                pageNumber.Maximum = Registry.Frames.Any() ? Registry.Frames.Max(p => p.FrameNumber) : 0;
                PageNumbers = Registry.Frames.Select(p => p.FrameNumber).ToList();
                foreach (var page in Registry.Frames) Pages.Add(page!);
            });

            _ = Task.Run(async () =>
            {
                var img = await LoadImage(Frame.FrameNumber, page => Provider.GetFrame(page, Scale.Navigation, TrackProgress), false);
                await Dispatcher.UIThread.InvokeAsync(() => RefreshView(img));

                var tasks = new List<Task>();
                foreach (var page in Pages.ToList().Where(page => page.Number != Frame.FrameNumber))
                {
                    if (tasks.Count >= 5) tasks.Remove(await Task.WhenAny(tasks).ConfigureAwait(false));
                    tasks.Add(LoadImage(page.Number, rPage => Provider.GetFrame(rPage, Scale.Thumbnail, null)));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);

                async Task<Stream> LoadImage(int number, Func<Frame, Task<Stream>> func, bool close = true)
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

            await using var db = new DatabaseContext();
            switch (parameter)
            {
                case RegistryInfo infos:
                    Registry = db.Registries.FirstOrDefault(r => r.Id == infos.RegistryId)!;
                    Frame = db.Frames.FirstOrDefault(r => r.RegistryId == infos.RegistryId && r.FrameNumber == infos.PageNumber)!;
                    break;
                case Uri url:
                    RegistryInfo? info = null;
                    Provider? provider = null;
                    foreach (var p in Data.Providers.Values)
                        if ((info = await p.GetRegistryFromUrlAsync(url)) != null)
                        {
                            provider = p;
                            break;
                        }
                    if (provider != null && info != null)
                    {
                        var registry = db.Registries.FirstOrDefault(r => r.Id == info.RegistryId);
                        var frame = db.Frames.FirstOrDefault(r => r.RegistryId == info.RegistryId && r.FrameNumber == info.PageNumber);
                        if (registry is null || frame is null)
                        {
                            var data = await provider.Infos(url);
                            registry = data.registry;
                            frame = registry.Frames.FirstOrDefault(f => f.FrameNumber == data.pageNumber) ?? registry.Frames.First();
                        }
                        Registry = registry;
                        Frame = frame;
                    }
                    break;
                default:
                    inRam = true;
                    break;
            }
            return (Registry != null && Frame != null, inRam);
        }


        private void GoToPreviousPage(object? _, RoutedEventArgs e) => ChangePage(Frame.FrameNumber - 1);
        private void GoToNextPage(object? _, RoutedEventArgs e) => ChangePage(Frame.FrameNumber + 1);
        private async void ChangePage(object _, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count >= 1 && e.AddedItems[0] is PageList page) await ChangePage(page).ConfigureAwait(false);
        }
        public async Task ChangePage(int pageNumber)
        {
            await using var db = new DatabaseContext();
            ChangePage(db.Frames.FirstOrDefault(f => f.RegistryId == Registry.Id && f.FrameNumber == pageNumber));
        }
        public async Task ChangePage(PageList? page)
        {
            if (page is null || Frame.FrameNumber == page.Number) return;
            Frame = page.Page;
            var image = await Provider.GetFrame(page.Page, Scale.Navigation, TrackProgress);
            var (success, stream) = await Data.TryGetThumbnailFromDrive(page.Page);
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
            var pageTotal = 0; //Registry.Frames.Any() ? Registry.Frames.Max(p => p.Number) : 0; // TODO
            PageNumber.Value = Frame.FrameNumber;
            PageTotal.Text = $"/ {pageTotal}";
            PreviousPage.IsEnabled = Frame.FrameNumber > 1;
            NextPage.IsEnabled = Frame.FrameNumber < pageTotal;
            SetInfo(InfoLocationCity, string.Join(", ", Registry.Location));
            /*SetInfo(InfoRegistryType, Info.Registry!.TypeToString);
            SetInfo(InfoRegistryDate, Registry.Dates);*/ // TODO
            SetInfo(InfoRegistryTitle, Registry.Title);
            SetInfo(InfoRegistrySubtitle, Registry.Subtitle);
            SetInfo(InfoRegistryAuthor, Registry.Author);
            SetInfo(InfoRegistryNotes, Registry.Notes);
            SetInfo(InfoRegistryId, Registry.CallNumber ?? Registry.Id);
            void SetInfo(TextBlock block, string? text)
            {
                block.Text = text ?? "";
                block.IsVisible = !string.IsNullOrWhiteSpace(text);
            }

            PageNotes.Text = Frame.Notes ?? "";
            DisplayIndex();

            var image = Image;
            var pageList = PageList;
            if (img != null) image.Source = img.ToBitmap();
            pageList.Selection.Select(Frame.FrameNumber - 1);
            pageList.ScrollIntoView(Frame.FrameNumber - 1);
            ImagePanel.Reset();
            OnPropertyChanged(nameof(image));
            //_ = Task.Run(async () => await LocalData.SaveRegistryAsync(Registry));
        }

        private async void Download(object sender, RoutedEventArgs e)
        {
            var stream = await Provider.GetFrame(Frame, Scale.Full, TrackProgress);
            RefreshView(stream);
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            var page = LocalData.GetFile(Frame);

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
            await TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(await Provider.Ark(Frame))!;
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
            using var db = new DatabaseContext();
            db.Records.Add(new Record(Registry.ProviderId, Registry.Id, Frame.FrameNumber)
            {
                Position = new Rect(100, 75, 100, 50)
            });
            db.SaveChanges();
            DisplayIndex();
        }
        private void DisplayIndex()
        {
            ImageCanvas.Children.Clear();

            using var db = new DatabaseContext();
            var indexes = db.Records.Where(r => r.ProviderId == Registry.ProviderId && r.RegistryId == Registry.Id && r.FrameNumber == Frame.FrameNumber);
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
        public static implicit operator PageList?(Frame? page) => page is null ? null : new PageList { Page = page }.Refresh();
        public Frame Page { get; private init; } = null!;
        public PageList Refresh()
        {
            Number = Page.FrameNumber;
            Notes = Page.Notes?.Split('\n').FirstOrDefault() ?? "";
            return this;
        }

        public Bitmap? Thumbnail { get; set; }
        public int Number { get; private set; }
        public string Notes { get; private set; } = "";
    }
}
