namespace ChuckDeviceController.Plugin.EventBus.Events
{
    /// <summary>
    /// Plugin event for event bus service.
    /// </summary>
    public class PluginEvent : IEvent
    {
        /// <summary>
        /// Gets the event payload.
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload">Plugin event payload to send.</param>
        public PluginEvent(string payload)
        {
            Payload = payload;
        }
    }
}