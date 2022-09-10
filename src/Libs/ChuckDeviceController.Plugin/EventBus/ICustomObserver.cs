namespace ChuckDeviceController.Plugin.EventBus
{
    /// <summary>
    /// Typed observer implementation.
    /// </summary>
    /// <typeparam name="T">
    /// The <seealso cref="IEvent"/> event type the observer should expect.
    /// </typeparam>
    public interface ICustomObserver<T> : IObserver<IEvent>
        where T : IEvent
    {
        /// <summary>
        /// Unsubscribes from the inherited <seealso cref="IEvent"/>
        /// type indicated.
        /// </summary>
        void Unsubscribe();
    }
}