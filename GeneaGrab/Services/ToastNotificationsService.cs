using GeneaGrab.Activation;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Notifications;

namespace GeneaGrab.Services
{
    internal partial class ToastNotificationsService : ActivationHandler<ToastNotificationActivatedEventArgs>
    {
        public void ShowToastNotification(ToastNotification toastNotification)
        {
            try { ToastNotificationManager.CreateToastNotifier().Show(toastNotification); }
            catch (Exception e) { Serilog.Log.Warning(e, "ToastNotification initialization failed"); }
        }

        protected override Task HandleInternalAsync(ToastNotificationActivatedEventArgs args)
        {
            //// TODO WTS: Handle activation from toast notification
            //// More details at https://docs.microsoft.com/windows/uwp/design/shell/tiles-and-notifications/send-local-toast
            return Task.CompletedTask;
        }
    }
}
