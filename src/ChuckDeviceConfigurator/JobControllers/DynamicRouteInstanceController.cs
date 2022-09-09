namespace ChuckDeviceConfigurator.JobControllers
{
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Geometry;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Plugin;

    // TODO: Calculate proper status for DynamicRoute job controller instance

    public class DynamicRouteInstanceController : CircleInstanceController
    {
        #region Variables

        private readonly ILogger<DynamicRouteInstanceController> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;

        #endregion

        #region Properties

        public IReadOnlyList<IMultiPolygon> MultiPolygons { get; }

        public bool OptimizeDynamicRoute { get; }

        #endregion

        #region Constructor

        public DynamicRouteInstanceController(
            Instance instance,
            List<IMultiPolygon> multiPolygons,
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

        #region Private Methods

        private List<ICoordinate> GenerateDynamicRoute()
        {
            _logger.LogInformation($"[{Name}] Generating dynamic route...");

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var route = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                CircleSize = Strings.DefaultCircleSize,
                RouteType = RouteGenerationType.Randomized,
                MultiPolygons = (List<IMultiPolygon>)MultiPolygons,
                MaximumPoints = 500,
            });

            stopwatch.Stop();
            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
            _logger.LogInformation($"[{Name}] Dynamic route generation took {totalSeconds}s");

            if (OptimizeDynamicRoute)
            {
                stopwatch.Start();

                _routeCalculator.AddCoordinates(route);
                var optimized = _routeCalculator.CalculateShortestRoute();

                stopwatch.Stop();
                totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                _logger.LogInformation($"[{Name}] Dynamic route optimization took {totalSeconds}s");

                return optimized.ToList();
            }
            return route;
        }

        #endregion
    }
}