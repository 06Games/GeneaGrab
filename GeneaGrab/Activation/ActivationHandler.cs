using System.Threading.Tasks;

namespace GeneaGrab.Activation
{
    // For more information on understanding and extending activation flow see https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/UWP/activation.md
    internal abstract class ActivationHandler
    {
        public abstract bool CanHandle(object args);
        public abstract Task HandleAsync(object args);
    }

    /// <summary>Extend this class to implement new ActivationHandlers</summary>
    internal abstract class ActivationHandler<T> : ActivationHandler where T : class
    {
        /// <summary>Override this method to add the activation logic in your activation handler</summary>
        protected abstract Task HandleInternalAsync(T args);
        public override Task HandleAsync(object args) => HandleInternalAsync(args as T);
        /// <summary>CanHandle checks the args is of type you have configured</summary>
        public override bool CanHandle(object args) => args is T && CanHandleInternal(args as T);
        /// <summary>You can override this method to add extra validation on activation args to determine if your ActivationHandler should handle this activation args</summary>
        protected virtual bool CanHandleInternal(T args) => true;
    }
}
