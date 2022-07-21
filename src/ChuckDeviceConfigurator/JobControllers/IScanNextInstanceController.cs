namespace ChuckDeviceConfigurator.JobControllers
{
    using ChuckDeviceController.Geometry.Models;

    public interface IScanNextInstanceController
    {
        Queue<Coordinate> ScanNextCoordinates { get; }
    }
}