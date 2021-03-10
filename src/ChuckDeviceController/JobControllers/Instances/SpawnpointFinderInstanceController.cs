namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Common.JobControllers;
    using Chuck.Common.JobControllers.Tasks;
    using Chuck.Data.Entities;
    using Chuck.Data.Factories;
    using Chuck.Data.Repositories;
    using Chuck.Geometry.Geofence.Models;
    using Geofence = Chuck.Geometry.Geofence.Models.Geofence;

    public class SpawnpointFinderInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<SpawnpointFinderInstanceController> _logger;
        private readonly SpawnpointRepository _spawnpointRepository;

        private int _lastIndex;

        private readonly object _indexLock = new object();

        #endregion

        #region Properties

        public string Name { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public IReadOnlyList<List<Coordinate>> Geofences { get; }

        public IReadOnlyList<Coordinate> SpawnpointCoordinates { get; private set; }

        #endregion

        #region Constructor(s)

        public SpawnpointFinderInstanceController()
        {
            _spawnpointRepository = new SpawnpointRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _logger = new Logger<SpawnpointFinderInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _lastIndex = 0;

            SpawnpointCoordinates = new List<Coordinate>();
        }

        public SpawnpointFinderInstanceController(string name, List<List<Coordinate>> geofences, ushort minLevel, ushort maxLevel, string groupName = null, bool isEvent = false) : this()
        {
            Name = name;
            Geofences = geofences;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            GroupName = groupName;
            IsEvent = isEvent;

            SpawnpointCoordinates = Task.Run(async () => await Bootstrap().ConfigureAwait(false))
                                        .GetAwaiter()
                                        .GetResult();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTask(string uuid, string accountUsername, bool startup)
        {
            if (SpawnpointCoordinates.Count == 0)
            {
                _logger.LogWarning($"[{uuid}] No spawnpoints available to find TTH!");
                return null;
            }
            Coordinate currentCoord;
            lock (_indexLock)
            {
                var currentIndex = _lastIndex;
                _logger.LogDebug($"[{uuid}] Current index: {currentIndex}");
                currentCoord = SpawnpointCoordinates[currentIndex];
                if (!startup)
                {
                    if (_lastIndex + 1 == SpawnpointCoordinates.Count)
                    {
                        SpawnpointCoordinates = Bootstrap().ConfigureAwait(false)
                                                           .GetAwaiter()
                                                           .GetResult();
                        _lastIndex = 0;
                        if (SpawnpointCoordinates.Count == 0)
                        {
                            _logger.LogWarning($"[{uuid}] No unknown spawnpoints to check, sending 0,0");
                            currentCoord = new Coordinate();
                        }
                    }
                    else
                    {
                        _lastIndex++;
                    }
                }
            }
            return await Task.FromResult(new CircleTask
            {
                Action = ActionType.ScanPokemon,
                Area = Name,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            }).ConfigureAwait(false);
        }

        public async Task<string> GetStatus()
        {
            // TODO: Show amount found/remaining/total
            if (SpawnpointCoordinates.Count == 0)
            {
                return "No Unknown Spawnpoints";
            }
            var percentage = Math.Round(((double)_lastIndex / (double)SpawnpointCoordinates.Count) * 100.00, 2);
            var text = $"Spawnpoints {_lastIndex:N0}/{SpawnpointCoordinates.Count:N0} ({percentage}%)";
            return await Task.FromResult(text).ConfigureAwait(false);
        }

        public void Reload()
        {
            _lastIndex = 0;
        }

        public void Stop()
        {
        }

        #endregion

        private async Task<List<Coordinate>> Bootstrap()
        {
            // Get all unknown spawnpoints within geofence areas
            var list = new List<Coordinate>();
            foreach (var geofence in Geofences)
            {
                var fence = Geofence.FromPolygon(geofence);
                var bbox = new BoundingBox
                {
                    MinimumLatitude = geofence.Min(x => x.Latitude),
                    MaximumLatitude = geofence.Max(x => x.Latitude),
                    MinimumLongitude = geofence.Min(x => x.Longitude),
                    MaximumLongitude = geofence.Max(x => x.Longitude),
                };
                var spawnpoints = await _spawnpointRepository.GetAllAsync(
                    bbox.MinimumLatitude,
                    bbox.MinimumLongitude,
                    bbox.MaximumLatitude,
                    bbox.MinimumLongitude,
                    true
                ).ConfigureAwait(false);
                var spawnCoords = spawnpoints.Select(x => new Coordinate(x.Latitude, x.Longitude));
                list.AddRange(spawnCoords);
            }
            return list;
        }
    }
}