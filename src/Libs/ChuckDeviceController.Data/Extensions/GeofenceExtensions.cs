namespace ChuckDeviceController.Data.Extensions;

using Microsoft.Extensions.Logging;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Logging;

public static class GeofenceExtensions
{
    private static readonly ILogger<Geofence> _logger =
        GenericLoggerFactory.CreateLogger<Geofence>();

    public static string? ConvertToIni(this IGeofence geofence)
    {
        var sb = new System.Text.StringBuilder();
        switch (geofence.Type)
        {
            case GeofenceType.Circle:
                {
                    var coords = geofence.ConvertToCoordinates();
                    if (coords == null)
                    {
                        _logger.LogError($"Error: Unable to convert coordinates to INI format");
                        return null;
                    }

                    sb.AppendLine($"[{geofence.Name}]");
                    foreach (var coord in coords)
                    {
                        sb.AppendLine($"{coord.Latitude},{coord.Longitude}");
                    }
                    break;
                }
            case GeofenceType.Geofence:
                {
                    var (_, coordinates) = geofence.ConvertToMultiPolygons();
                    foreach (var coords in coordinates)
                    {
                        sb.AppendLine($"[{geofence.Name}]");
                        foreach (var coord in coords)
                        {
                            sb.AppendLine($"{coord.Latitude},{coord.Longitude}");
                        }
                    }
                    break;
                }
        }
        return sb.ToString();
    }

    public static IReadOnlyList<ICoordinate> ConvertToCoordinates(this IReadOnlyList<IGeofence> geofences)
    {
        var coords = new List<ICoordinate>();
        foreach (var geofence in geofences)
        {
            var result = ConvertToCoordinates(geofence);
            if (result == null)
                continue;

            coords.AddRange(result);
        }
        return coords;
    }

    public static IReadOnlyList<ICoordinate>? ConvertToCoordinates(this IGeofence geofence)
    {
        if (geofence == null)
        {
            _logger.LogError($"Provided geofence was null, unable to convert to Coordinates list");
            return default;
        }

        var coordsArray = ParseGeofenceArea<List<Coordinate>>(geofence.Name, geofence?.Data?.Area);
        if (coordsArray == null)
        {
            _logger.LogError($"Failed to parse Coordinates list from geofence");
            return null;
        }
        var coords = new List<ICoordinate>(coordsArray);
        return coords;
    }

    public static (IReadOnlyList<IMultiPolygon>, IReadOnlyList<IReadOnlyList<Coordinate>>) ConvertToMultiPolygons(
        this IReadOnlyList<IGeofence> geofences)
    {
        var multiPolygons = new List<IMultiPolygon>();
        var coordinates = new List<IReadOnlyList<Coordinate>>();
        foreach (var geofence in geofences)
        {
            var result = ConvertToMultiPolygons(geofence);
            if (result.Item1 == null || result.Item2 == null)
                continue;

            multiPolygons.AddRange(result.Item1);
            coordinates.AddRange(result.Item2);
        }
        return (multiPolygons, coordinates);
    }

    public static (IReadOnlyList<IMultiPolygon>, IReadOnlyList<IReadOnlyList<Coordinate>>) ConvertToMultiPolygons(
        this IGeofence geofence)
    {
        if (geofence == null)
        {
            _logger.LogError($"Provided geofence was null, unable to convert to MultiPolygons list");
            return default;
        }

        var coordsArray = ParseGeofenceArea<List<List<Coordinate>>>(geofence.Name, geofence?.Data?.Area);
        if (coordsArray == null)
        {
            _logger.LogError($"Failed to parse MultiPolygon coordinates from geofence");
            return default;
        }

        var coordinates = new List<List<Coordinate>>(coordsArray);
        var areaArrayEmptyInner = new List<MultiPolygon>();
        foreach (var coordList in coordsArray)
        {
            var multiPolygon = new MultiPolygon();
            Coordinate? first = null;
            Coordinate? last = null;
            for (var i = 0; i < coordList.Count; i++)
            {
                var coord = coordList[i];
                if (i == 0)
                    first = coord;
                else if (i == coordList.Count - 1)
                    last = coord;

                multiPolygon.Add(new Polygon(coord.Latitude, coord.Longitude));
            }
            // Check if the first and last coordinates are not null and are the same, if
            // not add the first coordinate to the end of the list
            if (first != null && last != null && first.CompareTo(last) != 0)
            {
                // Insert first coordinate at the end of the list
                multiPolygon.Add(new Polygon(first.Latitude, first.Longitude));
            }
            areaArrayEmptyInner.Add(multiPolygon);
        }
        var multiPolygons = new List<IMultiPolygon>(areaArrayEmptyInner);
        return (multiPolygons, coordinates);
    }

    private static T? ParseGeofenceArea<T>(string geofenceName, dynamic area)
    {
        try
        {
            if (area is null)
            {
                _logger.LogError($"Failed to parse coordinates for geofence '{geofenceName}'");
                return default;
            }
            //string areaJson = JsonExtensions.ToJson(area);
            //string areaJson = Convert.ToString(area.ToJson());
            string areaJson = area;
            var coordsArray = (T?)
            (
                area is T
                    ? area
                    : areaJson.FromJson<T>()
            );
            return coordsArray;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ParseGeofenceArea: {ex}");
            return default;
        }
    }
}