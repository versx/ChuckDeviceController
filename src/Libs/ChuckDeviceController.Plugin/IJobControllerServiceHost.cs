namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;

/// <summary>
/// Plugin host handler contract used to interact with and manage the
/// job controller service.
/// </summary>
public interface IJobControllerServiceHost : IInstanceServiceHost
{
    /// <summary>
    /// Gets a dictionary of active and configured devices.
    /// </summary>
    IReadOnlyDictionary<string, IDevice> Devices { get; }

    /// <summary>
    /// Gets a dictionary of all loaded job controller instances.
    /// </summary>
    IReadOnlyDictionary<string, IJobController> Instances { get; }

    /// <summary>
    /// Gets a list of all registered custom job controller instance types.
    /// </summary>
    //IReadOnlyList<string> CustomInstanceTypes { get; }
    IReadOnlyDictionary<string, GeofenceType> CustomInstanceTypes { get; }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="customInstanceType"></param>
    /// <returns></returns>
    Task RegisterJobControllerAsync<T>(string customInstanceType) where T : IJobController;

    /// <summary>
    /// Assigns the specified device to a specific job controller
    /// instance by name.
    /// </summary>
    /// <param name="device">Device entity.</param>
    /// <param name="instanceName">Job controller instance name.</param>
    Task AssignDeviceToJobControllerAsync(IDevice device, string instanceName);
}