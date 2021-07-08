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
            UnhandledException += (_, e) => Log.Fatal(e.Message, e.Exception);
            _activationService = new Lazy<ActivationService>(() => new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(new Views.ShellPage())));
        }
        protected override async void OnActivated(IActivatedEventArgs args) => await ActivationService.ActivateAsync(args);
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated) await ActivationService.ActivateAsync(args);
            await LocalData.LoadData().ConfigureAwait(false);
        }
    }
}
