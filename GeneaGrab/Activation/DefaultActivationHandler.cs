using GeneaGrab.Services;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace GeneaGrab.Activation
{
    internal class DefaultActivationHandler : ActivationHandler<IActivatedEventArgs>
    {
        private readonly Type _navElement;
        public DefaultActivationHandler(Type navElement) => _navElement = navElement;

        protected override Task HandleInternalAsync(IActivatedEventArgs args)
        {
            // When the navigation stack isn't restored, navigate to the first page and configure the new page by passing required information in the navigation parameter
            object arguments = args is LaunchActivatedEventArgs launchArgs ? launchArgs.Arguments : null;

            NavigationService.Navigate(_navElement, arguments);
            return Task.CompletedTask;
        }

        protected override bool CanHandleInternal(IActivatedEventArgs args)
        {
            // None of the ActivationHandlers has handled the app activation
            return NavigationService.Frame.Content == null && _navElement != null;
        }
    }
}
