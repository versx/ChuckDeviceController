namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Common.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public class CircleInstanceController : BaseSmartInstanceController, IScanNextInstanceController
    {
        #region Variables

        private readonly ILogger<CircleInstanceController> _logger;

        #endregion

        #region Properties

        public override IReadOnlyList<Coordinate> Coordinates { get; internal set; }

        public Queue<ICoordinate> ScanNextCoordinates { get; } = new();

        #endregion

        #region Constructor

        public CircleInstanceController(
            Instance instance,
            List<Coordinate> coords,
            CircleInstanceType circleType = CircleInstanceType.Pokemon)
            : base(instance, coords, circleType, instance.Data?.CircleRouteType ?? Strings.DefaultCircleRouteType)
        {
            _logger = new Logger<CircleInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            Coordinates = coords;
        }

        #endregion

        #region Public Methods

        public override async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            // Add device to device list
            AddDevice(options.Uuid);

            // Check if on demand scanning coordinates list has any to send to workers
            if (ScanNextCoordinates.Count > 0)
            {
                var coord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateTask(coord, CircleType);
                _logger.LogInformation($"[{Name}] [{options.Uuid}] Executing ScanNext API job at '{coord}'");
                return await Task.FromResult(scanNextTask);
            }

            if ((Coordinates?.Count ?? 0) == 0)
            {
                _logger.LogWarning($"[{Name}] [{options.Uuid}] Instance requires at least one coordinate, returning empty task for device");
                return null;
            }

            // Get next scan coordinate for device based on route type
            var currentCoord = GetNextScanLocation(options.Uuid);

            // Check if we were unable to retrieve a coordinate to send
            if (currentCoord == null)
            {
                _logger.LogWarning($"[{Name}] [{options.Uuid}] Failed to retrieve next scan coordinate");
                return null;
            }

            var task = CreateTask(currentCoord, CircleType);
            return await Task.FromResult(task);
        }

        public override async Task<string> GetStatusAsync()
        {
            return await base.GetStatusAsync();
        }

        public override Task ReloadAsync()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // TODO: Lock lastIndex
            _lastIndex = 0;

            // Clear all existing devices from route index cache
            _currentUuid.Clear();

            return Task.CompletedTask;
        }

        public override Task StopAsync()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            // Clear all existing devices from route index cache
            _currentUuid.Clear();

            return Task.CompletedTask;
        }

        #endregion
    }
}