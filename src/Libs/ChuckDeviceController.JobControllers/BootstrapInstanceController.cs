namespace ChuckDeviceController.JobControllers;

using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Common.Tasks;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.JobControllers.Tasks;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.Routing;
using ChuckDeviceController.Routing.Utilities;

public class BootstrapInstanceController : BaseSmartInstanceController, IScanNextInstanceController
{
    #region Variables

    private readonly ILogger<BootstrapInstanceController> _logger;
    private readonly IRoutingHost _routeGenerator;
    private readonly IRouteCalculator _routeCalculator;
    private readonly List<IMultiPolygon> _multiPolygons;
    //private ulong _startTime = 0;

    #endregion

    #region Properties

    public override string Description => $"Quickly scan at area based on custom circle plot sizes.";

    public override IReadOnlyList<ICoordinate> Coordinates { get; internal set; }

    public bool FastBootstrapMode { get; }

    public ushort CircleSize { get; }

    public bool OptimizeRoute { get; }

    public string OnCompleteInstanceName { get; }

    public Queue<ICoordinate> ScanNextCoordinates { get; } = new();

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
        IReadOnlyList<IMultiPolygon> multiPolygons,
        IRoutingHost routeGenerator,
        IRouteCalculator routeCalculator)
        : base(instance, new List<ICoordinate>(), CircleInstanceType.Pokemon, CircleInstanceRouteType.Smart)
    {
        FastBootstrapMode = instance.Data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode;
        CircleSize = instance.Data?.CircleSize ?? Strings.DefaultCircleSize;
        OptimizeRoute = instance.Data?.OptimizeBootstrapRoute ?? Strings.DefaultOptimizeBootstrapRoute;
        OnCompleteInstanceName = instance.Data?.BootstrapCompleteInstanceName ?? Strings.DefaultBootstrapCompleteInstanceName;

        _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
        _multiPolygons = multiPolygons.ToList();
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
            _logger.LogInformation("[{Name}] [{Uuid}] Executing ScanNext API job at '{Coord}'", Name, options.Uuid, coord);
            return await Task.FromResult(scanNextTask);
        }

        if ((Coordinates?.Count ?? 0) == 0)
        {
            _logger.LogWarning("[{Name}] [{Uuid}] Instance does not contain any coordinates, returning empty task for device", Name, options.Uuid);
            return null!;
        }

        // Get next scan coordinate for device based on route type
        var currentCoord = GetNextScanLocation(options.Uuid);

        // Check if we were unable to retrieve a coordinate to send
        if (currentCoord == null)
        {
            _logger.LogWarning("[{Name}] [{Uuid}] Failed to retrieve next scan coordinate", Name, options.Uuid);
            return null!;
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
        _logger.LogDebug("[{Name}] Reloading instance", Name);

        // Clear all existing devices from route index cache
        _currentUuid.Clear();

        // Generate bootstrap coordinates route again
        Coordinates = GenerateBootstrapCoordinates();
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _logger.LogDebug("[{Name}] Stopping instance", Name);

        // Clear all existing devices from route index cache
        _currentUuid.Clear();

        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private BootstrapTask CreateBootstrapTask(ICoordinate currentCoord)
    {
        return new BootstrapTask
        {
            Action = FastBootstrapMode
                ? DeviceActionType.ScanRaid // 5 second loads
                : DeviceActionType.ScanPokemon, // 10 second loads
            Latitude = currentCoord.Latitude,
            Longitude = currentCoord.Longitude,
            MinimumLevel = MinimumLevel,
            MaximumLevel = MaximumLevel,
        };
    }

    private CircleTask CreateScanNextTask(ICoordinate currentCoord)
    {
        return new CircleTask
        {
            Action = DeviceActionType.ScanPokemon,
            MinimumLevel = MinimumLevel,
            MaximumLevel = MaximumLevel,
            Latitude = currentCoord.Latitude,
            Longitude = currentCoord.Longitude,
            LureEncounter = EnableLureEncounters,
        };
    }

    private List<ICoordinate> GenerateBootstrapCoordinates()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var bootstrapRoute = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
        {
            MultiPolygons = _multiPolygons,
            RouteType = RouteGenerationType.Bootstrap,
            //RouteType = RouteGenerationType.Randomized,
            RadiusM = CircleSize,
        });

        stopwatch.Stop();
        var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
        _logger.LogInformation("[{Name}] Bootstrap route generation took {TotalSeconds}s", Name, totalSeconds);

        if ((bootstrapRoute?.Count ?? 0) == 0)
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
            var optimizedRoute = RouteOptimizeUtil.Optimize(bootstrapRoute!);

            stopwatch.Stop();
            totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
            _logger.LogInformation("[{Name}] Bootstrap route optimization took {TotalSeconds}s", Name, totalSeconds);

            return optimizedRoute;
        }

        return bootstrapRoute ?? new();
    }

    private async Task CheckCompletionStatusAsync(string uuid)
    {
        // Check if device has completed all coordinates in the route, if all have been visisted
        // call InstanceComplete event for device.

        // REVIEW: Possibly keep track of which coordinates in case device is given same route
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
            OnInstanceComplete(OnCompleteInstanceName, uuid, lastCompletedTime);
        }
    }

    #endregion
}