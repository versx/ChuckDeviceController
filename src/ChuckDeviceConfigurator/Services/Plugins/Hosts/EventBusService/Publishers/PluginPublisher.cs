namespace ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService.Publishers
{
    using ChuckDeviceController.Plugin.EventBus;
    using ChuckDeviceController.Plugin.EventBus.Events;

    public class PluginPublisher : IPublisher
    {
        private readonly IEventAggregatorHost _eventAggregatorHost;

        public PluginPublisher(IEventAggregatorHost eventAggregatorHost)
        {
            _eventAggregatorHost = eventAggregatorHost;
        }

        public void Publish(string payload)
        {
            _eventAggregatorHost.Publish(new PluginEvent(payload));
        }
    }
}