namespace ChuckDeviceConfigurator.JobControllers
{
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public class DynamicRouteInstanceController : CircleInstanceController
    {
        #region Variables

        private readonly ILogger<DynamicRouteInstanceController> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;

        #endregion

        #region Properties

        public IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        public bool OptimizeDynamicRoute { get; }

        #endregion

        #region Constructor

        public DynamicRouteInstanceController(
            Instance instance,
            List<MultiPolygon> multiPolygons,
            IRouteGenerator routeGenerator,
            IRouteCalculator routeCalculator)
            : base(instance, new(), CircleInstanceType.Pokemon)
        {
            _logger = new Logger<DynamicRouteInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;

            MultiPolygons = multiPolygons;
            OptimizeDynamicRoute = instance.Data?.OptimizeDynamicRoute ?? Strings.DefaultOptimizeDynamicRoute;
            Coordinates = GenerateDynamicRoute();
        }

        #endregion

        public override async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            // Add device to device list
            AddDevice(options.Uuid);
            Coordinate? currentCoord;

            // Check if on demand scanning coordinates list has any to send to workers
            if (ScanNextCoordinates.Count > 0)
            {
                currentCoord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateTask(currentCoord);
                return await Task.FromResult(scanNextTask);
            }

            if ((Coordinates?.Count ?? 0) == 0)
            {
                // TODO: Throw error that instance requires at least one coordinate
                _logger.LogError($"[{Name}] Instance requires at least one coordinate, please edit it to contain one.");
                return null;
            }

            // Get next coordinate
            currentCoord = SmartRoute(options.Uuid);

            // Check if we were unable to retrieve a coordinate to send
            if (currentCoord == null)
            {
                // TODO: Not sure if this will ever hit, need to test
                return null;
            }

            var task = CreateTask(currentCoord);
            return await Task.FromResult(task);
        }

        public override async Task<string> GetStatusAsync()
        {
            var circleStatus = await base.GetStatusAsync();
            var status = circleStatus == Strings.DefaultInstanceStatus
                ? Strings.DefaultInstanceStatus
                : $"{circleStatus}, (Coordinates: {Coordinates.Count:N0})";
            return await Task.FromResult(status);
        }

        private List<Coordinate> GenerateDynamicRoute()
        {
            var route = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                CircleSize = Strings.DefaultCircleSize,
                RouteType = RouteGenerationType.Randomized,
                MultiPolygons = (List<MultiPolygon>)MultiPolygons,
                MaximumPoints = 500,
            });

            if (OptimizeDynamicRoute)
            {
                _routeCalculator.ClearCoordinates();
                _routeCalculator.AddCoordinates(route);
                var optimized = _routeCalculator.CalculateShortestRoute();
                _routeCalculator.ClearCoordinates();
                return optimized.ToList();
            }
            return route;
        }
    }
}