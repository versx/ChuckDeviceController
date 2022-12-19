namespace ChuckDeviceController.Plugin.EventBus;

/// <summary>
/// 
/// </summary>
public interface IEventAggregatorHost : IObservable<IEvent>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    void Publish(IEvent message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    IDisposable Subscribe(ICustomObserver<IEvent> observer);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="observer"></param>
    /// <returns></returns>
    IDisposable Subscribe<T>(ICustomObserver<T> observer)
        where T : IEvent;
}