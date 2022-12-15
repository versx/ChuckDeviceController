namespace ChuckDeviceController.Plugin.EventBus
{
    /// <summary>
    /// An interface contract for publishing data to the event bus
    /// aggregator service which all subscribers will receive.
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publish data to all subscriptions via event bus aggregator host.
        /// </summary>
        /// <param name="payload">Payload data to publish to subscribers.</param>
        void Publish(string payload);
    }
}