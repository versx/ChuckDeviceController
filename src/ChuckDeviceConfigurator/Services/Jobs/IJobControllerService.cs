namespace ChuckDeviceConfigurator.Services.Jobs;

using POGOProtos.Rpc;

using ChuckDeviceConfigurator.Services.Rpc.Models;
using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Plugin;

/// <summary>
/// Service to manage all <see cref="IJobController"/> instances.
/// </summary>
public interface IJobControllerService : IJobControllerServiceHost, IQueueManager
{
    #region Public

    /// <summary>
    /// Load devices from database and cache internally.
    /// </summary>
    /// <param name="serviceProvider">Service provider instance.</param>
    void LoadDevices(ServiceProvider serviceProvider);

    /// <summary>
    /// Starts the <see cref="IJobControllerService"/>.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the <see cref="IJobControllerService"/>.
    /// </summary>
    void Stop();

    #endregion

    #region Instances

    /// <summary>
    /// Retrieves the job instance controller assigned to the provided
    /// device.
    /// </summary>
    /// <param name="uuid">
    /// Device universally unique identifier to search for.
    /// </param>
    /// <returns>
    /// Returns the job instance controller assigned to the device.
    /// </returns>
    IJobController GetInstanceController(string uuid);

    /// <summary>
    /// Retrieves the job instance controller by name.
    /// </summary>
    /// <param name="instanceName">Name of the job instance controller.</param>
    /// <returns>Returns the job instance controller by name.</returns>
    IJobController GetInstanceControllerByName(string instanceName);

    /// <summary>
    /// Gets the status for the provided instance.
    /// </summary>
    /// <param name="instance">The instance to get the status for.</param>
    /// <returns>Returns the text value of the instance status.</returns>
    Task<string> GetStatusAsync(Instance instance);

    /// <summary>
    /// Adds the instance to the instance cache and initializes a job
    /// controller instance from it.
    /// </summary>
    /// <param name="instance">The instance to add.</param>
    Task AddInstanceAsync(Instance instance);

    /// <summary>
    /// Reloads the specified instance in the cache with newest version.
    /// </summary>
    /// <param name="newInstance">New instance to add to the cache.</param>
    /// <param name="oldInstanceName">Old instance name to remove from the cache.</param>
    Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName);

    /// <summary>
    /// Reloads the specified instance in the cache.
    /// </summary>
    /// <param name="instanceName">Name of instance to reload.</param>
    Task ReloadInstanceAsync(string instanceName);

    /// <summary>
    /// Reloads all job controller instances.
    /// </summary>
    void ReloadAllInstances();

    /// <summary>
    /// Removes the job controller instance by name from the cache.
    /// </summary>
    /// <param name="instanceName">Instance name to remove.</param>
    Task RemoveInstanceAsync(string instanceName);

    #endregion

    #region Devices

    /// <summary>
    /// Adds the device to the device cache.
    /// </summary>
    /// <param name="device">Device to add to cache.</param>
    void AddDevice(Device device);

    /// <summary>
    /// Removes the device from the device cache.
    /// </summary>
    /// <param name="device">Device to remove from cache.</param>
    Task RemoveDeviceAsync(Device device);

    /// <summary>
    /// Removes the device from the device cache by device UUID.
    /// </summary>
    /// <param name="uuid">Device UUID to remove from cache.</param>
    void RemoveDevice(string uuid);

    /// <summary>
    /// Reloads the specified device in the cache with newest version.
    /// </summary>
    /// <param name="device">New device to add to the cache.</param>
    /// <param name="oldDeviceUuid">Old device UUID to remove from the cache.</param>
    /// <summary>
    void ReloadDevice(Device device, string oldDeviceUuid);

    /// <summary>
    /// Gets a list of device UUIDs assigned to specified instance.
    /// </summary>
    /// <param name="instanceName">
    /// Instance name to retrieve assigned devices from.
    /// </param>
    /// <returns>Returns a list of device UUIDs assigned to instance.</returns>
    IEnumerable<string> GetDeviceUuidsInInstance(string instanceName);

    #endregion

    #region Receivers

    /// <summary>
    /// Informs all Pokemon IV job controller instances that a Pokemon has been
    /// scanned/discovered. Whether the Pokemon has IV or not will determine if
    /// it is removed from the IV queue or if it is added to it depending if it
    /// is in the desirable IV scan list.
    /// </summary>
    /// <param name="pokemon">Pokemon to inform Pokemon IV job controllers of.</param>
    /// <param name="hasIv">Whether the Pokemon has IV set or not.</param>
    void GotPokemon(Pokemon pokemon, bool hasIv);

    /// <summary>
    /// Inserts received nearby fort data for leveling job controller instance to use.
    /// </summary>
    /// <param name="fort">Fort proto data to insert.</param>
    /// <param name="username">Account username that received fort proto data.</param>
    void GotFort(PokemonFortProto fort, string username);

    /// <summary>
    /// Assigns trainer stats to leveling job controller instance to keep track of
    /// progressed trainer level progress while leveling up.
    /// </summary>
    /// <param name="username">Account username to assign trainer stats.</param>
    /// <param name="level">Current trainer level.</param>
    /// <param name="xp">Current trainer experience points.</param>
    void GotPlayerInfo(string username, ushort level, ulong xp);

    /// <summary>
    /// Gets a value determining whether to store the leveling data found by trainer
    /// </summary>
    /// <param name="username">Trainer username to get value from</param>
    /// <returns>
    /// Returns information about the trainers leveling status
    /// be stored.
    /// </returns>
    TrainerLevelingStatus GetTrainerLevelingStatus(string username);

    #endregion
}