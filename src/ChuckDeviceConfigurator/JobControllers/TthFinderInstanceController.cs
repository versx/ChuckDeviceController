﻿namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Models;

    /*
     * TODO: TthFinder logic
     * - Keep running until all spawnpoint tth are found
     * - Keep running until x amount of times to complete visit of spawnpoints
     * - Add chained instance once x amount of completes are reached or if all spawnpoints are found
     * - All of the above
    */

    // TODO: Keep list of spawnpoint ids, check how many have been found in this session and display in status
    // TODO: Notify admin that bootstrapping the area before using this type of job controller instance is highly recommended.

    public class TthFinderInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<TthFinderInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _factory;
        private readonly IRouteCalculator _routeCalculator;
        private int _lastIndex = 0;
        private ulong _startTime = 0;
        private ulong _lastCompletedTime = 0;
        private Dictionary<ulong, uint?> _spawnpoints;

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

        #region Constructor

        public TthFinderInstanceController(
            IDbContextFactory<MapDataContext> factory,
            Instance instance,
            List<MultiPolygon> multiPolygons,
            IRouteCalculator routeCalculator)
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
            _spawnpoints = new Dictionary<ulong, uint?>();

            SpawnpointCoordinates = GenerateSpawnpointCoordinates();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            if (SpawnpointCoordinates?.Count == 0)
            {
                _logger.LogWarning($"[{Name}] [{options.Uuid}] No spawnpoints available to find TTH!");
                return null;
            }

            // TODO: Lock _lastIndex
            var currentIndex = _lastIndex;
            var currentCoord = SpawnpointCoordinates[currentIndex];

            if (_startTime == 0)
            {
                _startTime = DateTime.UtcNow.ToTotalSeconds();
            }

            if (_lastIndex + 1 == SpawnpointCoordinates.Count)
            {
                _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();

                await Reload();

                if (SpawnpointCoordinates.Count == 0)
                {
                    _logger.LogWarning($"[{Name}] [{options.Uuid}] No unknown spawnpoints to check, sending 0,0");
                    currentCoord = new Coordinate();
                    // TODO: Assign instance to chained instance upon completion of tth finder
                }
            }
            else
            {
                _lastIndex++;
            }

            var task = await GetSpawnpointTaskAsync(currentCoord);
            return task;
        }

        public async Task<string> GetStatusAsync()
        {
            if (SpawnpointCoordinates.Count == 0)
            {
                return "No Unknown Spawnpoints";
            }
            var position = (double)_lastIndex / (double)SpawnpointCoordinates.Count;
            var percent = Math.Round(position * 100.0, 2);
            var completed = _lastCompletedTime > 0
                ? $", Last Completed @ {_lastCompletedTime.FromSeconds()}"
                : "";

            // TODO: Get count of spawnpoints found for instance session
            var status = $"Spawnpoints: {_lastIndex:N0}/{SpawnpointCoordinates.Count:N0} ({percent}%){completed}";
            return await Task.FromResult(status);
        }

        public Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            _lastIndex = 0;

            // Generate spawnpoint coordinates route again
            SpawnpointCoordinates = GenerateSpawnpointCoordinates();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task<BootstrapTask> GetSpawnpointTaskAsync(Coordinate currentCoord)
        {
            return await Task.FromResult(new BootstrapTask
            {
                Area = Name,
                Action = DeviceActionType.ScanPokemon,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            });
        }

        private IReadOnlyList<Coordinate> GenerateSpawnpointCoordinates()
        {
            var coordinates = new List<Coordinate>();
            foreach (var multiPolygon in MultiPolygons)
            {
                var bbox = multiPolygon.GetBoundingBox();
                var spawnpointCoords = GetSpawnpointCoordinates(bbox, OnlyUnknownSpawnpoints);
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

                // Convert Queue to List for now
                coordinates = optimized.ToList();
            }

            return coordinates;
        }

        private List<Coordinate> GetSpawnpointCoordinates(BoundingBox bbox, bool onlyUnknown)
        {
            using (var context = _factory.CreateDbContext())
            {
                // Get all spawnpoints within area's bounding box
                var spawnpoints = context.Spawnpoints.AsEnumerable()
                                                     .Where(spawn => bbox.IsInBoundingBox(spawn.Latitude, spawn.Longitude));
                // Get all spawnpoints or only spawnpoints with unknown despawn times if specified
                var list =
                (
                    onlyUnknown
                        ? spawnpoints.Where(spawn => spawn.DespawnSecond == null)
                        : spawnpoints
                ).ToList();

                // Cache all existing spawnpoints
                _spawnpoints = list.ToDictionary(x => x.Id, y => y.DespawnSecond);

                var coords = list.Select(spawn => new Coordinate(spawn.Latitude, spawn.Longitude))
                                 .ToList();
                return coords;
            }
        }

        #endregion
    }
}