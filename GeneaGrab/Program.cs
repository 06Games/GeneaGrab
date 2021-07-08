using System;
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

            // Define a key for this instance, based on some app-specific logic.
            // If the key is always unique, then the app will never redirect. If the key is always non-unique, then the app will always redirect to the first instance.
            // In practice, the app should produce a key that is sometimes unique and sometimes not, depending on its own needs.
            string key = activatedArgs.Kind == ActivationKind.Protocol ? "Link" : Guid.NewGuid().ToString();

            var instance = AppInstance.FindOrRegisterInstanceForKey(key);
            if (instance.IsCurrentInstance) Windows.UI.Xaml.Application.Start((p) => new App()); // If we successfully registered this instance, we can now just go ahead and do normal XAML initialization.
            else instance.RedirectActivationTo(); // Some other instance has registered for this key, so we'll redirect this activation to that instance instead.
        }
    }
}
