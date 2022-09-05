namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Common.Geometry;

    public interface IScanNextInstanceController
    {
        Queue<ICoordinate> ScanNextCoordinates { get; }
    }
}