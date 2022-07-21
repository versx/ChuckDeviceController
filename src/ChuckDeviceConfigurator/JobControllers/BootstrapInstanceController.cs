namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.JobControllers.EventArgs;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Routing.Utilities;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data;
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
        private ulong _startTime = 0;
        private readonly Dictionary<Coordinate, bool> _coordsCompleted;

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
            _coordsCompleted = new Dictionary<Coordinate, bool>();
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
            Coordinate? currentCoord = null;

            if (ScanNextCoordinates.Count > 0)
            {
                currentCoord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateScanNextTask(currentCoord);
                return await Task.FromResult(scanNextTask);
            }

            if ((Coordinates?.Count ?? 0) == 0)
            {
                _logger.LogWarning($"[{Name}] [{options.Uuid}] No bootstrap coordinates available!");
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
            _coordsCompleted[currentCoord] = true;

            var now = DateTime.UtcNow.ToTotalSeconds();
            if (_startTime == 0)
            {
                _startTime = now;
            }

            //if (_lastIndex == Coordinates.Count)
            // TODO: Keep track of coordinates visited, if all visited call InstanceComplete event
            if (_coordsCompleted.All(coord => coord.Value == true) && _coordsCompleted.Count == Coordinates.Count)
            {
                _lastCompletedTime = now;

                // Assign instance to chained instance upon completion of bootstrap,
                // if specified
                if (string.IsNullOrEmpty(OnCompleteInstanceName))
                {
                    // Just keep reloading bootstrap route if no chained instance specified
                    //_timesCompleted++;
                    await Reload();
                }
                else
                {
                    // Trigger OnComplete event
                    var lastCompletedTime = Convert.ToUInt64(_lastCompletedTime);
                    // TODO: Trigger all devices in _currentUuid not just the one fetching the job currently
                    OnInstanceComplete(OnCompleteInstanceName, options.Uuid, lastCompletedTime);
                }
            }

            var task = await CreateBootstrapTaskAsync(currentCoord);
            return task;
        }

        public override async Task<string> GetStatusAsync()
        {
            var position = (double)_lastIndex / (double)Coordinates.Count;
            var percent = Math.Round(position * 100.00, 2);
            var lastCompletedTime = Convert.ToUInt64(_lastCompletedTime);
            var completed = lastCompletedTime > 0
                //? $", Last Completed @ {_lastCompletedTime.FromSeconds()} ({_timesCompleted} times)"
                ? $", Last Completed @ {lastCompletedTime.FromSeconds()}"
                : "";
            var status = $"Bootstrapping: {Strings.DefaultInstanceStatus}";
            if (_lastCompletedTime > 0)
            {
                status = $"Bootstrapping: {_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%){completed}";
            }
            return await Task.FromResult(status);
        }

        public override Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            _lastIndex = 0;

            // Generate bootstrap coordinates route again
            Coordinates = GenerateBootstrapCoordinates();
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task<BootstrapTask> CreateBootstrapTaskAsync(Coordinate currentCoord)
        {
            return await Task.FromResult(new BootstrapTask
            {
                Area = Name,
                Action = FastBootstrapMode
                    ? DeviceActionType.ScanRaid // 5 second loads
                    : DeviceActionType.ScanPokemon, // 10 second loads
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            });
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

            var bootstrapRoute = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                MultiPolygons = _multiPolygons,
                RouteType = RouteGenerationType.Bootstrap,
                //RouteType = RouteGenerationType.Randomized,
                CircleSize = CircleSize,
            });

            if (bootstrapRoute?.Count == 0)
            {
                throw new Exception($"No bootstrap coordinates generated!");
            }

            if (OptimizeRoute)
            {
                // Optimized route but contains a couple big jumps
                //_routeCalculator.ClearCoordinates();
                //_routeCalculator.AddCoordinates(bootstrapRoute);
                //var optimizedRoute = _routeCalculator.CalculateShortestRoute();
                //_routeCalculator.ClearCoordinates();

                // Benchmark - Roughly 60.4414-64.7969 :(
                //Utilities.Utils.BenchmarkAction(() => RouteOptimizeUtil.Optimize(bootstrapRoute));

                // Optimized route with no big jumps, although takes a lot longer to generate
                var optimizedRoute = RouteOptimizeUtil.Optimize(bootstrapRoute);
                return optimizedRoute;
            }

            return bootstrapRoute;
        }

        #endregion
    }

    /*
    public class BootstrapInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<BootstrapInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _mapFactory;
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

        public event EventHandler<BootstrapInstanceCompleteEventArgs>? InstanceComplete;
        private void OnInstanceComplete(string instanceName, string deviceUuid, ulong completionTimestamp)
        {
            InstanceComplete?.Invoke(this, new BootstrapInstanceCompleteEventArgs(instanceName, deviceUuid, completionTimestamp));
        }

        #endregion

        #region Constructor

        public BootstrapInstanceController(
            IDbContextFactory<MapDataContext> mapFactory,
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
            _multiPolygons = multiPolygons;
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;

            // Generate bootstrap route
            Coordinates = GenerateBootstrapCoordinates();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            if ((Coordinates?.Count ?? 0) == 0)
            {
                _logger.LogWarning($"[{Name}] [{options.Uuid}] No bootstrap coordinates available!");
                return null;
            }

            // TODO: Save last index to Instance.Data
            // TODO: Lock _lastIndex
            var currentIndex = _lastIndex;
            var currentCoord = Coordinates[currentIndex];
            _lastIndex++;

            var now = DateTime.UtcNow.ToTotalSeconds();
            if (_startTime == 0)
            {
                _startTime = now;
            }

            if (_lastIndex == Coordinates.Count)
            {
                _lastCompletedTime = now;

                // Assign instance to chained instance upon completion of bootstrap,
                // if specified
                if (string.IsNullOrEmpty(OnCompleteInstanceName))
                {
                    // Just keep reloading bootstrap route if no chained instance specified
                    //_timesCompleted++;
                    await Reload();
                }
                else
                {
                    // Trigger OnComplete event
                    OnInstanceComplete(OnCompleteInstanceName, options.Uuid, _lastCompletedTime);
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
            var status = $"Bootstrapping: {Strings.DefaultInstanceStatus}";
            if (_lastCompletedTime > 0)
            {
                status = $"Bootstrapping: {_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%){completed}";
            }
            return await Task.FromResult(status);
        }

        public Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            _lastIndex = 0;

            // Generate bootstrap coordinates route again
            Coordinates = GenerateBootstrapCoordinates();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task<BootstrapTask> GetBootstrapTaskAsync(Coordinate currentCoord)
        {
            return await Task.FromResult(new BootstrapTask
            {
                Area = Name,
                Action = FastBootstrapMode
                    ? DeviceActionType.ScanRaid // 5 second loads
                    : DeviceActionType.ScanPokemon, // 10 second loads
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
                RouteType = RouteGenerationType.Bootstrap,
                //RouteType = RouteGenerationType.Randomized,
                CircleSize = CircleSize,
            });

            if (bootstrapRoute?.Count == 0)
            {
                throw new Exception($"No bootstrap coordinates generated!");
            }

            if (OptimizeRoute)
            {
                // Optimized route but contains a couple big jumps
                //_routeCalculator.ClearCoordinates();
                //_routeCalculator.AddCoordinates(bootstrapRoute);
                //var optimizedRoute = _routeCalculator.CalculateShortestRoute();
                //_routeCalculator.ClearCoordinates();

                // Benchmark - Roughly 60.4414-64.7969 :(
                //Utilities.Utils.BenchmarkAction(() => RouteOptimizeUtil.Optimize(bootstrapRoute));

                // Optimized route with no big jumps, although takes a lot longer to generate
                var optimizedRoute = RouteOptimizeUtil.Optimize(bootstrapRoute);
                return optimizedRoute;
            }

            return bootstrapRoute;
        }

        private void TestRouting()
        {
            //var action = () =>
            //{
            //    var bootstrapRoute = _routeGenerator.GenerateBootstrapRoute(_multiPolygons, CircleSize);
            //    bootstrapRoute.ForEach(coord => _routeCalculator.AddCoordinate(coord));
            //    var optimizedRoute = _routeCalculator.CalculateShortestRoute();
            //};
            //var seconds = Utils.BenchmarkAction(action);

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
            var optimizerRoute = optimizer.OptimizeRouteAsync(new RouteOptimizerOptions
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
    */
}