namespace ChuckDeviceController.Geometry.Models.Contracts
{
    public interface ICoordinate : IComparable
    {
        double Latitude { get; }

        double Longitude { get; }
    }
}