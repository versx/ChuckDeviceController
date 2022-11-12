namespace ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService.Publishers
{
    using ChuckDeviceController.Plugin.EventBus;
    using ChuckDeviceController.Plugin.EventBus.Events;

    /// <summary>
    /// Creates a wrapper class for publishing data to the event bus
    /// aggregator service which all subscriptions will receive.
    /// </summary>
    public class PluginPublisher : IPublisher
    {
        private readonly IEventAggregatorHost _eventAggregatorHost;

        /// <summary>
        /// Instantiates a new wrapper instance implementation of <seealso cref="IPublisher"/>.
        /// </summary>
        /// <param name="eventAggregatorHost">The event bus aggregator host implementation.</param>
        public PluginPublisher(IEventAggregatorHost eventAggregatorHost)
        {
            _eventAggregatorHost = eventAggregatorHost;
        }

        /// <summary>
        /// Publish data to all subscriptions via event bus aggregator host.
        /// </summary>
        /// <param name="payload">Payload data to publish to subscribers.</param>
        public void Publish(string payload)
        {
            _eventAggregatorHost.Publish(new PluginEvent(payload));
        }
    }
}