namespace ChuckDeviceConfigurator.Services.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    // TODO: Find clusters to use with dynamic route
    // TODO: Check if Geofence type is Circle or Geofence
    // TODO: Add method signatures for List<MultiPolygon>, List<Geofence>, and List<Coordinate> as well as their singular variants

    public class RouteGenerator : IRouteGenerator
    {
        private const ushort DefaultCircleSize = 70;

        #region Variables

        //private static readonly Random _rand = new Random();
        private readonly IDbContextFactory<MapDataContext> _factory;

        #endregion

        #region Constructor

        public RouteGenerator(IDbContextFactory<MapDataContext> factory)
        {
            _factory = factory;
        }

        #endregion

        #region Public Methods

        public List<Coordinate> GenerateBootstrapRoute(List<MultiPolygon> multiPolygons, double circleSize = DefaultCircleSize)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in multiPolygons)
            {
                var coords = GenerateBootstrapRoute(multiPolygon, circleSize);
                coordinates.AddRange(coords);
            }
            return coordinates;
        }

        //public List<Coordinate> GenerateBootstrapRoute(Geofence geofence, double circleSize = 70)
        public List<Coordinate> GenerateBootstrapRoute(MultiPolygon multiPolygon, double circleSize = DefaultCircleSize)
        {
            var xMod = Math.Sqrt(0.75);
            var yMod = Math.Sqrt(0.568);
            var points = new List<Coordinate>();

            var polygon = multiPolygon.ConvertToCoordinates();
            //var line = geometryFactory.CreateLineString(polygon);
            var bbox = polygon.GetBoundingBox();
            var minLine = new Coordinate(bbox.MinimumLatitude, bbox.MinimumLongitude);
            //var maxLine = new Coordinate(bbox.MaximumLatitude, bbox.MaximumLongitude);
            //var line = new Coordinate(bbox.MaximumLatitude, bbox.MaximumLongitude);
            var minLat = bbox.MinimumLatitude;
            var minLon = bbox.MinimumLongitude;
            var maxLat = bbox.MaximumLatitude;
            var maxLon = bbox.MaximumLongitude;
            var currentLatLng = new Coordinate(maxLat, maxLon);
            var lastLatLng = new Coordinate(minLat, minLon);
            var startLatLng = GetDestination(currentLatLng, 90, circleSize * 1.5);
            var endLatLng = GetDestination(GetDestination(lastLatLng, 270, circleSize * 1.5), 180, circleSize);
            var row = 0;
            var heading = 270;
            var i = 0;

            while (currentLatLng.Latitude > endLatLng.Latitude)
            {
                do
                {
                    var distance = GetDistance(currentLatLng, minLine);
                    //var minDistance = GetDistance(currentLatLng, minLine);
                    //var maxDistance = GetDistance(currentLatLng, maxLine);
                    var isInGeofence = GeofenceService.IsPointInPolygon(currentLatLng, polygon);
                    if ((distance <= circleSize || distance == 0) && isInGeofence)
                    //if (minDistance <= circleSize && minDistance > 0 && polygon.Contains(currentLatLng) ||
                    //    mmaxDistance <= circleSize && mmaxDistance > 0)
                    {
                        points.Add(currentLatLng);
                    }
                    currentLatLng = GetDestination(currentLatLng, heading, xMod * circleSize * 2);
                    i++;
                } while ((heading == 270 && currentLatLng.Longitude > endLatLng.Longitude) || (heading == 90 && currentLatLng.Longitude < startLatLng.Longitude));

                currentLatLng = GetDestination(currentLatLng, 180, yMod * circleSize * 2);
                heading = row % 2 == 1
                    ? 270
                    : 90;
                currentLatLng = GetDestination(currentLatLng, heading, xMod * circleSize * 3);
                row++;
            }
            return points;
        }

        public List<Coordinate> GenerateRandomRoute(List<MultiPolygon> multiPolygons, int maxPoints = 500, double circleSize = DefaultCircleSize)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in multiPolygons)
            {
                var coords = GenerateRandomRoute(multiPolygon, maxPoints, circleSize);
                coordinates.AddRange(coords);
            }
            return coordinates;
        }

        public List<Coordinate> GenerateRandomRoute(MultiPolygon multiPolgyon, int maxPoints = 500, double circleSize = DefaultCircleSize)
        {
            var coordinates = multiPolgyon.ConvertToCoordinates();
            var routeCoords = Calculate(coordinates, maxPoints, circleSize);
            return routeCoords;
        }

        public List<Coordinate> GenerateOptimizedRoute(List<MultiPolygon> multiPolygons, double circleSize = DefaultCircleSize)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in multiPolygons)
            {
                var coords = GenerateOptimizedRoute(multiPolygon, circleSize);
                coordinates.AddRange(coords);
            }
            return coordinates;
        }

        public List<Coordinate> GenerateOptimizedRoute(MultiPolygon multiPolygon, double circleSize = DefaultCircleSize)
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

                spawnpoints.ForEach(x => coordinates.Add(new Coordinate(x.Latitude, x.Longitude)));
                pokestops.ForEach(x => coordinates.Add(new Coordinate(x.Latitude, x.Longitude)));
                gyms.ForEach(x => coordinates.Add(new Coordinate(x.Latitude, x.Longitude)));
                //cells.ForEach(x => list.Add(new Coordinate(x.Latitude, x.Longitude)));
                var s2cells = bbox.GetS2CellCoordinates();
                coordinates.AddRange(s2cells);
            }

            var coordsInArea = coordinates.Where(coord => GeofenceService.IsPointInPolygon(coord, polygon))
                                          .ToList();
            // TODO: Optimize spacing of coords for minimal overlap

            return coordsInArea;
        }

        #endregion

        public double GetDistance(Coordinate source, Coordinate destination)
        {
            double dx = source.Latitude - destination.Latitude;
            double dy = source.Longitude - destination.Longitude;
            return Math.Sqrt(dx * dx + dy * dy);
        }

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

        /// <summary>
        /// Notes: Expects geofence coordinates
        /// </summary>
        /// <param name="coords">List of coordinates for the geofence to calculate the route for.</param>
        /// <param name="maxPoints">Maximum amount of coordinate points to generate.</param>
        /// <param name="circleSize">The distance or spacing in meters between the previous and next coordinate generated.</param>
        /// <returns></returns>
        private static List<Coordinate> Calculate(List<Coordinate> coords, int maxPoints = 3000, double circleSize = 70)
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
                    //Latitude = (r.NextDouble() * ((maxLat - minLat) + (circleSize / 270))) + minLat,
                    //Longitude += (r.NextDouble() * ((maxLon - minLon) + (circleSize / 270))) + minLon,
                };
                if (GeofenceService.IsPointInPolygon(coord, coords))
                {
                    result.Add(coord);
                }

                /*
                do
                {
                    coord.Latitude = r.NextDouble() * (maxLat - minLat) + minLat;
                    coord.Longitude = r.NextDouble() * (maxLon - minLon) + minLon;
                    // Check if coord is within polygon
                } while (!GeofenceService.IsPointInPolygon(coord, coords));
                result.Add(coord);
                */
            }
            result.Sort((a, b) => a.Latitude.CompareTo(b.Latitude));
            return result;
        }

        /// <summary>
        /// Returns the point that is a distance and heading away from
        /// the given origin point.
        /// </summary>
        /// <param name="latlng">Origin coordinate</param>
        /// <param name="heading">Heading in degrees, clockwise from 0 degrees north.</param>
        /// <param name="distance">Distance in meters</param>
        /// <returns>The destination coordinate</returns>
        private static Coordinate GetDestination(Coordinate latlng, double heading, double distance)
        {
            heading = (heading + 360) % 360;
            const double rad = Math.PI / 180;
            const double radInv = 180 / Math.PI;
            const int r = 6378137; // approximation of Earth's radius
            var lon1 = latlng.Longitude * rad;
            var lat1 = latlng.Latitude * rad;
            var rheading = heading * rad;
            var sinLat1 = Math.Sin(lat1);
            var cosLat1 = Math.Cos(lat1);
            var cosDistR = Math.Cos(distance / r);
            var sinDistR = Math.Sin(distance / r);
            var lat2 = Math.Asin((sinLat1 * cosDistR) + (cosLat1 *
                    sinDistR * Math.Cos(rheading)));
            var lon2 = lon1 + Math.Atan2(Math.Sin(rheading) * sinDistR *
                    cosLat1, cosDistR - (sinLat1 * Math.Sin(lat2)));
            lon2 *= radInv;
            lon2 = lon2 > 180 ? lon2 - 360 : lon2 < -180 ? lon2 + 360 : lon2;
            return new Coordinate(lat2 * radInv, lon2);
        }

        #endregion
    }

    /*
    public class RouteOptimizer
    {
        private static readonly Random _rand = new Random();
        private readonly SpawnpointRepository _spawnpointsRepository;
        private readonly PokestopRepository _pokestopRepository;
        private readonly GymRepository _gymRepository;
        private readonly CellRepository _cellRepository;
        private readonly IReadOnlyList<Geofence> _geofences;

        public bool IncludeGyms { get; set; }

        public bool IncludePokestops { get; set; }

        public bool IncludeSpawnpoints { get; set; }

        public bool IncludeNests { get; set; }

        public bool OptimizePolygons { get; set; }

        public bool OptimizeCircles { get; set; }

        public RouteOptimizer()
        {
            _spawnpointsRepository = new SpawnpointRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _pokestopRepository = new PokestopRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _gymRepository = new GymRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _cellRepository = new CellRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
        }

        public RouteOptimizer(List<Geofence> geofences) : this()
        {
            _geofences = geofences;
        }

        public async Task<List<Coordinate>> GenerateRoute(bool optimize, ushort circleSize = 70, ushort optimizationAttempts = 1, bool tsp = false)
        {
            var result = new List<Coordinate>();
            foreach (var geofence in _geofences)
            {
                //var newCircle, currentLatLng, point;
                var bboxCoords = geofence.BBox.Coordinates;
                var minLat = bboxCoords.Min(x => x.X);
                var minLon = bboxCoords.Min(x => x.Y);
                var maxLat = bboxCoords.Max(x => x.X);
                var maxLon = bboxCoords.Max(x => x.Y);
                var bbox = new BoundingBox
                {
                    MinimumLatitude = minLat,
                    MinimumLongitude = minLon,
                    MaximumLatitude = maxLat,
                    MaximumLongitude = maxLon,
                };
                var center = new Coordinate((minLat + maxLat) / 2, (minLon + maxLon) / 2);
                var entities = await GetEntityLocations(bbox);
                // Filter all entities within geofence
                var filtered = entities.Where(x => RouteGenerator.IsPointInPolygon(x, (List<Coordinate>)geofence.Coordinates)).ToList();
                var radius = 550;//circleSize;
                foreach (var entity in entities)
                {
                    var distance = entity.DistanceTo(center);
                    if (distance <= 750)//radius)
                    {
                        result.Add(entity);
                    }
                }
            }
            return optimize || tsp
                ? GetOptimization(result, circleSize, optimizationAttempts, tsp)
                : result;
        }

        public async Task<List<Coordinate>> GetEntityLocations(BoundingBox bbox)
        {
            var list = new List<Coordinate>();
            // TODO: Include S2Cells
            if (IncludeGyms)
            {
                var gyms = await _gymRepository.GetAllAsync(bbox);
                list.AddRange(gyms.Select(x => new Coordinate(x.Latitude, x.Longitude)));
            }
            if (IncludePokestops)
            {
                var pokestops = await _pokestopRepository.GetAllAsync(bbox);
                list.AddRange(pokestops.Select(x => new Coordinate(x.Latitude, x.Longitude)));
            }
            if (IncludeSpawnpoints)
            {
                var spawnpoints = await _spawnpointsRepository.GetAllAsync(bbox);
                list.AddRange(spawnpoints.Select(x => new Coordinate(x.Latitude, x.Longitude)));
            }
            if (IncludeNests)
            {
            }
            return list;
        }

        private static List<Coordinate> GetOptimization(List<Coordinate> points, ushort circleSize = 750, ushort optimizationAttempts = 1, bool tsp = true)
        {
            if (points.Count == 0)
            {
                throw new Exception("Invalid points set");
            }
            var bestAttempt = new List<Coordinate>();
            for (var i = 0; i < optimizationAttempts; i++)
            {
                points.Shuffle();
                var workingGyms = points;
                var attempt = new List<Coordinate>();
                while (workingGyms.Count > 0)
                {
                    var gym1 = workingGyms.FirstOrDefault();
                    workingGyms.Remove(gym1);
                    for (var j = 0; j < workingGyms.Count; j++)
                    {
                        var gym2 = workingGyms[j];
                        var dist = Haversine(gym1, gym2);
                        if (dist < circleSize)
                        {
                            workingGyms.RemoveAt(j);
                        }
                    }
                    attempt.Add(gym1);
                }
                if (bestAttempt.Count == 0 || attempt.Count < bestAttempt.Count)
                {
                    bestAttempt = attempt;
                }
            }
            if (tsp)
            {
                var workingGyms = bestAttempt;
                var index = _rand.Next(0, workingGyms.Count - 1);
                var finalAttempt = new List<Coordinate>();
                var distances = new Dictionary<double, Coordinate>();
                while (workingGyms.Count > 0)
                {
                    var gym1 = workingGyms[index];
                    workingGyms.RemoveAt(index);
                    finalAttempt.Add(gym1);
                    //workingGyms.RemoveAt(index); // TODO: Probably don't need this, no idea why they'd do it twice lol :thinking:
                    for (var i = 0; i < workingGyms.Count; i++)
                    {
                        var gym2 = workingGyms[i];
                        var dist = Haversine(gym1, gym2);
                        if (!distances.ContainsKey(dist))
                        {
                            distances.Add(dist, gym2);
                        }
                        while (dist == 0)
                        {
                            dist++;
                        }
                        distances[dist] = gym2;
                        index = i;
                    }
                    //ksort(distances);
                    //
                    distances = new Dictionary<double, Coordinate>
                    (
                        from pair in distances
                        orderby pair.Value ascending
                        select pair
                    );
                    //
                    var closestGym = distances.FirstOrDefault();
                    distances.Remove(closestGym.Key);
                    gym1 = closestGym.Value;
                }
                bestAttempt = finalAttempt;
            }
            return bestAttempt;
        }

        private static double Haversine(Coordinate coord1, Coordinate coord2)
        {
            var r = 6371000;
            var latFrom = (coord1.Latitude * Math.PI / 180);
            var lngFrom = (coord1.Longitude * Math.PI / 180);
            var latTo = (coord2.Latitude * Math.PI / 180);
            var lngTo = (coord2.Longitude * Math.PI / 180);
            var latDelta = latTo - latFrom;
            var lngDelta = lngTo - lngFrom;
            var a = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(latDelta / 2), 2) +
                Math.Cos(latFrom) * Math.Cos(latTo) * Math.Pow(Math.Sin(lngDelta / 2), 2)));
            return a * r;
        }
    }
    */
}