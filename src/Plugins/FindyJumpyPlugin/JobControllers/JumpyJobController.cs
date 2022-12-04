namespace FindyJumpyPlugin.JobControllers
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Common.Tasks;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.Geometry.Models.Contracts;
    using ChuckDeviceController.Plugin;

    using Extensions;
    using Tasks;

    // TODO: Add base/abstract GenericTask : ITask class for plugins via CDC.Common lib
    // TODO: Expose geofence/circle converter service to Plugins

    public struct JumpyCoord
    {
        public ulong Id { get; set; }

        public ICoordinate Coordinate { get; set; }

        public ushort SpawnSeconds { get; set; }
    }

    [GeofenceType(GeofenceType.Geofence)]
    public class JumpyJobController : IJobController, IJobControllerCoordinates, IScanNextInstanceController
    {
        private const ushort SleepTimeAutoPokemon = 10;
        private const ushort BufferTimeDistance = 20;

        #region Variables

        private readonly IDatabaseHost _dbHost;
        private readonly IGeofenceServiceHost _geofenceService;

        private readonly object _pokemonLock = new();
        private readonly IMemoryCache _pokemonCache;
        private List<JumpyCoord> _pokemonCoords = new();
        private ulong _lastCompletedTime;
        private ulong _lastLastCompletedTime;
        private int _currentDevicesMaxLocation = 0;
        private bool _firstRun = true;

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

        public JumpyJobController(
            IInstance instance,
            List<List<ICoordinate>> coords,
            List<IMultiPolygon> multiPolygons,
            IDatabaseHost dbHost,
            IGeofenceServiceHost geofenceService)
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
            _geofenceService = geofenceService;

            // TODO: Get MemoryCache config
            _pokemonCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

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
                Console.WriteLine($"[{Name}] [{options.Uuid}] Executing ScanNext API job at '{coord}'");
                return await Task.FromResult(scanNextTask);
            }

            var hit = _pokemonCache.Get<int>(Name);
            if (hit == 0)
            {
                InitJumpyCoordinates();
                _pokemonCache.Set(Name, 1);
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

                Console.WriteLine($"[{Name}] [{options.Uuid}] Pokemon - Instance: {Name} - oldLoc={loc} & newLoc={newLoc}/{_pokemonCoords.Count / 2}");

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
            Console.WriteLine($"[InitJumpyCoordinates] Starting...");

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
                    if (_geofenceService.IsPointInMultiPolygons(spawnpointCoord, MultiPolygons))
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

                Console.WriteLine($"[InitJumpyCoordinates] Got {count:N0} spawnpoints in min/max rectangle");
                Console.WriteLine($"[InitJumpyCoordinates] Got {tmpCoords.Count:N0} spawnpoints in geofence(s)");

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
                _pokemonCache.Set(Name, 1);
                return 0;
            }

            var curTime = currentTime;
            var loc = ++currentLocation;
            if (loc > countCoords)
            {
                Console.WriteLine($"[GetNextScanLocation] Reached end of data, resetting back to zero");
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
                        Console.WriteLine($"[GetNextScanLocation] No zero location, unknown error occurred...");
                    }
                }
                nextCoord = _pokemonCoords[loc];
            }

            var spawnSeconds = nextCoord.SpawnSeconds;
            var (minTime, maxTime) = spawnSeconds.GetOffsetsForSpawnTimer();
            Console.WriteLine($"[GetNextScanLocation] minTime={minTime} & curTime={curTime} & maxTime={maxTime}");

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
                Console.WriteLine($"[GetNextScanLocation] a1: curTime between min and max, moving standard 1 forward");

                // Test if we are getting too close to the minTime
                if (curTime - minTime < BufferTimeDistance)
                {
                    Console.WriteLine($"[GetNextScanLocation] a2: Sleeping 10 seconds as too close to minTime, in normal time");
                    Thread.Sleep(SleepTimeAutoPokemon * 1000);
                }
            }
            else if (curTime < minTime)
            {
                // Spawn is past time to visit, need to find a good one to jump to
                Console.WriteLine($"[GetNextScanLocation] b1: curTime={curTime} > maxTime, iterate");

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
                            Console.WriteLine($"[GetNextScanLocation] b2: mnTime={mnTime} & curTime={curTime} & & mxTime={mxTime}");
                            found = true;
                            loc = i;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GetNextScanLocation] Error: {ex}");
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
                            Console.WriteLine($"[GetNextScanLocation] b3: iterate backwards solution={found}");
                            Console.WriteLine($"[GetNextScanLocation] b4: mnTime={mnTime} & curTime={curTime} & mxTime={mxTime}");
                            found = true;
                            loc = i;
                            break;
                        }
                    }
                }
            }
            else if (curTime < minTime && !_firstRun)
            {
                Console.WriteLine($"[GetNextScanLocation] c1: Sleeping 20 seconds");
                Thread.Sleep(20 * 1000);

                loc--;
            }
            else if (curTime > maxTime)
            {
                // Spawn is past time to visit, need to find a good one to jump to
                Console.WriteLine($"[GetNextScanLocation] d1: curTime={curTime} > maxTime={maxTime}, iterate");

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
                        Console.WriteLine($"[GetNextScanLocation] d2: iterate forward solution={found}");
                        Console.WriteLine($"[GetNextScanLocation] d3: mnTime={mnTime} & curTime={curTime} & mxTime={mxTime}");
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
                            Console.WriteLine($"[GetNextScanLocation] d4: iterate backwards solution={found}");
                            Console.WriteLine($"[GetNextScanLocation] d5: mnTime={mnTime} & curTime={curTime} & mxTime={mxTime}");
                            found = true;
                            loc = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"[GetNextScanLocation] e1: Criteria fail with curTime={curTime} & curLocation={currentLocation} & despawn={spawnSeconds}");
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
}