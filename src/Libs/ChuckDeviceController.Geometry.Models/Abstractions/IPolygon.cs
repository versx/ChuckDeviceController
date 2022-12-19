namespace ChuckDeviceController.Geometry.Models.Abstractions;

public interface IPolygon : IList<double>
{
    ICoordinate ToCoordinate();
}