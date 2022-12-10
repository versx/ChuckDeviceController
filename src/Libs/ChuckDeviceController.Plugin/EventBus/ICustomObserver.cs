namespace ChuckDeviceController.Plugin.EventBus
{
    /// <summary>
    /// Typed observer implementation.
    /// </summary>
    /// <typeparam name="TEvent">
    /// The <seealso cref="IEvent"/> event type the observer should expect.
    /// </typeparam>
    public interface ICustomObserver<TEvent> : IObserver<TEvent>
        where TEvent : IEvent
    {
        /// <summary>
        /// Unsubscribes from the inherited <seealso cref="IEvent"/>
        /// type indicated.
        /// </summary>
        void Unsubscribe();
    }
}