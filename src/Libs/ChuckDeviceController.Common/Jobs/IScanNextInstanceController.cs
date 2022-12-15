namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Geometry.Models.Contracts;

    public interface IScanNextInstanceController
    {
        Queue<ICoordinate> ScanNextCoordinates { get; }
    }
}