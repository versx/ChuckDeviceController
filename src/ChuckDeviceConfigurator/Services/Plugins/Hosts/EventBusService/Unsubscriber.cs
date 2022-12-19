namespace ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Plugin.EventBus;

/// <summary>
/// Enables a subscriber to unsubscribe from further events before
/// disposing of the object.
/// </summary>
public class Unsubscriber<T> : IDisposable
    where T : IEvent
{
    #region Variables

    private readonly SafeCollection<IObserver<T>> _observers;
    private readonly IObserver<T> _observerToUnsubscribe;
    private bool _isDisposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Instantiates a disposable instance a subscriber can use to unsubscribe from events.
    /// </summary>
    /// <param name="observers">Registered observer instances.</param>
    /// <param name="observerToUnsubscribe">Observers instances to unsubscribe from.</param>
    public Unsubscriber(SafeCollection<IObserver<T>> observers, IObserver<T> observerToUnsubscribe)
    {
        _observers = observers;
        _observerToUnsubscribe = observerToUnsubscribe;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of the object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the object.
    /// </summary>
    /// <param name="disposing">
    /// Indicates if the disposing process has already started or not.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            if (_observers.Contains(_observerToUnsubscribe))
            {
                _observers.Remove(_observerToUnsubscribe);
                Console.WriteLine($"Observer has unsubsribed from event bus events.");
            }
        }

        _isDisposed = true;
    }

    #endregion
}