namespace ChuckDeviceController.Plugin.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        void Publish(string payload);
    }
}