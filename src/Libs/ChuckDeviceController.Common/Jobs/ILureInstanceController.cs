namespace ChuckDeviceController.Common.Jobs;

/// <summary>
/// Manages lure pokemon encounters.
/// </summary>
public interface ILureInstanceController
{
    /// <summary>
    /// Gets a value determining whether lure encounters are enabled or not.
    /// </summary>
    public bool EnableLureEncounters { get; }
}