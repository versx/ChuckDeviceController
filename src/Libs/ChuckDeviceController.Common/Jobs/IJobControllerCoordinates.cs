namespace ChuckDeviceController.Common.Jobs;

using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// Job controller uses coordinates instead of geofences.
/// </summary>
public interface IJobControllerCoordinates
{
    /// <summary>
    /// Gets a list of coordinate objects.
    /// </summary>
    IReadOnlyList<ICoordinate> Coordinates { get; }
}