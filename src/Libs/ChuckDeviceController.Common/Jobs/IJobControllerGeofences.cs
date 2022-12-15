namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Geometry.Models.Contracts;

    public interface IJobControllerGeofences
    {
        IReadOnlyList<IMultiPolygon> MultiPolygons { get; }
    }
}