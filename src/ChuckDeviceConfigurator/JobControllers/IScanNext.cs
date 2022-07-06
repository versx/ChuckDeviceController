namespace ChuckDeviceConfigurator.JobControllers
{
    using ChuckDeviceController.Geometry.Models;

    public interface IScanNext
    {
        Queue<Coordinate> ScanNextCoordinates { get; }
    }
}