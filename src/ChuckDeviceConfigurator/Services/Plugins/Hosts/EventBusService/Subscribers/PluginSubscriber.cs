namespace ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService.Subscribers;

using ChuckDeviceController.Plugin.EventBus;
using ChuckDeviceController.Plugin.EventBus.Events;

/// <summary>
/// Plugin subscriber tracks only generic plugin events payloads.
/// </summary>
public class PluginSubscriber : ICustomObserver<PluginEvent>
{
    private readonly IDisposable _unsubscriber;

    public PluginSubscriber(IEventAggregatorHost eventAggregatorHost)
    {
        _unsubscriber = eventAggregatorHost.Subscribe(this);
    }

    public void OnNext(IEvent @event)
    {
        var pluginEvent = (PluginEvent)@event;
        Console.WriteLine($"{GetType().Name}: processing event with description '{pluginEvent.Payload}'.");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"{GetType().Name}: finished event processing.");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"{GetType().Name}: experienced error condition.");
    }

    public void Unsubscribe() => _unsubscriber.Dispose();
}