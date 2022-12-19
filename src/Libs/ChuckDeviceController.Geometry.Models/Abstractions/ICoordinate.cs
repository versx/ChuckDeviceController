namespace ChuckDeviceController.Geometry.Models.Abstractions;

public interface ICoordinate : IComparable
{
    double Latitude { get; }

    double Longitude { get; }
}