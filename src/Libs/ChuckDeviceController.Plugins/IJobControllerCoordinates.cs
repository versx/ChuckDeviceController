namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common;

    public interface IJobControllerCoordinates
    {
        IReadOnlyList<ICoordinate> Coordinates { get; }
    }
}