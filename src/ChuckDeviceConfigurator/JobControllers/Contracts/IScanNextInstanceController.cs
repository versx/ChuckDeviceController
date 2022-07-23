namespace ChuckDeviceConfigurator.JobControllers.Contracts
{
    using ChuckDeviceController.Geometry.Models;

    public interface IScanNextInstanceController
    {
        Queue<Coordinate> ScanNextCoordinates { get; }
    }
}