namespace ChuckDeviceConfigurator.JobControllers
{
    using Google.Common.Geometry;
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    // TODO: Change to expect geofences instead of circle points, get all gyms within geofences

    public class SmartRaidGym
    {
        public Gym Gym { get; set; }

        public ulong Updated { get; set; }

        public Coordinate Coordinate { get; set; }
    }

    public class GymsResult
    {
        public List<SmartRaidGym> NoBoss { get; set; } = new();

        public List<SmartRaidGym> NoRaid { get; set; } = new();
    }

    public class CircleSmartRaidInstanceController : IJobController
    {
        #region Constants

        private const ushort RaidInfoBeforeHatchS = 120; // 2 minutes
        private const ushort IgnoreTimeEggS = 150; // 2.5 minutes
        private const ushort IgnoreTimeBossS = 60; // 1 minute
        private const ushort NoRaidTimeS = 1800; // 30 minutes

        #endregion

        #region Variables

        private readonly ILogger<CircleSmartRaidInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _factory;
        private readonly System.Timers.Timer _timer;

        //private readonly object _smartRaidLock = new();
        private readonly Dictionary<string, Gym> _smartRaidGyms;
        private readonly Dictionary<Coordinate, List<string>> _smartRaidGymsInPoint;
        private readonly Dictionary<Coordinate, ulong> _smartRaidPointsUpdated;

        private readonly object _statsLock = new();
        private ulong _startDate;
        private ulong _count = 0;

        #endregion

        #region Properties

        public string Name { get; }

        //public IReadOnlyList<Coordinate> Coordinates { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        #endregion

        #region Constructor

        public CircleSmartRaidInstanceController(IDbContextFactory<MapDataContext> factory, Instance instance, List<MultiPolygon> multiPolygons)
        {
            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            _factory = factory;
            _logger = new Logger<CircleSmartRaidInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _smartRaidGyms = new Dictionary<string, Gym>();
            _smartRaidGymsInPoint = new Dictionary<Coordinate, List<string>>();
            _smartRaidPointsUpdated = new Dictionary<Coordinate, ulong>();

            // Load/build gyms list for smart raid cache
            LoadGymsAsync();

            _timer = new System.Timers.Timer(30 * 1000); // 30 second interval
            _timer.Elapsed += async (sender, e) => await UpdateGymInfoAsync();
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(GetTaskOptions options)
        {
            var coord = GetNextScanLocation();
            if (coord == null)
            {
                // Unable to retrieve coordinate for next gym to check
                _logger.LogWarning($"[{Name}] [{options.Uuid}] Failed to retrieve gym coordinates for next job task.");
                return null;
            }

            // Update smart raid statistics for Dashboard status
            UpdateStats();

            var task = new CircleTask
            {
                Area = Name,
                Action = DeviceActionType.ScanRaid,
                Latitude = coord.Latitude,
                Longitude = coord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
            return await Task.FromResult(task);
        }

        public async Task<string> GetStatusAsync()
        {
            // TODO: Check how many gyms haven't been updated in the last x (30) minutes?
            //var gymsToCheck = GetGymsToCheck();
            var now = DateTime.UtcNow.ToTotalSeconds();
            var gymsTotal = _smartRaidGyms.Count;
            var gymsWithActiveRaidsCount = _smartRaidGyms.Count(gym => gym.Value.RaidEndTimestamp > now);
            var gymsWithoutActiveRaidsCount = gymsTotal - gymsWithActiveRaidsCount;
            //var gymsWithRaidsUpdatedRecently = _smar
            //var gymsUpdated = _smartRaidPointsUpdated.Values.Count(gymUpdated => gymUpdated > 0 && (now - gymUpdated) <= 1800);
            //var gymsNotUpdated = gymsTotal - gymsUpdated;

            uint? scansPerHour = null;
            lock (_statsLock)
            {
                if (_startDate > 0)
                {
                    var delta = now - _startDate;
                    if (_count > 0 && delta > 0)
                    {
                        scansPerHour = Convert.ToUInt32((double)_count / (double)delta * 3600);
                    }
                }
            }
            var scansStatus = scansPerHour > 0
                ? Convert.ToString(scansPerHour ?? 0)
                : "--";
            //var gymsUpdatedCount = gymsTotal - (gymsToCheck.NoRaid.Count + gymsToCheck.NoBoss.Count);
            //var gymsStatus = $", (Gyms Updated: {gymsUpdated:N0}/{gymsTotal:N0})";
            //var gymsStatus = $", (Raids Updated: {gymsUpdatedCount:N0}/{gymsTotal:N0})";
            var gymsStatus = $", (Active: {gymsWithActiveRaidsCount:N0}/{gymsWithoutActiveRaidsCount:N0}|{gymsTotal:N0})";
            var status = $"Scans/h: {scansStatus}{gymsStatus}";
            return await Task.FromResult(status);
        }

        public Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // Clear gyms cache and load gyms again
            _smartRaidGyms.Clear();
            _smartRaidGymsInPoint.Clear();
            _smartRaidPointsUpdated.Clear();

            // Load/gym gyms list for smart raid cache
            LoadGymsAsync();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            _timer.Stop();
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async void LoadGymsAsync()
        {
            // Instead of running a query for each coordinate,
            // retrieve all gyms from the database and filter which
            // are in the S2 cell. _Should increase SQL performance._
            //
            // Benchmark - (query all vs individual)
            // All: 0.157 0.1368 0.1696 vs Individual: 0.9209 0.9139 0.9386
            // New logic: 0.1884 0.1743 0.1913357 - Still faster
            // Roughly about 6 times faster.

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var allGyms = await GetGymsAsync();
            var routeGenerator = new Services.Routing.RouteGenerator(_factory);
            var route = routeGenerator.GenerateRoute(new Services.Routing.RouteGeneratorOptions
            {
                CircleSize = 500, // TODO: Make configurable?
                MaximumPoints = 500,
                MultiPolygons = (List<MultiPolygon>)MultiPolygons,
                RouteType = Services.Routing.RouteGenerationType.Bootstrap,
            });

            // Optimize bootstrapped route
            var routeCalculator = new Services.Routing.RouteCalculator(new List<Coordinate>(route));
            routeCalculator.ClearCoordinatesAfterOptimization = true;
            var optimizedRoute = routeCalculator.CalculateShortestRoute();

            foreach (var coord in route)
            {
                // TODO: Get S2 cells within multi polygon bounding box
                var latlng = S2LatLng.FromDegrees(coord.Latitude, coord.Longitude);
                var cellIds = latlng.GetLoadedS2CellIds()
                                    .Select(cell => cell.Id)
                                    .ToList();
                try
                {
                    var gyms = allGyms.Where(gym => cellIds.Contains(gym.CellId))
                                      .ToList();
                    var gymIds = gyms.Select(gym => gym.Id).ToList();
                    _smartRaidGymsInPoint[coord] = gymIds;
                    _smartRaidPointsUpdated[coord] = 0;

                    foreach (var gym in gyms)
                    {
                        // REVIEW: Shoot, when did .NET allow setting dictionary value without
                        // adding key to dictionary first and just using key index.
                        // :thinking: Finally! <3
                        _smartRaidGyms[gym.Id] = gym;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"LoadGymsAsync: {ex}");
                    // Sleep for 5 seconds
                    Thread.Sleep(5000);
                }
            }

            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            _logger.LogDebug($"Took: {totalSeconds}s");
        }

        /*
        private async void LoadGymsAsync3()
        {
            // Instead of running a query for each coordinate,
            // retrieve all gyms from the database and filter which
            // are in the S2 cell. _Should increase SQL performance._
            //
            // Benchmark - (query all vs individual)
            // All: 0.157 0.1368 0.1696 vs Individual: 0.9209 0.9139 0.9386
            // Roughly about 6 times faster.

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var allGyms = await GetGymsAsync();
            //var gyms = await GetGymsAsync((List<MultiPolygon>)MultiPolygons);
            foreach (var multiPolygon in MultiPolygons)
            {
                // TODO: Get S2 cells within multi polygon bounding box
                //var latlng = S2LatLng.FromDegrees(coord.Latitude, coord.Longitude);
                //var cellIds = latlng.GetLoadedS2CellIds()
                //                    .Select(cell => cell.Id)
                //                    .ToList();

                try
                {
                    var bbox = multiPolygon.GetBoundingBox();
                    var cells = bbox.GetS2CellCoverage();
                    //var cells2 = multiPolygon.GetS2CellIds();
                    var cellIds = cells.Select(cell => cell.Id).ToList();
                    var gymsInBbox = await GetGymsAsync(bbox);
                    var cellCoords = cellIds.Select(cell => cell.ToCoordinate())
                                            .ToList();
                    //foreach (var cell in cells)
                    //foreach (var cellCoord in cellCoords)
                    foreach (var cell in cells)
                    {
                        var cellId = cell.Id;
                        var cellCoord = cell.ToCoordinate();
                        var gyms = allGyms.Where(gym => gym.CellId == cellId)
                                          .ToList();
                        var gymIds = gyms.Select(gym => gym.Id).ToList();

                        //var gyms = await GetGymsByCellIdsAsync(cellIds);
                        //var cellCoord = gym.CellId.ToCoordinate();
                        //var gymCoord = new Coordinate(gym.Latitude, gym.Longitude);
                        //var dist = cellCoord.DistanceTo(gymCoord);
                        //_logger.LogInformation($"Gym ({gymCoord}) distance from S2 cell ({cellCoord}): {dist}");
                        // Check if distance between gym and cell is less than 70m
                        //if (dist <= 750)//70)
                        //{
                        //if (!_smartRaidGymsInPoint.ContainsKey(cellCoord))
                        //{
                        //    _smartRaidGymsInPoint.Add(cellCoord, new List<string>());
                        //}

                        _smartRaidGymsInPoint[cellCoord] = gymIds; //.Add(gym.Id);
                        _smartRaidPointsUpdated[cellCoord] = 0;
                        //}

                        foreach (var gym in gyms)
                        {
                            // REVIEW: Shoot, when did .NET allow setting dictionary value without
                            // adding key to dictionary first and just using key index.
                            // :thinking: Finally! <3
                            _smartRaidGyms[gym.Id] = gym;
                        }
                    }

                    //foreach (var gym in gyms)
                    //{
                    //    var gymCoord = new Coordinate(gym.Latitude, gym.Longitude);
                    //    var cellCoord = gym.CellId.ToCoordinate();
                    //    if (!_smartRaidGymsInPoint.ContainsKey(cellCoord))
                    //    {
                    //        _smartRaidGymsInPoint.Add(cellCoord, new List<string>());
                    //    }
                    //    _smartRaidGymsInPoint[cellCoord].Add(gym.Id);
                    //    _smartRaidPointsUpdated[cellCoord] = 0;

                    //    // REVIEW: Shoot, when did .NET allow setting dictionary value without
                    //    // adding key to dictionary first and just using key index.
                    //    // :thinking: Finally! <3
                    //    _smartRaidGyms[gym.Id] = gym;
                    //}
                }
                catch (Exception ex)
                {
                    _logger.LogError($"LoadGymsAsync: {ex}");
                    // Sleep for 5 seconds
                    Thread.Sleep(5000);
                }
            }

            var coords = _smartRaidGyms.Values.Select(gym => new Coordinate(gym.Latitude, gym.Longitude)).ToList();

            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            _logger.LogDebug($"Took: {totalSeconds}s");
        }
        */

        private GymsResult GetGymsToCheck()
        {
            var noRaid = new List<SmartRaidGym>();
            var noBoss = new List<SmartRaidGym>();
            var now = DateTime.UtcNow.ToTotalSeconds();

            // Loop through gym/raid list for specified coordinates
            foreach (var (point, gymIds) in _smartRaidGymsInPoint)
            {
                // Check if gym hasn't been updated or visited in the last 60 seconds
                // or if gym raid egg is close to hatching.
                var updated = _smartRaidPointsUpdated[point];
                var shouldUpdateEgg = updated == 0 || now >= updated + IgnoreTimeEggS;
                var shouldUpdateBoss = updated == 0 || now >= updated + IgnoreTimeBossS;

                // Loop through gyms within proximity of gym coordinates
                foreach (var gymId in gymIds)
                {
                    if (!_smartRaidGyms.ContainsKey(gymId))
                    {
                        // Does not contain smart raid by gym id
                        continue;
                    }
                    var gym = _smartRaidGyms[gymId];
                    if ((shouldUpdateEgg && gym.RaidEndTimestamp == null) ||
                        (now >= gym.RaidEndTimestamp + NoRaidTimeS))
                    {
                        noRaid.Add(new SmartRaidGym
                        {
                            Gym = gym,
                            Updated = updated,
                            Coordinate = point,
                        });
                    }
                    else if (shouldUpdateBoss &&
                        (gym.RaidPokemonId == null || gym.RaidPokemonId == 0) &&
                        gym.RaidBattleTimestamp != null &&
                        gym.RaidEndTimestamp != null &&
                        now > gym.RaidBattleTimestamp -
                        RaidInfoBeforeHatchS &&
                        now <= gym.RaidEndTimestamp)
                    {
                        noBoss.Add(new SmartRaidGym
                        {
                            Gym = gym,
                            Updated = updated,
                            Coordinate = point,
                        });
                    }
                }
            }

            return new GymsResult
            {
                NoBoss = noBoss,
                NoRaid = noRaid,
            };
        }

        private Coordinate? GetNextScanLocation()
        {
            // Build list of gyms to check
            var gyms = GetGymsToCheck();

            Coordinate? coord = null;
            var now = DateTime.UtcNow.ToTotalSeconds();
            if (gyms.NoBoss.Count > 0)
            {
                // Sort gyms with no raid boss by last updated timestamp
                gyms.NoBoss.Sort((gym1, gym2) => gym1.Updated.CompareTo(gym2.Updated));
                // Get first gym with no raid boss from list
                var raid = gyms.NoBoss.FirstOrDefault();
                if (raid != null)
                {
                    // Set return result to gym location which will be the next task
                    coord = raid.Coordinate;

                    // Set last updated timestamp for gym to now
                    _smartRaidPointsUpdated[coord] = now;
                }
            }
            else if (gyms.NoRaid.Count > 0)
            {
                // Sort gyms with no active raid by last updated timestamp
                gyms.NoRaid.Sort((gym1, gym2) => gym1.Updated.CompareTo(gym2.Updated));
                // Get first gym with no active raid from list
                var raid = gyms.NoRaid.FirstOrDefault();
                if (raid != null)
                {
                    // Set return result to gym location which will be the next task
                    coord = raid.Coordinate;

                    // Set last updated timestamp for gym to now
                    _smartRaidPointsUpdated[coord] = now;
                }
            }
            return coord;
        }

        private async Task UpdateGymInfoAsync()
        {
            // Instead of running a query for all gyms and filtering,
            // retrieve all gyms from the database and filter which
            // are gym id matches. _Should increase SQL performance._
            //
            // Benchmark - (query all vs individual)
            // All: 0.0216 0.0188 0.0202 vs Individual: 0.071 0.0648 0.06
            // Roughly about 3-4 times faster. Although it's pretty much
            // the same logic. :thinking:

            var gymIds = _smartRaidGyms.Keys.ToList();
            var gyms = await GetGymsAsync(gymIds);
            if ((gyms?.Count ?? 0) == 0)
            {
                // Failed to get gyms by ids
                _logger.LogWarning($"Failed to get list of gyms by ids");
                Thread.Sleep(5000);
                return;
            }

            // Retrieve and sync all gyms/gym info from the database that match
            // the gym ids from the smart raid gyms cache.
            foreach (var gym in gyms)
            {
                _smartRaidGyms[gym.Id] = gym;
            }
        }

        private void UpdateStats()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            lock (_statsLock)
            {
                // Set start date if not set already
                if (_startDate == 0)
                {
                    _startDate = now;
                }
                // If count is at it's maximum value, reset to 0
                if (_count == ulong.MaxValue)
                {
                    _count = 0;
                    _startDate = now;
                }
                else
                {
                    // Increment gym stat count
                    _count++;
                }
            }
        }

        #endregion

        #region Database Helpers

        private async Task<List<Gym>> GetGymsAsync(List<string>? ids = null)
        {
            using (var context = _factory.CreateDbContext())
            {
                if ((ids?.Count ?? 0) == 0)
                {
                    var allGyms = context.Gyms.ToList();
                    return await Task.FromResult(allGyms);
                }

                var gyms = context.Gyms.Where(gym => ids.Contains(gym.Id))
                                       .ToList();
                return gyms;
            }
        }

        /*
        private async Task<List<Gym>> GetGymsAsync(BoundingBox bbox)
        {
            using (var context = _factory.CreateDbContext())
            {
                var gyms = context.Gyms.AsEnumerable()
                                       .Where(gym => bbox.IsInBoundingBox(gym.Latitude, gym.Longitude))
                                       .ToList();
                return await Task.FromResult(gyms);
            }
        }

        private async Task<List<Gym>> GetGymsAsync(List<MultiPolygon> multiPolygons)
        {
            using (var context = _factory.CreateDbContext())
            {
                var gyms = context.Gyms.AsEnumerable()
                                       .Where(gym => GeofenceService.InMultiPolygon(multiPolygons, gym.Latitude, gym.Longitude))
                                       .ToList();
                return await Task.FromResult(gyms);
            }
        }
        */

        #endregion
    }
}