namespace ChuckDeviceController.Common.Jobs;

using ChuckDeviceController.Geometry.Models.Abstractions;

public interface IScanNextInstanceController
{
    Queue<ICoordinate> ScanNextCoordinates { get; }
}