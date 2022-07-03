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

        public BootstrapInstanceController(Instance instance, List<List<Coordinate>> coords)
        {
            Name = instance.Name;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            //FastBootstrapMode = instance.Data?.FastBootstrapMode
            GroupName = instance.Data?.AccountGroup;
            IsEvent = instance.Data?.IsEvent ?? false;

            _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            // TODO: Generate bootstrap route
            //Coordinates = _routeGenerator.GenerateBootstrapRoute((List<Geofence>)coords, circleSize);
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, bool isStartup = false)
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
            _lastIndex = 0;
        }

        public void Stop()
        {
        }

        #endregion
    }
}