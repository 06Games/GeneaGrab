using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

namespace GeneaGrab
{
    public static class Program
    {
        static void Main(string[] _)
        {
            IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

            // If the Windows shell indicates a recommended instance, then the app can choose to redirect this activation to that instance instead.
            if (AppInstance.RecommendedInstance != null)
            {
                AppInstance.RecommendedInstance.RedirectActivationTo();
                return;
            }

            AppInstance instance = null;
            if (activatedArgs.Kind != ActivationKind.Launch) instance = AppInstance.GetInstances().FirstOrDefault();
            if (instance is null) instance = AppInstance.FindOrRegisterInstanceForKey(Guid.NewGuid().ToString());

            if (instance.IsCurrentInstance) Windows.UI.Xaml.Application.Start((p) => new App()); // If we successfully registered this instance, we can now just go ahead and do normal XAML initialization.
            else instance.RedirectActivationTo(); // Some other instance has registered for this key, so we'll redirect this activation to that instance instead.
        }
    }
}
