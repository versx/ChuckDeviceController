namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Geometry.Models.Contracts;

    public interface IJobControllerCoordinates
    {
        IReadOnlyList<ICoordinate> Coordinates { get; }
    }
}