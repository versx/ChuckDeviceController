namespace ChuckDeviceController.Geometry.Models.Contracts
{
    public interface IPolygon : IList<double>
    {
        ICoordinate ToCoordinate();
    }
}