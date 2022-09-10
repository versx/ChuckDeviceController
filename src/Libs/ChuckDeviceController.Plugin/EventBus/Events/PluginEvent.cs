namespace ChuckDeviceController.Plugin.EventBus.Events
{
    // TODO: Test impl
    public class PluginEvent : IEvent
    {
        public string Payload { get; }

        public PluginEvent(string payload)
        {
            Payload = payload;
        }
    }
}