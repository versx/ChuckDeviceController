namespace ChuckDeviceController.Plugin.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICustomObserver<T> : IObserver<IEvent>
        where T : IEvent
    {
        /// <summary>
        /// 
        /// </summary>
        void Unsubscribe();
    }
}