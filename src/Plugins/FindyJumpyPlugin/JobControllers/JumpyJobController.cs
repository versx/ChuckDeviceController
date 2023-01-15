namespace FindyJumpyPlugin.JobControllers;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Abstractions;
using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Common.Tasks;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Plugin;

using Extensions;
using Tasks;

public struct JumpyCoord
{
    public ulong Id { get; set; }

    public ICoordinate Coordinate { get; set; }

    public ushort SpawnSeconds { get; set; }
}

[GeofenceType(nameof(GeofenceType.Geofence))]
public class JumpyJobController : IJobController, IJobControllerCoordinates, IScanNextInstanceController
{
    private const ushort SleepTimeAutoPokemon = 10;
    private const ushort BufferTimeDistance = 20;

    #region Variables

    private readonly ILogger<JumpyJobController> _logger;
    private readonly IDatabaseHost _dbHost;
    private readonly IGeofenceServiceHost _geofenceHost;
    //private readonly ILoggingHost _loggingHost;

    private readonly object _pokemonLock = new();
    private readonly IMemoryCache _pokemonCache;
    private List<JumpyCoord> _pokemonCoords = new();
    private ulong _lastCompletedTime;
    private ulong _lastLastCompletedTime;
    private int _currentDevicesMaxLocation = 0;
    private bool _firstRun = true;
    private readonly MemoryCacheEntryOptions _defaultCacheOptions = new()
    {
        Size = 1,
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
    };

    #endregion

    #region Properties

    public string Name { get; }

    public string Description => $"Finds Pokemon near spawnpoints about to trigger a Pokemon spawn.";

    public ushort MinimumLevel { get; }

    public ushort MaximumLevel { get; }

    public string? GroupName { get; }

    public bool IsEvent { get; }

    public IReadOnlyList<ICoordinate> Coordinates { get; }

    public IReadOnlyList<IMultiPolygon> MultiPolygons { get; }

    public Queue<ICoordinate> ScanNextCoordinates { get; }

    #endregion

    #region Constructor

    public JumpyJobController(
        IInstance instance,
        List<List<ICoordinate>> coords,
        List<IMultiPolygon> multiPolygons,
        IDatabaseHost dbHost,
        IGeofenceServiceHost geofenceHost,
        ILogger<JumpyJobController> logger,
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
        _logger = logger;
        _pokemonCache = memCache;

        InitJumpyCoordinates();
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
            _logger.LogDebug("[{Name}] [{Uuid}] Executing ScanNext API job at '{Coord}'", Name, options.Uuid, coord);
            return await Task.FromResult(scanNextTask);
        }

        var hit = _pokemonCache.Get<int>(Name);
        if (hit == 0)
        {
            InitJumpyCoordinates();
            _pokemonCache.Set(Name, 1, _defaultCacheOptions);
        }

        var (_, min, sec) = TimeExtensions.ConvertSecondsToHoursMinutesSeconds();
        var curSecInHour = min * 60 + sec;
        ITask task;
        lock (_pokemonLock)
        {
            // Increment location
            var loc = _currentDevicesMaxLocation;
            var newLoc = GetNextScanLocation(curSecInHour, loc);
            _firstRun = false;

            _logger.LogDebug("[{Name}] [{Uuid}] Pokemon - oldLoc={Loc:N0} & newLoc={NewLoc:N0}/{Count:N0}", Name, options.Uuid, loc, newLoc, _pokemonCoords.Count / 2);

            var currentCoord = new JumpyCoord
            {
                Id = 1,
                Coordinate = new Coordinate(0.0, 0.0),
                SpawnSeconds = 0,
            };
            if (_pokemonCoords.Count >= newLoc)
            {
                _currentDevicesMaxLocation = newLoc;
                currentCoord = _pokemonCoords[newLoc];
            }
            else
            {
                if (_pokemonCoords.Count == 1)
                {
                    _currentDevicesMaxLocation = 0;
                    currentCoord = _pokemonCoords[0];
                }
                else
                {
                    _currentDevicesMaxLocation = -1;
                }
            }

            task = CreateTask(currentCoord.Coordinate);
        }
        return await Task.FromResult(task);
    }

    public Task<string> GetStatusAsync()
    {
        var count = _pokemonCoords?.Count ?? 0 / 2;
        var status = $"Coord Count: {count:N0}";
        return Task.FromResult(status);
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

    private void InitJumpyCoordinates()
    {
        _logger.LogTrace($"Starting...");

        lock (_pokemonLock)
        {
            _pokemonCoords.Clear();

            var tmpCoords = new List<JumpyCoord>();
            var bbox = FindyJobController.GetBoundingBox(MultiPolygons);

            var spawnpoints = _dbHost.FindAsync<ISpawnpoint, uint>(
                spawnpoint => spawnpoint.DespawnSecond != null &&
                    spawnpoint.Latitude > bbox.MinimumLatitude &&
                    spawnpoint.Longitude > bbox.MinimumLongitude &&
                    spawnpoint.Latitude < bbox.MaximumLatitude &&
                    spawnpoint.Longitude < bbox.MaximumLongitude,
                spawnpoint => spawnpoint.DespawnSecond ?? 0,
                SortOrderDirection.Asc,
                limit: int.MaxValue).Result;

            var count = 0;
            foreach (var spawnpoint in spawnpoints)
            {
                var spawnSeconds = spawnpoint.DespawnSecond;
                spawnSeconds -= 1800; // Add 30 min so when spawn should show, we don't track as 60 min spawn.

                if (spawnSeconds < 0)
                {
                    spawnSeconds += 3600;
                }

                var spawnpointCoord = new Coordinate(spawnpoint.Latitude, spawnpoint.Longitude);
                if (_geofenceHost.IsPointInMultiPolygons(spawnpointCoord, MultiPolygons))
                {
                    tmpCoords.Add(new JumpyCoord
                    {
                        Id = spawnpoint.Id,
                        Coordinate = spawnpointCoord,
                        SpawnSeconds = (ushort)(spawnSeconds ?? 0),
                        //SpawnSeconds = (ushort)(spawnSeconds ?? 0 + 3500),
                    });
                }

                count++;
            }

            _logger.LogDebug("Got {Count:N0} spawnpoints in min/max rectangle", count);
            _logger.LogDebug("Got {Count:N0} spawnpoints in geofence(s)", tmpCoords.Count);

            // Sort the array, so 0-3600 sec in order
            _pokemonCoords = tmpCoords;

            // Take lazy man's approach, probably not ideal
            // Add elements to end, so 3600-7199 sec
            //foreach (var coord in tmpCoords)
            //{
            //    _pokemonCoords.Add(new AutoPokemonCoord
            //    {
            //        Id = coord.Id,
            //        Coordinate = coord.Coordinate,
            //        SpawnSeconds = (ushort)(coord.SpawnSeconds + 3600),
            //    });
            //}

            // Did the list shrink from last query?
            var oldCoord = _currentDevicesMaxLocation;
            if (oldCoord >= _pokemonCoords.Count)
            {
                _currentDevicesMaxLocation = 0;
            }
        }
    }

    private int GetNextScanLocation(ulong currentTime, int currentLocation)
    {
        var countArray = _pokemonCoords.Count;
        var countCoords = countArray / 2;
        if (countArray <= 0)
        {
            InitJumpyCoordinates();
            _pokemonCache.Set(Name, 1, _defaultCacheOptions);
            return 0;
        }

        var curTime = currentTime;
        var loc = ++currentLocation;
        if (loc > countCoords)
        {
            _logger.LogDebug($"Reached end of data, resetting back to zero");
            _lastLastCompletedTime = _lastCompletedTime;
            _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();
            return 0;
        }
        else if (loc < 0)
        {
            loc = 0;
        }

        JumpyCoord nextCoord;
        lock (_pokemonLock)
        {
            if (_pokemonCoords.Count - 1 < loc)
            {
                if (_pokemonCoords.Count == 1)
                {
                    loc = 0;
                }
                else
                {
                    _logger.LogDebug($"No starting location, unknown error occurred...");
                }
            }
            nextCoord = _pokemonCoords[loc];
        }

        var spawnSeconds = nextCoord.SpawnSeconds;
        var (minTime, maxTime) = spawnSeconds.GetOffsetsForSpawnTimer();
        _logger.LogDebug("minTime={MinTime} & curTime={CurTime} & maxTime={MaxTime}", minTime, curTime, maxTime);

        var topOfHour = minTime < 0;
        if (topOfHour)
        {
            curTime += 3600;
            minTime += 3600;
            maxTime += 3600;
        }

        // Logic
        if (curTime >= minTime && curTime <= maxTime)
        {
            // Good to jump as between to key points for current time
            _logger.LogDebug($"a1: curTime between min and max, moving standard 1 forward");

            // Test if we are getting too close to the minTime
            if (curTime - minTime < BufferTimeDistance)
            {
                _logger.LogDebug($"a2: Sleeping 10 seconds as too close to minTime, in normal time");
                Thread.Sleep(SleepTimeAutoPokemon * 1000);
            }
        }
        else if (curTime < minTime)
        {
            // Spawn is past time to visit, need to find a good one to jump to
            _logger.LogDebug("b1: curTime={CurTime} > maxTime, iterate", curTime);

            var found = false;
            var start = loc;
            try
            {
                for (var i = start; i < countArray; i++)
                {
                    if (_pokemonCoords.Count - 1 < loc)
                    {
                        return 0;
                    }

                    nextCoord = _pokemonCoords[i];
                    spawnSeconds = nextCoord.SpawnSeconds;

                    var (mnTime, mxTime) = spawnSeconds.GetOffsetsForSpawnTimer();
                    if (curTime >= mnTime && curTime <= mnTime + 120ul)
                    {
                        _logger.LogDebug("b2: mnTime={MnTime} & curTime={CurTime} & & mxTime={MxTime}", mnTime, curTime, mxTime);
                        found = true;
                        loc = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
            }

            if (!found)
            {
                for (var i = start; i >= 0; i--)
                {
                    if (_pokemonCoords.Count - 1 < loc)
                    {
                        return 0;
                    }

                    nextCoord = _pokemonCoords[i];
                    spawnSeconds = nextCoord.SpawnSeconds;

                    var (mnTime, mxTime) = spawnSeconds.GetOffsetsForSpawnTimer();
                    if (curTime >= mnTime + 30ul && curTime < mnTime + 120ul)
                    {
                        _logger.LogDebug("b3: iterate backwards solution={Found}", found);
                        _logger.LogDebug("b4: mnTime={MnTime} & curTime={CurTime} & mxTime={MxTime}", mnTime, curTime, mxTime);
                        found = true;
                        loc = i;
                        break;
                    }
                }
            }
        }
        else if (curTime < minTime && !_firstRun)
        {
            _logger.LogDebug($"c1: Sleeping 20 seconds");
            Thread.Sleep(20 * 1000);

            loc--;
        }
        else if (curTime > maxTime)
        {
            // Spawn is past time to visit, need to find a good one to jump to
            _logger.LogDebug("d1: curTime={CurTime} > maxTime={MaxTime}, iterate", curTime, maxTime);

            var found = false;
            var start = loc;
            for (var i = start; i < countArray; i++)
            {
                if (_pokemonCoords.Count - 1 < loc)
                {
                    return 0;
                }

                nextCoord = _pokemonCoords[i];
                spawnSeconds = nextCoord.SpawnSeconds;

                var (mnTime, mxTime) = spawnSeconds.GetOffsetsForSpawnTimer();
                if (curTime >= mnTime + 30ul && curTime <= mnTime + 120ul)
                {
                    _logger.LogDebug("d2: iterate forward solution={Found}", found);
                    _logger.LogDebug("d3: mnTime={MnTime} & curTime={CurTime} & mxTime={MxTime}", mnTime, curTime, mxTime);
                    found = true;
                    loc = i;
                    break;
                }
            }

            if (!found)
            {
                for (var i = start; i >= 0; i--)
                {
                    if (_pokemonCoords.Count - 1 < loc)
                    {
                        return 0;
                    }

                    nextCoord = _pokemonCoords[i];
                    spawnSeconds = nextCoord.SpawnSeconds;
                    var (mnTime, mxTime) = spawnSeconds.GetOffsetsForSpawnTimer();

                    if (curTime >= mnTime + 30ul && curTime <= mnTime + 120ul)
                    {
                        _logger.LogDebug("d4: iterate backwards solution={Found}", found);
                        _logger.LogDebug("d5: mnTime={MnTime} & curTime={CurTime} & mxTime={MxTime}", mnTime, curTime, mxTime);
                        found = true;
                        loc = i;
                        break;
                    }
                }
            }
        }
        else
        {
            _logger.LogDebug("e1: Criteria fail with curTime={CurTime} & curLocation={CurrentLocation} & despawn={SpawnSeconds}", curTime, currentLocation, spawnSeconds);
            // Go back to zero and iterate somewhere useful
            loc = 0;
        }

        if (loc == countCoords - 1)
        {
            _lastLastCompletedTime = _lastCompletedTime;
            _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();
        }

        if (loc > countCoords)
        {
            loc -= countCoords;
        }

        return loc;
    }

    private ITask CreateTask(ICoordinate coord)
    {
        return new JumpyTask
        {
            Action = DeviceActionType.ScanPokemon,
            Latitude = coord.Latitude,
            Longitude = coord.Longitude,
            MinimumLevel = MinimumLevel,
            MaximumLevel = MaximumLevel,
        };
    }

    #endregion
}