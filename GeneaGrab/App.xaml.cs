using GeneaGrab.Services;
using SixLabors.ImageSharp;
using System;
using System.IO;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace GeneaGrab
{
    public sealed partial class App : Application
    {
        private Lazy<ActivationService> _activationService;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        public App()
        {
            InitializeComponent();
            UnhandledException += OnAppUnhandledException;

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Data.Translate = (id, fallback) => Helpers.ResourceExtensions.GetLocalized(Helpers.Resource.Core, id) ?? fallback;
            Data.GetImage = async (registry, page) =>
            {
                try
                {
                    var folder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderPath(registry.ProviderID, registry.ID);
                    var file = await folder.TryGetItemAsync($"p{page.Number}.jpg") as Windows.Storage.StorageFile;
                    return file is null ? null : await Image.LoadAsync(await file.OpenStreamForReadAsync());
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    return null;
                }
            };
            Data.SaveImage = async (registry, page) =>
            {
                Windows.Storage.StorageFolder folder = await Windows.Storage.ApplicationData.Current.LocalCacheFolder.CreateFolderPath(registry.ProviderID, registry.ID);
                Windows.Storage.StorageFile file = await folder.CreateFileAsync($"p{page.Number}.jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                try { await page.Image.SaveAsJpegAsync(await file.OpenStreamForWriteAsync()); } catch (Exception e) { System.Diagnostics.Debug.WriteLine(e); }
                return file.Path;
            };

            if (!args.PrelaunchActivated) await ActivationService.ActivateAsync(args);

            await Views.MainPage.LoadData();
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/uwp/api/windows.ui.xaml.application.unhandledexception
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(CreateShell));
        }

        private UIElement CreateShell()
        {
            return new Views.ShellPage();
        }
    }
}
