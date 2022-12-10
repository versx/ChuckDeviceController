namespace ChuckDeviceController.Plugin.EventBus.Observer
{
    using ChuckDeviceController.Plugin.EventBus.Events;

    /// <summary>
    /// 
    /// </summary>
    public class PluginObserver : ICustomObserver<PluginEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnCompleted()
        {
            Console.WriteLine($"TestObserver - OnCompleted");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            Console.WriteLine($"TestObserver - OnError: {error}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void OnNext(PluginEvent value)
        {
            Console.WriteLine($"TestObserver - OnNext: {value.Payload}");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Unsubscribe()
        {
            Console.WriteLine($"TestObserver - Unsubscribe");
        }
    }
}