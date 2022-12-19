namespace ChuckDeviceController.Routing.Utilities;

using ChuckDeviceController.Geometry.Models.Abstractions;

internal static class GeoUtils
{
    public static int CompareCoordinates(ICoordinate coord1, ICoordinate coord2)
    {
        var d1 = Math.Pow(coord1.Latitude, 2) + Math.Pow(coord1.Longitude, 2);
        var d2 = Math.Pow(coord2.Latitude, 2) + Math.Pow(coord2.Longitude, 2);
        return d1.CompareTo(d2);
    }

    public static double GetDistanceFromPoint(ICoordinate point1, ICoordinate point2)
    {
        const double EarthsRadius = 6378137.0;
        if (point1 == point2)
        {
            return 0.0;
        }

        var lat1 = ConvertDegreesToRadians(point1.Latitude);
        var lon1 = ConvertDegreesToRadians(point1.Longitude);
        var lat2 = ConvertDegreesToRadians(point2.Latitude);
        var lon2 = ConvertDegreesToRadians(point2.Longitude);

        var a = Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1);
        if (a > 1.0)
        {
            return 0.0;
        }

        return Math.Acos(a) * EarthsRadius;
    }

    private static double ConvertDegreesToRadians(double value)
    {
        return value * Math.PI / 180;
    }
}