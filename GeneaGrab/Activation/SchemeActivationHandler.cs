using GeneaGrab.Services;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace GeneaGrab.Activation
{
    internal class SchemeActivationHandler : ActivationHandler<ProtocolActivatedEventArgs>
    {
        protected override Task HandleInternalAsync(ProtocolActivatedEventArgs args)
        {
            // Create data from activation Uri in ProtocolActivatedEventArgs
            var data = new SchemeActivationData(args.Uri);
            if (data.IsValid)
            {
                if (NavigationService.TryGetTabWithId(data.Identifier, out var tab))
                {
                    NavigationService.OpenTab(tab);
                    NavigationService.Navigate(data.PageType, data.Parameters);
                }
                else NavigationService.NewTab(data.PageType, data.Parameters);
            }
            return Task.CompletedTask;
        }

        protected override bool CanHandleInternal(ProtocolActivatedEventArgs args)
        {
            // If your app has multiple handlers of ProtocolActivationEventArgs use this method to determine which to use. (possibly checking args.Uri.Scheme)
            return true;
        }
    }
}
