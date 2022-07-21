namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public class CircleInstanceController : BaseSmartInstanceController, IScanNextInstanceController
    {
        private readonly ILogger<CircleInstanceController> _logger;

        public override IReadOnlyList<Coordinate> Coordinates { get; internal set; }

        public Queue<Coordinate> ScanNextCoordinates { get; } = new();

        #region Constructor

        public CircleInstanceController(Instance instance, List<Coordinate> coords, CircleInstanceType circleType = CircleInstanceType.Pokemon)
            : base(instance, coords, circleType, instance.Data?.CircleRouteType ?? Strings.DefaultCircleRouteType)
        {
            Coordinates = coords;
            _logger = new Logger<CircleInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
        }

        #endregion

        #region Public Methods

        public override async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            // Add device to device list
            AddDevice(options.Uuid);
            Coordinate? currentCoord = null;

            // Check if on demand scanning coordinates list has any to send to workers
            if (ScanNextCoordinates.Count > 0)
            {
                currentCoord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateTask(currentCoord, CircleType);
                return await Task.FromResult(scanNextTask);
            }

            if (Coordinates.Count == 0)
            {
                // TODO: Throw error that instance requires at least one coordinate
                _logger.LogError($"[{Name}] Instance requires at least one coordinate, please edit it to contain one.");
                return null;
            }

            switch (CircleType)
            {
                case CircleInstanceType.Pokemon:
                case CircleInstanceType.Raid:
                    switch (RouteType)
                    {
                        // TODO: Eventually remove leap frog routing logic (cough, remove all circle routing instances all together)
                        case CircleInstanceRouteType.Default:
                            // Get default leap frog route
                            currentCoord = BasicRoute();
                            break;
                        case CircleInstanceRouteType.Split:
                            // Split route by device count
                            currentCoord = SplitRoute(options.Uuid);
                            break;
                        //case CircleInstanceRouteType.Circular:
                        // Circular split route by device count
                        case CircleInstanceRouteType.Smart:
                            // Smart routing by device count
                            currentCoord = SmartRoute(options.Uuid);
                            break;
                    }
                    break;
            }

            // Check if we were unable to retrieve a coordinate to send
            if (currentCoord == null)
            {
                // TODO: Not sure if this will ever hit, need to test
                return null;
            }

            var task = CreateTask(currentCoord, CircleType);
            return await Task.FromResult(task);
        }

        public override async Task<string> GetStatusAsync()
        {
            var status = Strings.DefaultInstanceStatus;
            if (RouteType == CircleInstanceRouteType.Default)
            {
                // Gets the basic leap frog routing logic round time of any (the last) completed
                // device assigned to the route that has completed it.
                if (_lastCompletedTime > 0 && _lastLastCompletedTime > 0)
                {
                    var timeDiffSeconds = _lastCompletedTime - _lastLastCompletedTime;
                    if (timeDiffSeconds > 0)
                    {
                        var time = Math.Round(timeDiffSeconds, 2);
                        status = $"Round Time: {time:N0}s";
                    }
                }
            }
            else
            {
                // TODO: Get total average of all devices last completed time
                // Get the sum of all route round times for all devices if they have completed the
                // route at least once. Twice so that LastCompleted and LastCompletedWholeRoute are
                // both set. (maybe average too?)
                var totalRoundTime = _currentUuid.Values.Sum(device =>
                    // If device hasn't completed the route yet, or rather its assigned coordinates
                    // list, use 0 for route last completed sum calculation.
                    device.LastCompleted == 0 || device.LastCompletedWholeRoute == 0
                    ? 0
                    : (double)device.LastCompleted - (double)device.LastCompletedWholeRoute
                );
                if (totalRoundTime > 0)
                {
                    // Only show average route round time if at least one device has completed it.
                    var time = Math.Round(totalRoundTime);
                    // TODO: Format round trip time status by hours/minutes/seconds instead of just seconds.
                    status = $"Round Time: {time:N0}s";
                }
                else
                {
                    // No assigned devices have completed the route yet, show their current route indexes out
                    // of the total amount of coordinates for the route until one has fully completed it.
                    var indexes = _currentUuid.Values.Select(uuid => uuid.LastRouteIndex).ToList();
                    var indexesStatus = indexes.Count == 0
                        ? "0"
                        : string.Join(", ", _currentUuid.Values.Select(uuid => uuid.LastRouteIndex));
                    status = $"Route Indexes: {indexesStatus}/{Coordinates.Count}";
                }
            }
            return await Task.FromResult(status);
        }

        public override Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // TODO: Lock lastIndex
            _lastIndex = 0;
            // Clear all existing devices from route index cache
            _currentUuid.Clear();

            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
            return Task.CompletedTask;
        }

        #endregion
    }
}