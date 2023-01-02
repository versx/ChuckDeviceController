namespace FindyJumpyPlugin.JobControllers;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Common.Tasks;
using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Plugin;

using Tasks;

[GeofenceType(GeofenceType.Geofence)]
public class FindyJobController : IJobController, IJobControllerCoordinates, IScanNextInstanceController
{
    private const int DefaultMaxBatchSpawnpoints = 5000;

    #region Variables

    private readonly IDatabaseHost _dbHost;
    private readonly IGeofenceServiceHost _geofenceHost;
    //private readonly ILoggingHost _loggingHost;
    private readonly ILogger<FindyJobController> _logger;

    private readonly object _tthLock = new();
    private readonly IMemoryCache _tthCache;
    private List<ICoordinate> _tthCoords = new();
    private int _lastCountUnknown = 0;
    private int _currentDevicesMaxLocation = 0;

    #endregion

    #region Properties

    public string Name { get; }

    public ushort MinimumLevel { get; }

    public ushort MaximumLevel { get; }

    public string? GroupName { get; }

    public bool IsEvent { get; }

    public IReadOnlyList<ICoordinate> Coordinates { get; }

    public IReadOnlyList<IMultiPolygon> MultiPolygons { get; }

    public Queue<ICoordinate> ScanNextCoordinates { get; }

    #endregion

    #region Constructor

    public FindyJobController(
        IInstance instance,
        List<List<ICoordinate>> coords,
        List<IMultiPolygon> multiPolygons,
        IDatabaseHost dbHost,
        IGeofenceServiceHost geofenceHost,
        //ILoggingHost loggingHost,
        //ILoggerProvider loggerProvider,
        //ILoggerFactory logger,
        //ILogger logger2,
        //ILogger<IJobController> logger2,
        ILogger<FindyJobController> logger,
        IMemoryCache memCache)
    {
        Name = instance.Name;
        MinimumLevel = instance.MinimumLevel;
        MaximumLevel = instance.MaximumLevel;
        GroupName = instance.Data?.AccountGroup ?? null;
        IsEvent = instance.Data?.IsEvent ?? false;
        Coordinates = coords?.FirstOrDefault()!;
        ScanNextCoordinates = new();
        MultiPolygons = multiPolygons;

        _dbHost = dbHost;
        _geofenceHost = geofenceHost;
        //_loggingHost = loggingHost;
        //_logger = loggerProvider.CreateLogger(nameof(FindyJobController));
        //_logger = logger.CreateLogger<FindyJobController>();
        _logger = logger;
        _tthCache = memCache;

        InitFindyCoordinates();
    }

    #endregion

    #region Public Methods

    public async Task<ITask> GetTaskAsync(TaskOptions options)
    {
        // Check if on demand scanning coordinates list has any to send to workers
        if (ScanNextCoordinates.Count > 0)
        {
            var coord = ScanNextCoordinates.Dequeue();
            var scanNextTask = CreateTask(coord);
            _logger.LogDebug($"[{Name}] [{options.Uuid}] Executing ScanNext API job at '{coord}'");
            return await Task.FromResult(scanNextTask);
        }

        // Get route like for tth finding, specify fence and use tth = null
        // with each gettask, just increment to next point in list
        // requery the route every ???? min, set with cache above
        // run until data length == 0, then output a message to tell user done
        // since we actually care about laptime, use that variable
        var hit = _tthCache.Get<int>(Name);
        if (hit == 0)
        {
            InitFindyCoordinates();
            _tthCache.Set(Name, 1);
        }

        // Increment location
        var loc = _currentDevicesMaxLocation;
        var newLoc = loc + 1;
        if (newLoc >= _tthCoords.Count)
        {
            newLoc = 0;
        }

        _logger.LogDebug($"[{Name}] [{options.Uuid}] TTH - oldLoc={loc:N0} & newLoc={newLoc:N0}/{_tthCoords.Count:N0}");

        _currentDevicesMaxLocation = newLoc;

        ICoordinate currentCoord = new Coordinate(0.0, 0.0);
        if (_tthCoords.Count >= newLoc)
        {
            currentCoord = _tthCoords[newLoc];
        }
        else
        {
            if (_tthCoords.Count == 1)
            {
                _currentDevicesMaxLocation = 0;
                currentCoord = _tthCoords[newLoc];
            }
            else
            {
                _currentDevicesMaxLocation = -1;
            }
        }

        var task = CreateTask(currentCoord);
        return await Task.FromResult(task);
    }

    public async Task<string> GetStatusAsync()
    {
        var change = _tthCoords.Count - _lastCountUnknown;
        if (change == _tthCoords.Count)
        {
            change = 0;
        }
        var changes = change > 0 ? "+" : "";
        var status = $"Coord Count: {_tthCoords.Count:N0}, Delta: {changes}{change}";
        return await Task.FromResult(status);
    }

    public Task ReloadAsync()
    {
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void InitFindyCoordinates()
    {
        _logger.LogTrace($"Starting...");

        _lastCountUnknown = _tthCoords.Count;
        lock (_tthLock)
        {
            _tthCoords.Clear();

            var tmpCoords = new List<ICoordinate>();
            var bbox = GetBoundingBox(MultiPolygons);

            //var spawnpoints = await _dbHost.FindAsync<ISpawnpoint, ulong>(
            var spawnpoints = _dbHost.FindAsync<ISpawnpoint, ulong>(
                spawnpoint => spawnpoint.DespawnSecond == null &&
                    spawnpoint.Latitude > bbox.MinimumLatitude &&
                    spawnpoint.Longitude > bbox.MinimumLongitude &&
                    spawnpoint.Latitude < bbox.MaximumLatitude &&
                    spawnpoint.Longitude < bbox.MaximumLongitude,
                limit: DefaultMaxBatchSpawnpoints).Result;

            var count = 0;
            foreach (var spawnpoint in spawnpoints)
            {
                var spawnpointCoord = new Coordinate(spawnpoint.Latitude, spawnpoint.Longitude);
                if (_geofenceHost.IsPointInMultiPolygons(spawnpointCoord, MultiPolygons))
                {
                    tmpCoords.Add(spawnpointCoord);
                }

                count++;
            }

            _logger.LogDebug("Got {count:N0} points in min/max rectangle with null tth", count);
            _logger.LogDebug("Got {Count:N0} points in geofence(s) with null tth", tmpCoords.Count);

            if (count == 0)
            {
                _logger.LogDebug($"Got {count:N0} points in min/max rectangle with null tth");
            }

            if (!tmpCoords.Any())
            {
                _logger.LogDebug($"Got {tmpCoords.Count:N0} points in geofence(s) with null tth");
            }

            // Sort the array, so 0-3600 sec in order
            _tthCoords = tmpCoords;
        }
    }

    private ITask CreateTask(ICoordinate coord)
    {
        return new FindyTask
        {
            Action = DeviceActionType.ScanPokemon,
            Latitude = coord.Latitude,
            Longitude = coord.Longitude,
            MinimumLevel = MinimumLevel,
            MaximumLevel = MaximumLevel,
        };
    }

    public static IBoundingBox GetBoundingBox(IEnumerable<IMultiPolygon> multiPolygons)
    {
        // Get min and max coords from polygon(s)
        var minLat = 90d;
        var maxLat = -90d;
        var minLon = 180d;
        var maxLon = -180d;
        foreach (var polygon in multiPolygons)
        {
            var bounds = polygon.GetBoundingBox();
            minLat = Math.Min(minLat, bounds.MinimumLatitude);
            maxLat = Math.Max(maxLat, bounds.MaximumLatitude);
            minLon = Math.Min(minLon, bounds.MinimumLongitude);
            maxLon = Math.Max(maxLon, bounds.MaximumLongitude);
        }
        return new BoundingBox
        {
            MinimumLatitude = minLat,
            MaximumLatitude = maxLat,
            MinimumLongitude = minLon,
            MaximumLongitude = maxLon,
        };
    }

    #endregion
}