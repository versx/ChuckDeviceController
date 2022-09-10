namespace ChuckDeviceController.Plugin.EventBus.Events
{
    public class PluginEvent : IEvent
    {
        public string Payload { get; }

        public PluginEvent(string payload)
        {
            Payload = payload;
        }
    }
}