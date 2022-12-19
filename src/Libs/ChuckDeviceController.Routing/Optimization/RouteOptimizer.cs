namespace ChuckDeviceController.Routing.Optimization;

using System.Security.Cryptography;

using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry;
using ChuckDeviceController.Geometry.Extensions;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;

public class RouteOptimizer : IRouteOptimizer
{
    #region Variables

    private static readonly RandomNumberGenerator _rand = RandomNumberGenerator.Create();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether to include
    /// Gym locations when optimizing the route.
    /// </summary>
    public bool IncludeGyms { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include
    /// Pokestop locations when optimizing the route.
    /// </summary>
    public bool IncludePokestops { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include
    /// Spawnpoint locations when optimizing the route.
    /// </summary>
    public bool IncludeSpawnpoints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include
    /// S2 Cell locations when optimizing the route.
    /// </summary>
    public bool IncludeS2Cells { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include
    /// Nest locations when optimizing the route.
    /// </summary>
    public bool IncludeNests { get; set; }

    /// <summary>
    /// Gets or sets the geofencing boundaries used when
    /// optimizing the route.
    /// </summary>
    public IReadOnlyList<MultiPolygon> MultiPolygons { get; set; }

    #endregion

    #region Constructor

    public RouteOptimizer(List<MultiPolygon> multiPolygons)
    {
        MultiPolygons = multiPolygons;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Optimizes the provides route using the specified route
    /// optimization options.
    /// </summary>
    /// <param name="options">Route optimization options to use.</param>
    /// <returns>Returns the optimized route.</returns>
    public async Task<List<ICoordinate>> OptimizeRouteAsync(RouteOptimizerOptions options)
    {
        var coordinates = new List<ICoordinate>();
        foreach (var multiPolygon in MultiPolygons)
        {
            //var newCircle, currentLatLng, point;
            var coords = multiPolygon.ToCoordinates();
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
            var optimizedRoute = GetOptimization(coordinates, options.RadiusM, options.OptimizationAttempts, options.OptimizeTsp);
            return optimizedRoute;
        }

        return coordinates;
    }

    #endregion

    #region Private Methods

    private async Task<List<ICoordinate>> GetEntityCoordinatesAsync(IBoundingBox bbox)
    {
        var coordinates = new List<ICoordinate>();
        // TODO: Fetch interested locations or provide them via constructor/options
        //if (IncludeGyms)
        //{
        //    var gymCoords = context.Gyms
        //        .AsEnumerable()
        //        .Where(gym => bbox.IsInBoundingBox(gym.Latitude, gym.Longitude))
        //        .Select(gym => gym.ToCoordinate())
        //        .ToList();
        //    coordinates.AddRange(gymCoords);
        //}
        //if (IncludePokestops)
        //{
        //    var pokestopCoords = context.Pokestops
        //        .AsEnumerable()
        //        .Where(stop => bbox.IsInBoundingBox(stop.Latitude, stop.Longitude))
        //        .Select(stop => stop.ToCoordinate())
        //        .ToList();
        //    coordinates.AddRange(pokestopCoords);
        //}
        //if (IncludeSpawnpoints)
        //{
        //    var spawnpointCoords = context.Spawnpoints
        //        .AsEnumerable()
        //        .Where(spawn => bbox.IsInBoundingBox(spawn.Latitude, spawn.Longitude))
        //        .Select(spawn => spawn.ToCoordinate())
        //        .ToList();
        //    coordinates.AddRange(spawnpointCoords);
        //}
        //if (IncludeS2Cells)
        //{
        //    var cellCoords = context.Cells
        //        .AsEnumerable()
        //        .Where(cell => bbox.IsInBoundingBox(cell.Latitude, cell.Longitude))
        //        .Select(cell => cell.ToCoordinate())
        //        .ToList();
        //    coordinates.AddRange(cellCoords);
        //}
        //if (IncludeNests)
        //{
        //}

        var coordsInArea = coordinates
            .Where(coord => GeofenceService.InMultiPolygon((List<IMultiPolygon>)MultiPolygons, coord))
            .ToList();

        return await Task.FromResult(coordsInArea);
    }

    private static List<ICoordinate> GetOptimization(List<ICoordinate> coordinates, ushort circleSize = 750, ushort optimizationAttempts = 1, bool tsp = true)
    {
        if ((coordinates?.Count ?? 0) == 0)
        {
            throw new Exception("Invalid coordinates set");
        }

        var bestAttempt = new List<ICoordinate>();
        for (var i = 0; i < optimizationAttempts; i++)
        {
            var coords = new List<ICoordinate>(coordinates!);
            coords.Shuffle();

            var attempt = new List<ICoordinate>();
            while (coords.Count > 0)
            {
                var coord1 = coords.FirstOrDefault()!;
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

    private static List<ICoordinate> TspCoordinates(List<ICoordinate> coordinates)
    {
        var result = new List<ICoordinate>();
        var index = _rand.Next(0, coordinates.Count);
        var distances = new Dictionary<double, ICoordinate>();

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
            distances = new Dictionary<double, ICoordinate>
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

    private static double Haversine(ICoordinate coord1, ICoordinate coord2)
    {
        const int radius = 6378137; // approximation of Earth's radius
        //var radius = 6371000;
        var latFrom = coord1.Latitude * Math.PI / 180;
        var lngFrom = coord1.Longitude * Math.PI / 180;
        var latTo = coord2.Latitude * Math.PI / 180;
        var lngTo = coord2.Longitude * Math.PI / 180;
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