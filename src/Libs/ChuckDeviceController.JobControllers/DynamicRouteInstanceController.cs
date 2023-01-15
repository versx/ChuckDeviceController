namespace ChuckDeviceController.JobControllers;

using Microsoft.Extensions.Logging;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.Routing;

public class DynamicRouteInstanceController : CircleInstanceController
{
    #region Variables

    private readonly ILogger<DynamicRouteInstanceController> _logger;
    private readonly IRoutingHost _routeGenerator;
    private readonly IRouteCalculator _routeCalculator;

    #endregion

    #region Properties

    public override string Description => $"Dynamically generated routing for raids and Pokemon spawns.";

    public IReadOnlyList<IMultiPolygon> MultiPolygons { get; }

    public bool OptimizeDynamicRoute { get; }

    #endregion

    #region Constructor

    public DynamicRouteInstanceController(
        Instance instance,
        IReadOnlyList<IMultiPolygon> multiPolygons,
        IRoutingHost routeGenerator,
        IRouteCalculator routeCalculator)
        : base(instance, new List<ICoordinate>(), CircleInstanceType.Pokemon)
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
        _logger.LogInformation("[{Name}] Generating dynamic route...", Name);

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var options = new RouteGeneratorOptions
        {
            RadiusM = Strings.DefaultCircleSize,
            RouteType = RouteGenerationType.Randomized,
            MultiPolygons = (List<IMultiPolygon>)MultiPolygons,
            MaximumPoints = 500,
        };
        var route = _routeGenerator.GenerateRoute(options);

        stopwatch.Stop();
        var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
        _logger.LogInformation("[{Name}] Dynamic route generation took {TotalSeconds}s", Name, totalSeconds);

        if (OptimizeDynamicRoute)
        {
            stopwatch.Start();

            _routeCalculator.AddCoordinates(route);
            var optimized = _routeCalculator.CalculateShortestRoute();

            stopwatch.Stop();
            totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
            _logger.LogInformation("[{Name}] Dynamic route optimization took {TotalSeconds}s", Name, totalSeconds);

            return optimized.ToList();
        }
        return route;
    }

    #endregion
}