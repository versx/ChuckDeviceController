namespace ChuckDeviceConfigurator.Extensions
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public static class GeofenceExtensions
    {
        public static List<Coordinate> ConvertToCoordinates(this IReadOnlyList<Geofence> geofences)
        {
            var coords = new List<Coordinate>();
            foreach (var geofence in geofences)
            {
                var area = geofence.Data?.Area;
                if (area is null)
                {
                    Console.WriteLine($"Failed to parse geofence '{geofence.Name}' coordinates");
                    continue;
                }
                string areaJson = Convert.ToString(area);
                var coordsArray = (List<Coordinate>)
                (
                    area is List<Coordinate>
                        ? area
                        : areaJson.FromJson<List<Coordinate>>()
                );
                coords.AddRange(coordsArray);
            }
            return coords;
        }

        public static List<Coordinate> ConvertToCoordinates(this Geofence geofence)
        {
            var coords = new List<Coordinate>();
            var area = geofence.Data?.Area;
            if (area is null)
            {
                Console.WriteLine($"Failed to parse geofence '{geofence.Name}' coordinates");
                return null;
            }
            string areaJson = Convert.ToString(area);
            var coordsArray = (List<Coordinate>)
            (
                area is List<Coordinate>
                    ? area
                    : areaJson.FromJson<List<Coordinate>>()
            );
            coords.AddRange(coordsArray);
            return coords;
        }

        public static (List<MultiPolygon>, List<List<Coordinate>>) ConvertToMultiPolygons(
            this IReadOnlyList<Geofence> geofences)
        {
            var multiPolygons = new List<MultiPolygon>();
            var coordinates = new List<List<Coordinate>>();
            foreach (var geofence in geofences)
            {
                var area = geofence.Data?.Area;
                if (area is null)
                {
                    Console.WriteLine($"Failed to parse coordinates for geofence '{geofence.Name}'");
                    continue;
                }
                string areaJson = Convert.ToString(area);
                var coordsArray = (List<List<Coordinate>>)
                (
                    area is List<List<Coordinate>>
                        ? area
                        : areaJson.FromJson<List<List<Coordinate>>>()
                );
                coordinates.AddRange(coordsArray);

                var areaArrayEmptyInner = new List<MultiPolygon>();
                foreach (var coord in coordsArray)
                {
                    var multiPolygon = new MultiPolygon();
                    Coordinate? first = null;
                    for (var i = 0; i < coord.Count; i++)
                    {
                        if (i == 0)
                        {
                            first = coord[i];
                        }
                        multiPolygon.Add(new Polygon(coord[i].Latitude, coord[i].Longitude));
                    }
                    if (first != null)
                    {
                        multiPolygon.Add(new Polygon(first.Latitude, first.Longitude));
                    }
                    areaArrayEmptyInner.Add(multiPolygon);
                }
                multiPolygons.AddRange(areaArrayEmptyInner);
            }
            return (multiPolygons, coordinates);
        }

        public static (List<MultiPolygon>, List<List<Coordinate>>) ConvertToMultiPolygons(
            this Geofence geofence)
        {
            var multiPolygons = new List<MultiPolygon>();
            var coordinates = new List<List<Coordinate>>();

            var area = geofence.Data?.Area;
            if (area is null)
            {
                Console.WriteLine($"Failed to parse coordinates for geofence '{geofence.Name}'");
                return default;
            }
            string areaJson = Convert.ToString(area);
            var coordsArray = (List<List<Coordinate>>)
            (
                area is List<List<Coordinate>>
                    ? area
                    : areaJson.FromJson<List<List<Coordinate>>>()
            );
            coordinates.AddRange(coordsArray);

            var areaArrayEmptyInner = new List<MultiPolygon>();
            foreach (var coord in coordsArray)
            {
                var multiPolygon = new MultiPolygon();
                Coordinate? first = null;
                for (var i = 0; i < coord.Count; i++)
                {
                    if (i == 0)
                    {
                        first = coord[i];
                    }
                    multiPolygon.Add(new Polygon(coord[i].Latitude, coord[i].Longitude));
                }
                if (first != null)
                {
                    multiPolygon.Add(new Polygon(first.Latitude, first.Longitude));
                }
                areaArrayEmptyInner.Add(multiPolygon);
            }
            multiPolygons.AddRange(areaArrayEmptyInner);
            return (multiPolygons, coordinates);
        }
    }
}