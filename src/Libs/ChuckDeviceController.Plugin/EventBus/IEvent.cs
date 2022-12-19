namespace ChuckDeviceController.Plugin.EventBus;

/// <summary>
/// Interface contract for event bus events.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Gets or sets the payload data included in the event.
    /// </summary>
    string Payload { get; }
}