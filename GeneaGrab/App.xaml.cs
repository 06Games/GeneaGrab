using GeneaGrab.Services;
using Serilog;
using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace GeneaGrab
{
    public sealed partial class App : Application
    {
        private Lazy<ActivationService> _activationService;
        private ActivationService ActivationService => _activationService.Value;
        public App()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(), $@"{Windows.Storage.ApplicationData.Current.TemporaryFolder.Path}\Logs\{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}.json").CreateLogger();
            Data.Translate = (id, fallback) => Helpers.ResourceExtensions.GetLocalized(Helpers.Resource.Core, id) ?? fallback;
            Data.GetImage = LocalData.GetImageAsync;
            Data.SaveImage = LocalData.SaveImageAsync;

            InitializeComponent();
            UnhandledException += OnAppUnhandledException;
            _activationService = new Lazy<ActivationService>(CreateActivationService); // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
        }
        protected override async void OnActivated(IActivatedEventArgs args) => await ActivationService.ActivateAsync(args);
        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) => Log.Fatal(e.Message, e.Exception);
        private ActivationService CreateActivationService() => new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(CreateShell));
        private UIElement CreateShell() => new Views.ShellPage();

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated) await ActivationService.ActivateAsync(args);
            await LocalData.LoadData().ConfigureAwait(false);
        }
    }
}
