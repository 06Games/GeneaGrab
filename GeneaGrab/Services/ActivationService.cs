using GeneaGrab.Activation;
using GeneaGrab.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GeneaGrab.Services
{
    // For more information on understanding and extending activation flow see
    // https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/UWP/activation.md
    internal class ActivationService
    {
        private readonly Type _defaultNavItem;
        private readonly Lazy<UIElement> _shell;

        public ActivationService(App _, Type defaultNavItem, Lazy<UIElement> shell = null)
        {
            _shell = shell;
            _defaultNavItem = defaultNavItem;
        }

        public async Task ActivateAsync(object activationArgs)
        {
            if (IsInteractive(activationArgs))
            {
                await InitializeAsync().ConfigureAwait(true); // Initialize services that you need before app activation take into account that the splash screen is shown while this code runs.
                if (Window.Current.Content == null) // Do not repeat app initialization when the Window already has content, just ensure that the window is active
                    Window.Current.Content = _shell?.Value ?? new Frame(); // Create a Shell or Frame to act as the navigation context
            }

            await HandleActivationAsync(activationArgs).ConfigureAwait(false); // Depending on activationArgs one of ActivationHandlers or DefaultActivationHandler will navigate to the first page

            if (IsInteractive(activationArgs))
            {
                Window.Current.Activate(); // Ensure the current window is active
                await StartupAsync().ConfigureAwait(false); // Tasks after activation
            }
        }

        private Task InitializeAsync() => ThemeSelectorService.InitializeAsync();

        private async Task HandleActivationAsync(object activationArgs)
        {
            var activationHandler = GetActivationHandlers().FirstOrDefault(h => h.CanHandle(activationArgs));
            if (activationHandler != null) await activationHandler.HandleAsync(activationArgs);

            if (!IsInteractive(activationArgs)) return;
            var defaultHandler = new DefaultActivationHandler(_defaultNavItem);
            if (defaultHandler.CanHandle(activationArgs)) await defaultHandler.HandleAsync(activationArgs);
        }

        private Task StartupAsync() => ThemeSelectorService.SetRequestedThemeAsync();

        private IEnumerable<ActivationHandler> GetActivationHandlers()
        {
            yield return Singleton<SchemeActivationHandler>.Instance;
        }

        private bool IsInteractive(object args) => args is IActivatedEventArgs;
    }
}
