namespace ChuckDeviceController.Common.Jobs
{
    public interface IJobControllerCoordinates
    {
        IReadOnlyList<ICoordinate> Coordinates { get; }
    }
}