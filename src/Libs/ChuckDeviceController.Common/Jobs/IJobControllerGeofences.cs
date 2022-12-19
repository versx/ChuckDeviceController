namespace ChuckDeviceController.Common.Jobs;

using ChuckDeviceController.Geometry.Models.Abstractions;

public interface IJobControllerGeofences
{
    IReadOnlyList<IMultiPolygon> MultiPolygons { get; }
}