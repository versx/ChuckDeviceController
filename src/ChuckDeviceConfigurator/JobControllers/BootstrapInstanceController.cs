namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public class BootstrapInstanceController : IJobController
    {
        private readonly ILogger<BootstrapInstanceController> _logger;
        //private readonly RouteGenerator _routeGenerator;
        private uint _lastIndex = 0;

        #region Properties

        public string Name { get; }

        public IReadOnlyList<Coordinate> Coordinates { get; }

        //public IReadOnlyList<Geofence> Geofences { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public bool FastBootstrapMode { get; }

        #endregion

        #region Constructor

        public BootstrapInstanceController(Instance instance, List<List<Coordinate>> geofences)
        {
            Name = instance.Name;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            FastBootstrapMode = instance.Data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            // TODO: Generate bootstrap route
            //_geofences = Geofence.FromPolygons(geofences);
            //_routeGenerator = new RouteGenerator();
            Coordinates = new List<Coordinate>();
            //Coordinates = _routeGenerator.GenerateBootstrapRoute((List<Geofence>)_geofences, circleSize);
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false)
        {
            // TODO: Save last index to Instance.Data
            // TODO: Lock _lastIndex
            var currentIndex = (int)_lastIndex;
            var currentCoord = Coordinates[currentIndex];
            if (!isStartup)
            {
                if (_lastIndex + 1 == Coordinates.Count)
                {
                    //_lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();
                    Reload();
                    // TODO: Assign instance to chained instance upon completion of bootstrap
                }
                else
                {
                    _lastIndex++;
                }
            }
            return await Task.FromResult(new BootstrapTask
            {
                Area = Name,
                Action = FastBootstrapMode
                    ? DeviceActionType.ScanRaid
                    : DeviceActionType.ScanPokemon,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            });
        }

        public async Task<string> GetStatusAsync()
        {
            var percent = Math.Round(Convert.ToDouble((double)_lastIndex / (double)Coordinates.Count) * 100.00, 2);
            var status = $"{_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%)";
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
    }
}