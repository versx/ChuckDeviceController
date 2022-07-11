namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Models;

    // TODO: Make 'OnlyUnknownSpawnpoints' configurable
    // TODO: Make 'OptimizeRoute' configurable

    public class TthFinderInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<TthFinderInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _factory;
        private readonly IRouteCalculator _routeCalculator;
        private uint _lastIndex = 0;

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        public IReadOnlyList<Coordinate> SpawnpointCoordinates { get; private set; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public bool OnlyUnknownSpawnpoints { get; }

        public bool OptimizeRoute { get; }

        #endregion

        #region Constructors

        public TthFinderInstanceController(IDbContextFactory<MapDataContext> factory, Instance instance, List<MultiPolygon> multiPolygons, IRouteCalculator routeCalculator)
        {
            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;
            OnlyUnknownSpawnpoints = instance.Data?.OnlyUnknownSpawnpoints ?? Strings.DefaultOnlyUnknownSpawnpoints;
            OptimizeRoute = instance.Data?.OptimizeSpawnpointsRoute ?? Strings.DefaultOptimizeSpawnpointRoute;

            _logger = new Logger<TthFinderInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _factory = factory;
            _routeCalculator = routeCalculator;

            SpawnpointCoordinates = Bootstrap();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(GetTaskOptions options)
        {
            if (SpawnpointCoordinates.Count == 0)
            {
                _logger.LogWarning($"[{options.Uuid}] No spawnpoints available to find TTH!");
                return null;
            }

            Coordinate currentCoord;
            // TODO: Lock _lastIndex
            var currentIndex = (int)_lastIndex;
            currentCoord = SpawnpointCoordinates[currentIndex];
            if (!options.IsStartup)
            {
                if (_lastIndex + 1 == SpawnpointCoordinates.Count)
                {
                    SpawnpointCoordinates = Bootstrap();
                    _lastIndex = 0;
                    if (SpawnpointCoordinates.Count == 0)
                    {
                        _logger.LogWarning($"[{options.Uuid}] No unknown spawnpoints to check, sending 0,0");
                        currentCoord = new Coordinate();
                        // TODO: Assign instance to chained instance upon completion of tth finder
                    }
                }
                else
                {
                    _lastIndex++;
                }
            }
            return await Task.FromResult(new CircleTask
            {
                Action = DeviceActionType.ScanPokemon,
                Area = Name,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            });
        }

        public async Task<string> GetStatusAsync()
        {
            if (SpawnpointCoordinates.Count == 0)
            {
                return "No Unknown Spawnpoints";
            }
            var position = (double)_lastIndex / (double)SpawnpointCoordinates.Count;
            var percent = Math.Round(position * 100.0, 2);
            var status = $"Spawnpoints: {_lastIndex:N0}/{SpawnpointCoordinates.Count:N0} ({percent}%)";
            return await Task.FromResult(status);
        }

        public void Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            _lastIndex = 0;
        }

        public void Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
        }

        #endregion

        #region Private Methods

        private IReadOnlyList<Coordinate> Bootstrap()
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in MultiPolygons)
            {
                var bbox = multiPolygon.GetBoundingBox();
                var spawnpointCoords = GetSpawnpoints(bbox, true);
                var polygon = multiPolygon.ConvertToCoordinates();
                // Filter spawnpoint coordinates that are within the geofence
                var coordsInArea = spawnpointCoords.Where(coord => GeofenceService.IsPointInPolygon(coord, polygon))
                                                   .ToList();
                coordinates.AddRange(coordsInArea);
            }

            // Optimize spawnpoints list by distance with IRouteCalculator
            if (OptimizeRoute)
            {
                _routeCalculator.ClearCoordinates();
                _routeCalculator.AddCoordinates(coordinates);
                var optimized = _routeCalculator.CalculateShortestRoute();
                _routeCalculator.ClearCoordinates();

                return optimized.ToList();
            }

            return coordinates;
        }

        private List<Coordinate> GetSpawnpoints(BoundingBox bbox, bool onlyUnknown)
        {
            using (var context = _factory.CreateDbContext())
            {
                if (onlyUnknown)
                {
                    var unknownSpawnpoints = context.Spawnpoints.AsEnumerable()
                                                                .Where(spawn => spawn.DespawnSecond == null)
                                                                .Where(spawn => bbox.IsInBoundingBox(spawn.Latitude, spawn.Longitude))
                                                                .Select(spawn => new Coordinate(spawn.Latitude, spawn.Longitude))
                                                                .ToList();
                    return unknownSpawnpoints;
                }
                var spawnpoints = context.Spawnpoints.AsEnumerable()
                                                     .Where(spawn => bbox.IsInBoundingBox(spawn.Latitude, spawn.Longitude))
                                                     .Select(spawn => new Coordinate(spawn.Latitude, spawn.Longitude))
                                                     .ToList();
                return spawnpoints;
            }
        }

        #endregion
    }
}