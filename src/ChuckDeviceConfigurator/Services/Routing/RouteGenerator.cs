namespace ChuckDeviceConfigurator.Services.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    // TODO: Find clusters to use with dynamic route

    public class RouteGenerator : IRouteGenerator
    {
        private const ushort DefaultCircleSize = Strings.DefaultCircleSize; // 70m

        #region Variables

        private readonly IDbContextFactory<MapDataContext> _factory;

        #endregion

        #region Constructor

        public RouteGenerator(IDbContextFactory<MapDataContext> factory)
        {
            _factory = factory;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a route using the provided route generation options.
        /// </summary>
        /// <param name="options">Routing generation </param>
        /// <returns>Returns a list of the generated route.</returns>
        /// <exception cref="Exception"></exception>
        public List<Coordinate> GenerateRoute(RouteGeneratorOptions options)
        {
            var coordinates = new List<Coordinate>();
            var geofences = options.MultiPolygons;
            var maxPoints = options.MaximumPoints;
            var circleSize = options.CircleSize;

            if ((geofences?.Count ?? 0) == 0)
            {
                throw new Exception($"Unable to generate route, at least one geofence is required.");
            }

            switch (options.RouteType)
            {
                case RouteGenerationType.Bootstrap:
                    var bootstrapRoute = GenerateBootstrapRoute(geofences!, circleSize);
                    coordinates.AddRange(bootstrapRoute);
                    break;
                case RouteGenerationType.Randomized:
                    var randomRoute = GenerateRandomRoute(geofences!, maxPoints, circleSize);
                    coordinates.AddRange(randomRoute);
                    break;
                case RouteGenerationType.Optimized:
                    var optimizedRoute = GenerateOptimizedRoute(geofences!, circleSize);
                    coordinates.AddRange(optimizedRoute);
                    break;
                default:
                    throw new Exception($"Invalid route generation type specified: {options.RouteType}");
            }
            return coordinates;
        }

        #endregion

        #region Route Generator Methods

        private List<Coordinate> GenerateBootstrapRoute(List<MultiPolygon> multiPolygons, double circleSize = DefaultCircleSize)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in multiPolygons)
            {
                var coords = GenerateBootstrapRoute(multiPolygon, circleSize);
                coordinates.AddRange(coords);
            }
            return coordinates;
        }

        private List<Coordinate> GenerateBootstrapRoute(MultiPolygon multiPolygon, double circleSize = DefaultCircleSize)
        {
            var xMod = Math.Sqrt(0.75);
            var yMod = Math.Sqrt(0.568);
            var points = new List<Coordinate>();

            var polygon = multiPolygon.ConvertToCoordinates();
            var bbox = polygon.GetBoundingBox();
            //var line = geometryFactory.CreateLineString(polygon);
            var minLine = new Coordinate(bbox.MinimumLatitude, bbox.MinimumLongitude);
            var minLat = bbox.MinimumLatitude;
            var minLon = bbox.MinimumLongitude;
            var maxLat = bbox.MaximumLatitude;
            var maxLon = bbox.MaximumLongitude;
            var currentLatLng = new Coordinate(maxLat, maxLon);
            var lastLatLng = new Coordinate(minLat, minLon);
            var startLatLng = GetNextCoordinate(currentLatLng, 90, circleSize * 1.5);
            var endLatLng = GetNextCoordinate(GetNextCoordinate(lastLatLng, 270, circleSize * 1.5), 180, circleSize);
            var row = 0;
            var heading = 270;
            var i = 0;

            while (currentLatLng.Latitude > endLatLng.Latitude)
            {
                do
                {
                    var distance = GetDistance(currentLatLng, minLine);
                    var isInGeofence = GeofenceService.IsPointInPolygon(currentLatLng, polygon);
                    if ((distance <= circleSize || distance == 0) && isInGeofence)
                    {
                        points.Add(currentLatLng);
                    }
                    currentLatLng = GetNextCoordinate(currentLatLng, heading, xMod * circleSize * 2);
                    i++;
                } while ((heading == 270 && currentLatLng.Longitude > endLatLng.Longitude) || (heading == 90 && currentLatLng.Longitude < startLatLng.Longitude));

                currentLatLng = GetNextCoordinate(currentLatLng, 180, yMod * circleSize * 2);
                heading = row % 2 == 1
                    ? 270
                    : 90;
                currentLatLng = GetNextCoordinate(currentLatLng, heading, xMod * circleSize * 3);
                row++;
            }
            return points;
        }

        private List<Coordinate> GenerateRandomRoute(List<MultiPolygon> multiPolygons, uint maxPoints = 500, double circleSize = DefaultCircleSize)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in multiPolygons)
            {
                var coords = GenerateRandomRoute(multiPolygon, maxPoints, circleSize);
                coordinates.AddRange(coords);
            }
            return coordinates;
        }

        private List<Coordinate> GenerateRandomRoute(MultiPolygon multiPolgyon, uint maxPoints = 500, double circleSize = DefaultCircleSize)
        {
            var coordinates = multiPolgyon.ConvertToCoordinates();
            var routeCoords = Calculate(coordinates, maxPoints, circleSize);
            return routeCoords;
        }

        private List<Coordinate> GenerateOptimizedRoute(List<MultiPolygon> multiPolygons, double circleSize = DefaultCircleSize)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in multiPolygons)
            {
                var coords = GenerateOptimizedRoute(multiPolygon, circleSize);
                coordinates.AddRange(coords);
            }
            return coordinates;
        }

        private List<Coordinate> GenerateOptimizedRoute(MultiPolygon multiPolygon, double circleSize = DefaultCircleSize)
        {
            var polygon = multiPolygon.ConvertToCoordinates();
            var bbox = polygon.GetBoundingBox();
            var minLat = bbox.MinimumLatitude;
            var minLon = bbox.MinimumLongitude;
            var maxLat = bbox.MaximumLatitude;
            var maxLon = bbox.MaximumLongitude;

            var coordinates = new List<Coordinate>();
            using (var context = _factory.CreateDbContext())
            {
                var spawnpoints = context.Spawnpoints.Where(spawn =>
                    spawn.Latitude >= minLat &&
                    spawn.Longitude >= minLon &&
                    spawn.Latitude <= maxLat &&
                    spawn.Longitude <= maxLon &&
                    spawn.DespawnSecond != null
                ).ToList();
                var pokestops = context.Pokestops.Where(stop =>
                    stop.Latitude >= minLat &&
                    stop.Longitude >= minLon &&
                    stop.Latitude <= maxLat &&
                    stop.Longitude <= maxLon
                ).ToList();
                var gyms = context.Gyms.Where(gym =>
                    gym.Latitude >= minLat &&
                    gym.Longitude >= minLon &&
                    gym.Latitude <= maxLat &&
                    gym.Longitude <= maxLon
                ).ToList();
                var cells = context.Cells.Where(cell =>
                    cell.Latitude >= minLat &&
                    cell.Longitude >= minLon &&
                    cell.Latitude <= maxLat &&
                    cell.Longitude <= maxLon
                ).ToList();

                spawnpoints.ForEach(x => coordinates.Add(x.ToCoordinate()));
                pokestops.ForEach(x => coordinates.Add(x.ToCoordinate()));
                gyms.ForEach(x => coordinates.Add(x.ToCoordinate()));
                //cells.ForEach(x => list.Add(x.ToCoordinate()));
                var s2cells = bbox.GetS2CellCoordinates();
                coordinates.AddRange(s2cells);
            }

            var coordsInArea = coordinates.Where(coord => GeofenceService.IsPointInPolygon(coord, polygon))
                                          .ToList();
            // TODO: Optimize spacing of coords for minimal overlap

            return coordsInArea;
        }

        #endregion

        #region Private Methods

        /*
        private static List<Coordinate> FilterCoordinates(List<Coordinate> coordinates, ushort stepDistance)
        {
            var list = new List<Coordinate>();
            foreach (var coord in coordinates)
            {
                // Coordinate is geofenced if in one geofenced area
                if (GeofenceService.IsPointInPolygon(coord, coordinates))
                {
                    list.Add(coord);
                    continue;
                }

                // Do a check if the radius is in the geofence even if the center is not
                var count = _rand.Next(0, 6);
                for (var i = 0; i < count; i++)
                {
                    var startLocation = GetNewCoord(coord, stepDistance, 90 + (60 * i));
                    if (GeofenceService.IsPointInPolygon(startLocation, coordinates))
                    {
                        list.Add(coord);
                        break;
                    }
                }
            }
            return list;
        }

        private static Coordinate GetNewCoord(Coordinate start, double distance, double bearing)
        {
            //var destination = 
            return null;
        }
        */

        private static double GetDistance(Coordinate source, Coordinate destination)
        {
            var dx = source.Latitude - destination.Latitude;
            var dy = source.Longitude - destination.Longitude;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Notes: Expects geofence coordinates
        /// </summary>
        /// <param name="coords">List of coordinates for the geofence to calculate the route for.</param>
        /// <param name="maxPoints">Maximum amount of coordinate points to generate.</param>
        /// <param name="circleSize">The distance or spacing in meters between the previous and next coordinate generated.</param>
        /// <returns></returns>
        private static List<Coordinate> Calculate(List<Coordinate> coords, uint maxPoints = 3000, double circleSize = 70)
        {
            var bbox = coords.GetBoundingBox();
            var minLat = bbox.MinimumLatitude;
            var minLon = bbox.MinimumLongitude;
            var maxLat = bbox.MaximumLatitude;
            var maxLon = bbox.MaximumLongitude;
            var rand = new Random();
            var result = new List<Coordinate>();
            for (var i = 0; i < maxPoints; i++)
            {
                var coord = new Coordinate
                {
                    Latitude = rand.NextDouble() * (maxLat - minLat) + minLat,
                    Longitude = rand.NextDouble() * (maxLon - minLon) + minLon,
                    //Latitude = (rand.NextDouble() * ((maxLat - minLat) + (circleSize / 270))) + minLat,
                    //Longitude = (rand.NextDouble() * ((maxLon - minLon) + (circleSize / 270))) + minLon,
                };
                if (GeofenceService.IsPointInPolygon(coord, coords))
                {
                    result.Add(coord);
                }
            }
            //result.Sort((a, b) => a.Latitude.CompareTo(b.Latitude));
            result.Sort(Utils.CompareCoordinates);
            return result;
        }

        /// <summary>
        /// Returns the point that is a distance and heading away from
        /// the given origin point.
        /// </summary>
        /// <param name="coordinate">Origin coordinate</param>
        /// <param name="heading">Heading in degrees, clockwise from 0 degrees north.</param>
        /// <param name="distance">Distance in meters</param>
        /// <returns>The destination coordinate</returns>
        private static Coordinate GetNextCoordinate(Coordinate coordinate, double heading, double distance)
        {
            heading = (heading + 360) % 360;
            const double earthRadius = 6378137; // Approximation of Earth's radius
            const double rad = Math.PI / 180;
            const double radInv = 180 / Math.PI;
            var lat1 = coordinate.Latitude * rad;
            var lon1 = coordinate.Longitude * rad;
            var rheading = heading * rad;
            var sinLat1 = Math.Sin(lat1);
            var cosLat1 = Math.Cos(lat1);
            var cosDistR = Math.Cos(distance / earthRadius);
            var sinDistR = Math.Sin(distance / earthRadius);
            var latAngle = Math.Asin((sinLat1 * cosDistR) + (cosLat1 *
                sinDistR * Math.Cos(rheading)));
            var lonAngle = lon1 + Math.Atan2(Math.Sin(rheading) * sinDistR *
                cosLat1, cosDistR - (sinLat1 * Math.Sin(latAngle)));
            lonAngle *= radInv;
            lonAngle = lonAngle > 180 ? lonAngle - 360 : lonAngle < -180 ? lonAngle + 360 : lonAngle;
            var coord = new Coordinate(latAngle * radInv, lonAngle);
            return coord;
        }

        #endregion
    }
}