namespace ChuckDeviceController.Common.Jobs
{
    public interface IScanNextInstanceController
    {
        Queue<ICoordinate> ScanNextCoordinates { get; }
    }
}