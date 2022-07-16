namespace ChuckDeviceConfigurator.Services.Routing
{
    using System.Security.Cryptography;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class RouteOptimizer : IRouteOptimizer
    {
        #region Variables

        private static readonly RandomNumberGenerator _rand = RandomNumberGenerator.Create();
        private readonly IDbContextFactory<MapDataContext> _factory;

        #endregion

        #region Properties

        public bool IncludeGyms { get; set; }

        public bool IncludePokestops { get; set; }

        public bool IncludeSpawnpoints { get; set; }

        public bool IncludeS2Cells { get; set; }

        public bool IncludeNests { get; set; }

        public bool OptimizePolygons { get; set; }

        public bool OptimizeCircles { get; set; }

        /// <summary>
        /// Gets or sets the Geofencing boundaries
        /// </summary>
        public IReadOnlyList<MultiPolygon> MultiPolygons { get; set; }

        #endregion

        #region Constructor

        public RouteOptimizer(IDbContextFactory<MapDataContext> factory, List<MultiPolygon> multiPolygons)
        {
            _factory = factory;
            MultiPolygons = multiPolygons;
        }

        #endregion

        #region Public Methods

        public async Task<List<Coordinate>> OptimizeRouteAsync(RouteOptimizerOptions options)
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in MultiPolygons)
            {
                //var newCircle, currentLatLng, point;
                var coords = multiPolygon.ConvertToCoordinates();
                var bbox = multiPolygon.GetBoundingBox();
                /*
                var centerCoord = new Coordinate
                (
                    (bbox.MinimumLatitude + bbox.MaximumLatitude) / 2,
                    (bbox.MinimumLongitude + bbox.MaximumLongitude) / 2
                );
                */
                var enityCoords = await GetEntityCoordinatesAsync(bbox);
                // Filter all entities within geofence
                var filtered = enityCoords.Where(coord => GeofenceService.IsPointInPolygon(coord, coords))
                                          .ToList();
                //var radius = 3750;//550;//options.CircleSize;
                foreach (var entityCoord in filtered)
                {
                    //var distance = entityCoord.DistanceTo(centerCoord);
                    //if (distance <= radius)
                    {
                        coordinates.Add(entityCoord);
                    }
                }
            }

            // TODO: Check if coords touching gym/stop/spawnpoint or within x meters, if not remove it from route

            if (options.OptimizeTsp)
            {
                var optimizedRoute = GetOptimization(coordinates, options.CircleSize, options.OptimizationAttempts, options.OptimizeTsp);
                return optimizedRoute;
            }

            return coordinates;
        }

        #endregion

        #region Private Methods

        private async Task<List<Coordinate>> GetEntityCoordinatesAsync(BoundingBox bbox)
        {
            var coordinates = new List<Coordinate>();
            using (var context = _factory.CreateDbContext())
            {
                if (IncludeGyms)
                {
                    var gymCoords = context.Gyms.AsEnumerable()
                                                .Where(gym => bbox.IsInBoundingBox(gym.Latitude, gym.Longitude))
                                                .Select(gym => new Coordinate(gym.Latitude, gym.Longitude))
                                                .ToList();
                    coordinates.AddRange(gymCoords);
                }
                if (IncludePokestops)
                {
                    var pokestopCoords = context.Pokestops.AsEnumerable()
                                                          .Where(stop => bbox.IsInBoundingBox(stop.Latitude, stop.Longitude))
                                                          .Select(stop => new Coordinate(stop.Latitude, stop.Longitude))
                                                          .ToList();
                    coordinates.AddRange(pokestopCoords);
                }
                if (IncludeSpawnpoints)
                {
                    var spawnpointCoords = context.Spawnpoints.AsEnumerable()
                                                              .Where(spawn => bbox.IsInBoundingBox(spawn.Latitude, spawn.Longitude))
                                                              .Select(spawn => new Coordinate(spawn.Latitude, spawn.Longitude))
                                                              .ToList();
                    coordinates.AddRange(spawnpointCoords);
                }
                if (IncludeS2Cells)
                {
                    var cellCoords = context.Cells.AsEnumerable()
                                                  .Where(cell => bbox.IsInBoundingBox(cell.Latitude, cell.Longitude))
                                                  .Select(cell => new Coordinate(cell.Latitude, cell.Longitude))
                                                  .ToList();
                    coordinates.AddRange(cellCoords);
                }
                if (IncludeNests)
                {
                }
            }

            var coordsInArea = coordinates.Where(coord => GeofenceService.InMultiPolygon((List<MultiPolygon>)MultiPolygons, coord))
                                            .ToList();

            return await Task.FromResult(coordsInArea);
        }

        private static List<Coordinate> GetOptimization(List<Coordinate> coordinates, ushort circleSize = 750, ushort optimizationAttempts = 1, bool tsp = true)
        {
            if (coordinates?.Count == 0)
            {
                throw new Exception("Invalid coordinates set");
            }

            var bestAttempt = new List<Coordinate>();
            for (var i = 0; i < optimizationAttempts; i++)
            {
                var coords = coordinates;
                coords.Shuffle();

                var attempt = new List<Coordinate>();
                while (coords.Count > 0)
                {
                    var coord1 = coords.FirstOrDefault();
                    coords.Remove(coord1);
                    for (var j = 0; j < coords.Count; j++)
                    {
                        var coord2 = coords[j];
                        //var dist = Haversine(coord1, coord2);
                        var dist = coord1.DistanceTo(coord2);
                        if (dist < circleSize)
                        {
                            coords.RemoveAt(j);
                        }
                    }
                    attempt.Add(coord1);
                }

                if ((bestAttempt.Count == 0 || attempt.Count < bestAttempt.Count) && attempt.Count > 0)
                {
                    bestAttempt = attempt;
                }
            }

            if (tsp)
            {
                var tspCoords = TspCoordinates(bestAttempt);
                return tspCoords;
            }
            return bestAttempt;
        }

        private static List<Coordinate> TspCoordinates(List<Coordinate> coordinates)
        {
            var result = new List<Coordinate>();
            var index = _rand.Next(0, coordinates.Count);
            var distances = new Dictionary<double, Coordinate>();

            while (coordinates.Count > 0)
            {
                var coord1 = coordinates[index];
                coordinates.RemoveAt(index);
                result.Add(coord1);

                for (var i = 0; i < coordinates.Count; i++)
                {
                    var coord2 = coordinates[i];
                    //var dist = Haversine(coord1, coord2);
                    var dist = coord1.DistanceTo(coord2);
                    if (!distances.ContainsKey(dist))
                    {
                        distances.Add(dist, coord2);
                    }
                    if (dist == 0)
                    {
                        dist++;
                    }
                    distances[dist] = coord2;
                    index = i;
                }

                //ksort(distances);
                distances = new Dictionary<double, Coordinate>
                (
                    from pair in distances
                    orderby pair.Value ascending
                    select pair
                );

                var (closestDistance, closestCoord) = distances.FirstOrDefault();
                distances.Remove(closestDistance);
                coord1 = closestCoord;
            }
            return result;
        }

        private static double Haversine(Coordinate coord1, Coordinate coord2)
        {
            const int radius = 6378137; // approximation of Earth's radius
            //var radius = 6371000;
            var latFrom = (coord1.Latitude * Math.PI / 180);
            var lngFrom = (coord1.Longitude * Math.PI / 180);
            var latTo = (coord2.Latitude * Math.PI / 180);
            var lngTo = (coord2.Longitude * Math.PI / 180);
            var latDelta = latTo - latFrom;
            var lngDelta = lngTo - lngFrom;
            var latSin = Math.Pow(Math.Sin(latDelta / 2), 2);
            var lngSin = Math.Pow(Math.Sin(lngDelta / 2), 2);
            var sqrt = Math.Sqrt(latSin + Math.Cos(latFrom) * Math.Cos(latTo) * lngSin);
            var angle = 2 * Math.Asin(sqrt);
            var value = angle * radius;
            return value;
        }

        #endregion
    }
}