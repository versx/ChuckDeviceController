namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Routing.Utilities;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.Extensions;

    public class BootstrapInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<BootstrapInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _mapFactory;
        private readonly IDbContextFactory<DeviceControllerContext> _deviceFactory;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;
        private readonly List<MultiPolygon> _multiPolygons;
        private int _lastIndex = 0;
        private ulong _startTime = 0;
        private ulong _lastCompletedTime = 0;
        //private int _timesCompleted = 0;

        #endregion

        #region Properties

        public string Name { get; }

        public List<Coordinate> Coordinates { get; private set; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public bool FastBootstrapMode { get; }

        public ushort CircleSize { get; }

        public bool OptimizeRoute { get; }

        public string OnCompleteInstanceName { get; set; }

        #endregion

        #region Events

        public event EventHandler<BootstrapInstanceCompleteEventArgs> InstanceComplete;
        private void OnInstanceComplete(string instanceName, string deviceUuid, ulong completionTimestamp)
        {
            InstanceComplete?.Invoke(this, new BootstrapInstanceCompleteEventArgs(instanceName, deviceUuid, completionTimestamp));
        }

        #endregion

        #region Constructor

        public BootstrapInstanceController(
            IDbContextFactory<MapDataContext> mapFactory,
            IDbContextFactory<DeviceControllerContext> deviceContext,
            Instance instance,
            List<MultiPolygon> multiPolygons,
            IRouteGenerator routeGenerator,
            IRouteCalculator routeCalculator)
        {
            Name = instance.Name;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            FastBootstrapMode = instance.Data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode;
            CircleSize = instance.Data?.CircleSize ?? Strings.DefaultCircleSize;
            OptimizeRoute = instance.Data?.OptimizeBootstrapRoute ?? Strings.DefaultOptimizeBootstrapRoute;
            OnCompleteInstanceName = instance.Data?.BootstrapCompleteInstanceName ?? Strings.DefaultBootstrapCompleteInstanceName;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _mapFactory = mapFactory;
            _deviceFactory = deviceContext;
            _multiPolygons = multiPolygons;
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;

            // Generate bootstrap route
            Coordinates = GenerateBootstrapCoordinates();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(GetTaskOptions options)
        {
            if (Coordinates?.Count == 0)
            {
                // TODO: If no Coordinates (bootstrap gen possibly failed, invalid geofence, etc) try and regenerate with retry limit?
                _logger.LogWarning($"[{Name}] [{options.Uuid}] No bootstrap coordinates available!");
                return null;
            }

            // TODO: Save last index to Instance.Data
            // TODO: Lock _lastIndex
            var currentIndex = _lastIndex;
            var currentCoord = Coordinates[currentIndex];
            _lastIndex++;

            if (!options.IsStartup)
            {
                if (_startTime == 0)
                {
                    _startTime = DateTime.UtcNow.ToTotalSeconds();
                }

                if (_lastIndex == Coordinates.Count)
                {
                    _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();

                    // Assign instance to chained instance upon completion of bootstrap,
                    // if specified
                    if (string.IsNullOrEmpty(OnCompleteInstanceName))
                    {
                        // Just keep reloading bootstrap route if no chained instance specified
                        //_timesCompleted++;
                        Reload();
                    }
                    else
                    {
                        // Trigger OnComplete event
                        OnInstanceComplete(OnCompleteInstanceName, options.Uuid, _lastCompletedTime);
                    }
                }
            }

            var task = await GetBootstrapTaskAsync(currentCoord);
            return task;
        }

        public async Task<string> GetStatusAsync()
        {
            var position = (double)_lastIndex / (double)Coordinates.Count;
            var percent = Math.Round(position * 100.00, 2);
            var completed = _lastCompletedTime > 0
                //? $", Last Completed @ {_lastCompletedTime.FromSeconds()} ({_timesCompleted} times)"
                ? $", Last Completed @ {_lastCompletedTime.FromSeconds()}"
                : "";
            var status = $"Bootstrapping: {_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%){completed}";
            return await Task.FromResult(status);
        }

        public void Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            _lastIndex = 0;

            // Generate bootstrap coordinates route again
            Coordinates = GenerateBootstrapCoordinates();
        }

        public void Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
        }

        #endregion

        #region Private Methods

        private async Task<BootstrapTask> GetBootstrapTaskAsync(Coordinate currentCoord)
        {
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

        private List<Coordinate> GenerateBootstrapCoordinates()
        {
            //TestRouting();

            var bootstrapRoute = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                MultiPolygons = _multiPolygons,
                //RouteType = RouteGenerationType.Bootstrap,
                RouteType = RouteGenerationType.Randomized,
                CircleSize = CircleSize,
            });

            if (bootstrapRoute?.Count == 0)
            {
                throw new Exception($"No bootstrap coordinates generated!");
            }

            if (OptimizeRoute)
            {
                //_routeCalculator.AddCoordinates(bootstrapRoute);
                //var optimizedRoute = _routeCalculator.CalculateShortestRoute();
                var optimizedRoute = RouteOptimizeUtil.Optimize(bootstrapRoute);
                return optimizedRoute.ToList();
            }

            return bootstrapRoute;
        }

        private void TestRouting()
        {
            /*
                var action = () =>
                {
                    var bootstrapRoute = _routeGenerator.GenerateBootstrapRoute(_multiPolygons, CircleSize);
                    bootstrapRoute.ForEach(coord => _routeCalculator.AddCoordinate(coord));
                    var optimizedRoute = _routeCalculator.CalculateShortestRoute();
                };
                var seconds = Utils.BenchmarkAction(action);
            */

            var optimizer = new RouteOptimizer(_mapFactory, _multiPolygons)
            {
                IncludeSpawnpoints = true,
                IncludeGyms = true,
                IncludeNests = true,
                IncludePokestops = true,
                IncludeS2Cells = true,
                OptimizeCircles = true,
                OptimizePolygons = true,
            };
            var optimizerRoute = optimizer.GenerateRouteAsync(new RouteOptimizerOptions
            {
                OptimizationAttempts = 2,
                CircleSize = CircleSize,
                OptimizeTsp = true,
            }).Result;

            _routeCalculator.ClearCoordinates();
            _routeCalculator.AddCoordinates(optimizerRoute);
            var calcRoute = _routeCalculator.CalculateShortestRoute();
            Console.WriteLine($"CalcRoute: {calcRoute}");

            var route = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                MultiPolygons = _multiPolygons,
                RouteType = RouteGenerationType.Bootstrap,
                MaximumPoints = 500,
                CircleSize = CircleSize,
            });
            Console.WriteLine($"Bootstrap: {route}");

            route = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                MultiPolygons = _multiPolygons,
                RouteType = RouteGenerationType.Randomized,
                MaximumPoints = 500,
                CircleSize = CircleSize,
            });
            Console.WriteLine($"Random: {route}");

            route = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                MultiPolygons = _multiPolygons,
                RouteType = RouteGenerationType.Optimized,
                MaximumPoints = 500,
                CircleSize = CircleSize,
            });
            Console.WriteLine($"Optimized: {route}");
        }

        #endregion
    }

    public sealed class BootstrapInstanceCompleteEventArgs : EventArgs
    {
        public string InstanceName { get; }

        public string DeviceUuid { get; set; }

        public ulong CompletionTimestamp { get; }

        public BootstrapInstanceCompleteEventArgs(string instanceName, string deviceUuid, ulong completionTimestamp)
        {
            InstanceName = instanceName;
            DeviceUuid = deviceUuid;
            CompletionTimestamp = completionTimestamp;
        }
    }
}