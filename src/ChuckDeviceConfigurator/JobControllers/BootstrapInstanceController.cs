namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.Extensions;

    public class BootstrapInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<BootstrapInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _factory;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;
        private readonly List<MultiPolygon> _multiPolygons;
        private int _lastIndex = 0;
        private ulong _startTime = 0;
        private ulong _lastCompletedTime = 0;
        private int _timesCompleted = 0;

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

        #endregion

        #region Constructor

        public BootstrapInstanceController(
            IDbContextFactory<MapDataContext> factory,
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
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _factory = factory;
            _multiPolygons = multiPolygons;
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;

            // Generate bootstrap route
            var bootstrapRoute = GenerateBootstrapCoordinates();
            Coordinates = bootstrapRoute.ToList();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(GetTaskOptions options)
        {
            // TODO: Save last index to Instance.Data
            // TODO: Lock _lastIndex
            var currentCoord = Coordinates[_lastIndex];
            if (!options.IsStartup)
            {
                if (_startTime == 0)
                {
                    _startTime = DateTime.UtcNow.ToTotalSeconds();
                }

                if (_lastIndex + 1 == Coordinates.Count)
                {
                    _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();
                    _timesCompleted++;
                    Reload();
                    // TODO: Assign instance to chained instance upon completion of bootstrap
                }
                else
                {
                    _lastIndex++;
                }
            }

            var task = await GetBootstrapTask(currentCoord);
            return task;
        }

        public async Task<string> GetStatusAsync()
        {
            var position = (double)_lastIndex / (double)Coordinates.Count;
            var percent = Math.Round(position * 100.00, 2);
            var completed = _lastCompletedTime > 0
                ? $", Completed @ {_lastCompletedTime.FromSeconds()} ({_timesCompleted} times)"
                : "";
            var status = $"{_lastIndex:N0}/{Coordinates.Count:N0} ({percent}%){completed}";
            return await Task.FromResult(status);
        }

        public void Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            _lastIndex = 0;

            // Generate bootstrap coordinates route again
            var bootstrapRoute = GenerateBootstrapCoordinates();
            Coordinates = bootstrapRoute.ToList();
        }

        public void Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
        }

        #endregion

        #region Private Methods

        private async Task<BootstrapTask> GetBootstrapTask(Coordinate currentCoord)
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

        private Queue<Coordinate> GenerateBootstrapCoordinates()
        {
            //TestRouting();

            var bootstrapRoute = _routeGenerator.GenerateBootstrapRoute(_multiPolygons, CircleSize);
            if (bootstrapRoute?.Count == 0)
            {
                throw new Exception($"No bootstrap coordinates generated!");
            }

            _routeCalculator.AddCoordinates(bootstrapRoute);
            var optimizedRoute = _routeCalculator.CalculateShortestRoute();
            return optimizedRoute;
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
                var seconds = BenchmarkAction(action);
            */

            var optimizer = new RouteOptimizer(_factory, _multiPolygons)
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
}