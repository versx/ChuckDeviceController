namespace ChuckDeviceController.Services.Routes
{
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Geofence;
    using ChuckDeviceController.Geofence.Models;
    using Google.Common.Geometry;
    using NetTopologySuite.Geometries;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Coordinate = ChuckDeviceController.Data.Entities.Coordinate;

    public class RouteGenerator
    {
        #region Variables

        private static readonly Random _rand = new Random();
        private readonly SpawnpointRepository _spawnpointsRepository = null;
        private readonly PokestopRepository _pokestopRepository = null;
        private readonly GymRepository _gymRepository = null;
        private readonly CellRepository _cellRepository = null;

        #endregion

        #region Singleton

        private static RouteGenerator _instance;
        public static RouteGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RouteGenerator();
                }
                return _instance;
            }
        }

        #endregion

        public List<Coordinate> GenerateBootstrapRoute(List<Geofence> geofences, double circleSize = 70)
        {
            List<Coordinate> list = new List<Coordinate>();
            geofences.ForEach(geofence =>
                list.AddRange(GenerateBootstrapRoute(geofence, circleSize)));
            return list;
        }

        public List<Coordinate> GenerateBootstrapRoute(Geofence geofence, double circleSize = 70)
        {
            GeometryFactory geometryFactory = GeometryFactory.Default;
            double xMod = Math.Sqrt(0.75);
            double yMod = Math.Sqrt(0.568);
            List<Coordinate> points = new List<Coordinate>();

            NetTopologySuite.Geometries.Coordinate[] polygon = geofence.Feature.Geometry.Coordinates;
            LineString line = geometryFactory.CreateLineString(polygon);
            NetTopologySuite.Geometries.Coordinate[] coords = geofence.BBox.Coordinates;
            double minLat = coords.Min(x => x.X);
            double minLon = coords.Min(x => x.Y);
            double maxLat = coords.Max(x => x.X);
            double maxLon = coords.Max(x => x.Y);
            NetTopologySuite.Geometries.Coordinate currentLatLng = new NetTopologySuite.Geometries.Coordinate(maxLat, maxLon);
            NetTopologySuite.Geometries.Coordinate lastLatLng = new NetTopologySuite.Geometries.Coordinate(minLat, minLon);
            NetTopologySuite.Geometries.Coordinate startLatLng = Destination(currentLatLng, 90, circleSize * 1.5);
            NetTopologySuite.Geometries.Coordinate endLatLng = Destination(Destination(lastLatLng, 270, circleSize * 1.5), 180, circleSize);
            int row = 0;
            int heading = 270;
            int i = 0;
            while (currentLatLng.X > endLatLng.X)
            {
                do
                {
                    Point point = new Point(currentLatLng);
                    double distance = point.Distance(line);
                    if (distance <= circleSize || distance == 0 || polygon.Contains(currentLatLng))
                    {
                        points.Add(new Coordinate(currentLatLng.X, currentLatLng.Y));
                    }
                    currentLatLng = Destination(currentLatLng, heading, (xMod * circleSize * 2));
                    i++;
                } while ((heading == 270 && currentLatLng.Y > endLatLng.Y) || (heading == 90 && currentLatLng.Y < startLatLng.Y));

                currentLatLng = Destination(currentLatLng, 180, yMod * circleSize * 2);
                heading = row % 2 == 1
                    ? 270
                    : 90;
                currentLatLng = Destination(currentLatLng, heading, xMod * circleSize * 3);
                row++;
            }
            return points;
        }

        public List<Coordinate> GenerateRandomRoute(Geofence geofence, int maxPoints = 3000, double circleSize = 70)
        {
            NetTopologySuite.Geometries.Coordinate[] coords = geofence.BBox.Coordinates;
            return Calculate
            (
                new Coordinate(coords[0].X, coords[0].Y),
                new Coordinate(coords[1].X, coords[1].Y),
                new Coordinate(coords[2].X, coords[2].Y),
                new Coordinate(coords[3].X, coords[3].Y),
                maxPoints,
                circleSize
            );
        }

        public async Task<List<Coordinate>> GenerateOptimizedRoute(Geofence geofence, double circleSize = 70)
        {
            NetTopologySuite.Geometries.Coordinate[] polygon = geofence.Feature.Geometry.Coordinates;
            //var line = geometryFactory.CreateLineString(polygon);
            NetTopologySuite.Geometries.Coordinate[] bboxCoords = geofence.BBox.Coordinates;
            double minLat = bboxCoords.Min(x => x.X);
            double minLon = bboxCoords.Min(x => x.Y);
            double maxLat = bboxCoords.Max(x => x.X);
            double maxLon = bboxCoords.Max(x => x.Y);
            BoundingBox bbox = new BoundingBox
            {
                MinimumLatitude = minLat,
                MinimumLongitude = minLon,
                MaximumLatitude = maxLat,
                MaximumLongitude = maxLon,
            };
            List<Data.Entities.Spawnpoint> spawnpoints = (await _spawnpointsRepository.GetAllAsync()).ToList();
            List<Data.Entities.Pokestop> pokestops = await _pokestopRepository.GetAllAsync(bbox);
            List<Data.Entities.Gym> gyms = await _gymRepository.GetAllAsync(bbox);
            List<Data.Entities.Cell> cells = await _cellRepository.GetAllAsync(bbox);
            List<Coordinate> list = new List<Coordinate>();
            spawnpoints.ForEach(x => list.Add(new Coordinate(x.Latitude, x.Longitude)));
            pokestops.ForEach(x => list.Add(new Coordinate(x.Latitude, x.Longitude)));
            gyms.ForEach(x => list.Add(new Coordinate(x.Latitude, x.Longitude)));
            //cells.ForEach(x => list.Add(new Coordinate(x.Latitude, x.Longitude)));
            List<Coordinate> s2cells = GetS2Cells(bbox);
            list.AddRange(s2cells);
            // TODO: Filter if within geofence area
            return list;
        }

        private static List<Coordinate> GetS2Cells(BoundingBox bbox)
        {
            S2RegionCoverer regionCoverer = new S2RegionCoverer
            {
                MinLevel = 15,
                MaxLevel = 15,
                //MaxCells = 100,
            };
            S2LatLngRect region = new S2LatLngRect(
                S2LatLng.FromDegrees(bbox.MinimumLatitude, bbox.MinimumLongitude),
                S2LatLng.FromDegrees(bbox.MaximumLatitude, bbox.MaximumLongitude)
            );
            S2CellUnion cellIds = regionCoverer.GetCovering(region);
            List<Coordinate> list = new List<Coordinate>();
            foreach (S2CellId cellId in cellIds)
            {
                S2LatLng center = cellId.ToLatLng();
                list.Add(new Coordinate(center.LatDegrees, center.LngDegrees));
            }
            // TODO: Check if point is within geofence
            //var filtered = FilterCoordinates(coordinates);
            //return filtered;
            return list;
        }

        private static List<Coordinate> FilterCoordinates(List<Coordinate> coordinates, ushort stepDistance)
        {
            List<Coordinate> list = new List<Coordinate>();
            foreach (Coordinate coord in coordinates)
            {
                // Coordinate is geofenced if in one geofenced area
                if (GeofenceService.IsPointInPolygon(coord, coordinates))
                {
                    list.Add(coord);
                    continue;
                }

                // Do a check if the radius is in the geofence even if the center is not
                int count = _rand.Next(0, 6);
                for (int i = 0; i < count; i++)
                {
                    Coordinate startLocation = GetNewCoord(coord, stepDistance, 90 + 60 * i);
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

        private static List<Coordinate> Calculate(Coordinate location1, Coordinate location2, Coordinate location3, Coordinate location4, int maxPoints = 3000, double circleSize = 70)
        {
            List<Coordinate> allCoords = new List<Coordinate> { location1, location2, location3, location4 };
            double minLat = allCoords.Min(x => x.Latitude);
            double minLon = allCoords.Min(x => x.Longitude);
            double maxLat = allCoords.Max(x => x.Latitude);
            double maxLon = allCoords.Max(x => x.Longitude);

            Random r = new Random();
            List<Coordinate> result = new List<Coordinate>();
            for (int i = 0; i < maxPoints; i++)
            {
                Coordinate point = new Coordinate();
                do
                {
                    //point.Latitude = r.NextDouble() * (maxLat - minLat) + minLat;
                    //point.Longitude = r.NextDouble() * (maxLon - minLon) + minLon;
                    point.Latitude = r.NextDouble() * ((maxLat - minLat) + circleSize / 270) + minLat;
                    point.Longitude += r.NextDouble() * ((maxLon - minLon) + circleSize / 270) + minLon;
                } while (!GeofenceService.IsPointInPolygon(point, allCoords));
                result.Add(point);
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
        private static NetTopologySuite.Geometries.Coordinate Destination(NetTopologySuite.Geometries.Coordinate latlng, double heading, double distance)
        {
            heading = (heading + 360) % 360;
            double rad = Math.PI / 180;
            double radInv = 180 / Math.PI;
            int r = 6378137; // approximation of Earth's radius
            double lon1 = latlng.Y * rad;
            double lat1 = latlng.X * rad;
            double rheading = heading * rad;
            double sinLat1 = Math.Sin(lat1);
            double cosLat1 = Math.Cos(lat1);
            double cosDistR = Math.Cos(distance / r);
            double sinDistR = Math.Sin(distance / r);
            double lat2 = Math.Asin(sinLat1 * cosDistR + cosLat1 *
                    sinDistR * Math.Cos(rheading));
            double lon2 = lon1 + Math.Atan2(Math.Sin(rheading) * sinDistR *
                    cosLat1, cosDistR - sinLat1 * Math.Sin(lat2));
            lon2 *= radInv;
            lon2 = lon2 > 180 ? lon2 - 360 : lon2 < -180 ? lon2 + 360 : lon2;
            return new NetTopologySuite.Geometries.Coordinate(lat2 * radInv, lon2);
        }
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