namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Geofence.Models;
    using ChuckDeviceController.JobControllers.Tasks;
    using Coordinate = ChuckDeviceController.Data.Entities.Coordinate;
    using Geofence = ChuckDeviceController.Geofence.Models.Geofence;

    public class SpawnpointFinderInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<SpawnpointFinderInstanceController> _logger;
        private readonly SpawnpointRepository _spawnpointRepository;

        private readonly Dictionary<string, DeviceIndex> _lastUuid;
        private static readonly Random _random = new Random();
        private DateTime _lastCompletedTime;
        private int _lastIndex;
        private DateTime _lastLastCompletedTime;
        private readonly List<Coordinate> _coordinates;

        private readonly object _indexLock = new object();

        #endregion

        #region Properties

        public string Name { get; set; }

        public ushort MinimumLevel { get; set; }

        public ushort MaximumLevel { get; set; }

        public List<List<Coordinate>> Geofences { get; }

        #endregion

        #region Constructor(s)

        public SpawnpointFinderInstanceController()
        {
            _coordinates = new List<Coordinate>();
            _spawnpointRepository = new SpawnpointRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _logger = new Logger<SpawnpointFinderInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _lastCompletedTime = DateTime.UtcNow;
            _lastUuid = new Dictionary<string, DeviceIndex>();
            _lastIndex = 0;
        }

        public SpawnpointFinderInstanceController(string name, List<List<Coordinate>> geofences, ushort minLevel, ushort maxLevel) : this()
        {
            Name = name;
            Geofences = geofences;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;

            _coordinates = Task.Run(async () => await Bootstrap())
                               .ConfigureAwait(false)
                               .GetAwaiter()
                               .GetResult();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTask(string uuid, string accountUsername, bool startup)
        {
            if (_coordinates.Count == 0)
            {
                _logger.LogWarning($"[{uuid}] No spawnpoints available to find TTH!");
                return null;
            }
            Coordinate currentCoord;
            lock (_indexLock)
            {
                var currentIndex = _lastIndex;
                _logger.LogDebug($"[{uuid}] Current index: {currentIndex}");
                currentCoord = _coordinates[currentIndex];
                if (!startup)
                {
                    if (_lastIndex + 1 == _coordinates.Count)
                    {
                        _lastLastCompletedTime = _lastCompletedTime;
                        _lastCompletedTime = DateTime.UtcNow;
                        _lastIndex = 0;
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
            // TODO: Show amount found/left/total
            if (_coordinates.Count == 0)
            {
                return $"Spawnpoints 0";
            }
            var percentage = Math.Round(((double)_lastIndex / (double)_coordinates.Count) * 100.00, 2);
            var text = $"Spawnpoints {_lastIndex:N0}/{_coordinates.Count:N0} ({percentage}%)";
            return await Task.FromResult(text).ConfigureAwait(false);
        }

        public void Reload()
        {
            _lastIndex = 0;
            _lastCompletedTime = default;
            _lastLastCompletedTime = default;
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
                var spawnpoints = await _spawnpointRepository.GetAllAsync(bbox, true).ConfigureAwait(false);
                var spawnCoords = spawnpoints.Select(x => new Coordinate(x.Latitude, x.Longitude));
                list.AddRange(spawnCoords);
            }
            return list;
        }
    }
}