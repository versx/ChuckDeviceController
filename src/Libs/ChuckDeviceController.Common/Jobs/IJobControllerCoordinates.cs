namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Common.Geometry;

    public interface IJobControllerCoordinates
    {
        IReadOnlyList<ICoordinate> Coordinates { get; }
    }
}