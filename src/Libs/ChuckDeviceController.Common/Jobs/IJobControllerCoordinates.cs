namespace ChuckDeviceController.Common.Jobs;

using ChuckDeviceController.Geometry.Models.Abstractions;

public interface IJobControllerCoordinates
{
    IReadOnlyList<ICoordinate> Coordinates { get; }
}