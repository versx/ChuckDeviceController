namespace ChuckDeviceController.Plugin.EventBus.Observer
{
    using ChuckDeviceController.Plugin.EventBus.Events;

    public class TestObserver : ICustomObserver<PluginEvent>
    {
        public void OnCompleted()
        {
            Console.WriteLine($"TestObserver - OnCompleted");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"TestObserver - OnError: {error}");
        }

        public void OnNext(IEvent value)
        {
            Console.WriteLine($"TestObserver - OnNext: {value.Payload}");
        }

        public void Unsubscribe()
        {
            Console.WriteLine($"TestObserver - Unsubscribe");
        }
    }
}