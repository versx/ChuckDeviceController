namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public class TthFinderInstanceController : IJobController
    {
        private readonly ILogger<TthFinderInstanceController> _logger;
        private uint _lastIndex = 0;

        #region Properties

        public string Name { get; }

        public IReadOnlyList<Coordinate> Coordinates { get; private set; }

        public IReadOnlyList<List<Coordinate>> Geofences { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        #endregion

        #region Constructors

        public TthFinderInstanceController(Instance instance, List<List<Coordinate>> geofences)
        {
            Name = instance.Name;
            Geofences = geofences;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            Coordinates = Bootstrap();

            _logger = new Logger<TthFinderInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false)
        {
            if (Coordinates.Count == 0)
            {
                _logger.LogWarning($"[{uuid}] No spawnpoints available to find TTH!");
                return null;
            }

            Coordinate currentCoord;
            // TODO: Lock _lastIndex
            var currentIndex = (int)_lastIndex;
            currentCoord = Coordinates[currentIndex];
            if (!isStartup)
            {
                if (_lastIndex + 1 == Coordinates.Count)
                {
                    Coordinates = Bootstrap();
                    _lastIndex = 0;
                    if (Coordinates.Count == 0)
                    {
                        _logger.LogWarning($"[{uuid}] No unknown spawnpoints to check, sending 0,0");
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
            if (Coordinates.Count == 0)
            {
                return "No Unknown Spawnpoints";
            }
            var percent = Math.Round(Convert.ToDouble((double)_lastIndex / (double)Coordinates.Count) * 100.0, 2);
            var status = $"Spawnpoints {_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%";
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

        private IReadOnlyList<Coordinate> Bootstrap()
        {
            var list = new List<Coordinate>();
            foreach (var geofence in Geofences)
            {
                // TODO: Change bbox to Polygon
                var bbox = new BoundingBox
                {
                    MinimumLatitude = geofence.Min(x => x.Latitude),
                    MaximumLatitude = geofence.Max(x => x.Latitude),
                    MinimumLongitude = geofence.Min(x => x.Longitude),
                    MaximumLongitude = geofence.Max(x => x.Longitude),
                };
                // TODO: Get all spawnpoints within bounding box
                //var spawnpointCoords = spawnpoints.Select(x => new Coordinate(x.Latitude, x.Longitude));
                //list.AddRange(spawnpointCoords);
            }
            return list;
        }
    }
}