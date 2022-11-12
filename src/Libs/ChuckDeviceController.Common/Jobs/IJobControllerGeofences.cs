namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Common.Geometry;

    public interface IJobControllerGeofences
    {
        IReadOnlyList<IMultiPolygon> MultiPolygons { get; }
    }
}