namespace ChuckDeviceController.Common.Geometry
{
    public interface ICoordinate : IComparable
    {
        double Latitude { get; }

        double Longitude { get; }
    }
}