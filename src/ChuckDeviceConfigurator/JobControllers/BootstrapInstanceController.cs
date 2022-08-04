namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.JobControllers.Contracts;
    using ChuckDeviceConfigurator.JobControllers.EventArgs;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Routing.Utilities;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class BootstrapInstanceController : BaseSmartInstanceController, IScanNextInstanceController
    {
        #region Variables

        private readonly ILogger<BootstrapInstanceController> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;
        private readonly List<MultiPolygon> _multiPolygons;
        //private ulong _startTime = 0;

        #endregion

        #region Properties

        public override IReadOnlyList<Coordinate> Coordinates { get; internal set; }

        public bool FastBootstrapMode { get; }

        public ushort CircleSize { get; }

        public bool OptimizeRoute { get; }

        public string OnCompleteInstanceName { get; set; }

        public Queue<Coordinate> ScanNextCoordinates { get; } = new();

        #endregion

        #region Events

        public event EventHandler<BootstrapInstanceCompleteEventArgs>? InstanceComplete;
        private void OnInstanceComplete(string instanceName, string deviceUuid, ulong completionTimestamp)
        {
            InstanceComplete?.Invoke(this, new BootstrapInstanceCompleteEventArgs(instanceName, deviceUuid, completionTimestamp));
        }

        #endregion

        #region Constructor

        public BootstrapInstanceController(
            Instance instance,
            List<MultiPolygon> multiPolygons,
            IRouteGenerator routeGenerator,
            IRouteCalculator routeCalculator)
            : base(instance, new(), CircleInstanceType.Pokemon, CircleInstanceRouteType.Smart)
        {
            FastBootstrapMode = instance.Data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode;
            CircleSize = instance.Data?.CircleSize ?? Strings.DefaultCircleSize;
            OptimizeRoute = instance.Data?.OptimizeBootstrapRoute ?? Strings.DefaultOptimizeBootstrapRoute;
            OnCompleteInstanceName = instance.Data?.BootstrapCompleteInstanceName ?? Strings.DefaultBootstrapCompleteInstanceName;

            _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _multiPolygons = multiPolygons;
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;

            // Generate bootstrap route
            Coordinates = GenerateBootstrapCoordinates();
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
                var scanNextTask = CreateScanNextTask(coord);
                _logger.LogInformation($"[{Name}] [{options.Uuid}] Executing ScanNext API job at '{coord}'");
                return await Task.FromResult(scanNextTask);
            }

            if ((Coordinates?.Count ?? 0) == 0)
            {
                _logger.LogWarning($"[{Name}] [{options.Uuid}] Instance does not contain any coordinates, returning empty task for device");
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

            _currentUuid[options.Uuid].CoordinatesCompletedCount++;

            await CheckCompletionStatusAsync(options.Uuid);

            var task = CreateBootstrapTask(currentCoord);
            return task;
        }

        public override async Task<string> GetStatusAsync()
        {
            // TODO: Get stats based on all devices for bootstrap job controller status
            var position = (double)_lastIndex / Coordinates.Count;
            var percent = Math.Round(position * 100.00, 2);
            var lastCompletedTime = Convert.ToUInt64(_lastCompletedTime);
            var completed = lastCompletedTime > 0
                //? $", Last Completed @ {_lastCompletedTime.FromSeconds()} ({_timesCompleted} times)"
                ? $", Last Completed @ {lastCompletedTime.FromSeconds().ToLocalTime()}"
                : "";
            var status = $"Bootstrapping: {Strings.DefaultInstanceStatus}";
            if (lastCompletedTime > 0)
            {
                status = $"Bootstrapping: {_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%){completed}";
            }
            return await Task.FromResult(status);
        }

        public override Task ReloadAsync()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // Clear all existing devices from route index cache
            _currentUuid.Clear();

            // Generate bootstrap coordinates route again
            Coordinates = GenerateBootstrapCoordinates();
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

        #region Private Methods

        private BootstrapTask CreateBootstrapTask(Coordinate currentCoord)
        {
            return new BootstrapTask
            {
                Area = Name,
                Action = FastBootstrapMode
                    ? DeviceActionType.ScanRaid // 5 second loads
                    : DeviceActionType.ScanPokemon, // 10 second loads
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
                // TODO: EnableLureEncounters for bootstrap job controller?
            };
        }

        private CircleTask CreateScanNextTask(Coordinate currentCoord)
        {
            return new CircleTask
            {
                Area = Name,
                Action = DeviceActionType.ScanPokemon,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                LureEncounter = EnableLureEncounters,
            };
        }

        private List<Coordinate> GenerateBootstrapCoordinates()
        {
            //TestRouting();

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var bootstrapRoute = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                MultiPolygons = _multiPolygons,
                RouteType = RouteGenerationType.Bootstrap,
                //RouteType = RouteGenerationType.Randomized,
                CircleSize = CircleSize,
            });

            stopwatch.Stop();
            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
            _logger.LogInformation($"[{Name}] Bootstrap route generation took {totalSeconds}s");

            if (bootstrapRoute?.Count == 0)
            {
                throw new Exception($"No bootstrap coordinates generated!");
            }

            if (OptimizeRoute)
            {
                stopwatch.Start();

                // Fast route optimization but contains a couple big jumps
                //_routeCalculator.ClearCoordinates();
                //_routeCalculator.AddCoordinates(bootstrapRoute);
                //var optimizedRoute = _routeCalculator.CalculateShortestRoute();
                //_routeCalculator.ClearCoordinates();

                // Benchmark - Roughly 60.4414-64.7969 :(
                //Utilities.Utils.BenchmarkAction(() => RouteOptimizeUtil.Optimize(bootstrapRoute));

                // Optimized route with no big jumps, although takes a lot longer to generate
                var optimizedRoute = RouteOptimizeUtil.Optimize(bootstrapRoute);

                stopwatch.Stop();
                totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                _logger.LogInformation($"[{Name}] Bootstrap route optimization took {totalSeconds}s");

                return optimizedRoute;
            }

            return bootstrapRoute;
        }

        private async Task CheckCompletionStatusAsync(string uuid)
        {
            // Check if device has completed all coordinates in the route, if all have been visisted
            // call InstanceComplete event for device.

            // REVIEW: Maybe keep track of which coordinates in case device is given same route
            var hasCompletedRoute = _currentUuid[uuid].CoordinatesCompletedCount == Coordinates.Count;
            if (hasCompletedRoute)
            {
                _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();

                // Assign instance to chained instance upon completion of bootstrap,
                // if specified
                if (string.IsNullOrEmpty(OnCompleteInstanceName))
                {
                    // Just keep reloading bootstrap route if no chained instance specified
                    //_timesCompleted++;
                    await ReloadAsync();
                    return;
                }

                // Trigger OnComplete event
                var lastCompletedTime = Convert.ToUInt64(_lastCompletedTime);
                // REVIEW: Trigger all devices in _currentUuid not just the one fetching the job currently
                // Probably best to keep it individually trigger per device
                OnInstanceComplete(OnCompleteInstanceName, uuid, lastCompletedTime);
            }
        }

        #endregion
    }
}