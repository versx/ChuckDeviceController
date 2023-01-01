namespace ChuckDeviceController.Common.Jobs;

using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// Job controller uses geofences instead of coordinates.
/// </summary>
public interface IJobControllerGeofences
{
    /// <summary>
    /// Gets a list of geofence objects.
    /// </summary>
    IReadOnlyList<IMultiPolygon> MultiPolygons { get; }
}