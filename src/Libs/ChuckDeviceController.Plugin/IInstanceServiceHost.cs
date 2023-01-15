namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Common.Abstractions;

/// <summary>
/// Instance service interface contract used to create new instances.
/// </summary>
public interface IInstanceServiceHost
{
    /// <summary>
    /// Creates a new instance in the database.
    /// </summary>
    /// <param name="options">Options used to create the new instance.</param>
    Task CreateInstanceAsync(IInstance options);
}