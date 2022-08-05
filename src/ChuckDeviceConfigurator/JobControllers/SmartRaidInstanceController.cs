namespace ChuckDeviceConfigurator.JobControllers
{
    using Google.Common.Geometry;
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Common.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class SmartRaidInstanceController : IJobController
    {
        #region Constants

        private const ushort RaidInfoBeforeHatchS = 120; // 2 minutes
        private const ushort IgnoreTimeEggS = 150; // 2.5 minutes
        private const ushort IgnoreTimeBossS = 60; // 1 minute
        private const ushort NoRaidTimeS = Strings.ThirtyMinutesS; // 30 minutes
        private const ushort GymInfoSyncS = 30; // 30 seconds

        #endregion

        #region Variables

        private readonly ILogger<SmartRaidInstanceController> _logger;
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

        public SmartRaidInstanceController(
            IDbContextFactory<MapDataContext> factory,
            Instance instance,
            List<MultiPolygon> multiPolygons)
        {
            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            _factory = factory;
            _logger = new Logger<SmartRaidInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _smartRaidGyms = new Dictionary<string, Gym>();
            _smartRaidGymsInPoint = new Dictionary<Coordinate, List<string>>();
            _smartRaidPointsUpdated = new Dictionary<Coordinate, ulong>();

            // Load/build gyms list for smart raid cache
            LoadGymsAsync();

            _timer = new System.Timers.Timer(GymInfoSyncS * 1000); // 30 second interval
            _timer.Elapsed += async (sender, e) => await UpdateGymInfoAsync();
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
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
            // Check how many gyms haven't been updated in the last x (30) minutes?
            //var gymsToCheck = GetGymsToCheck();
            var now = DateTime.UtcNow.ToTotalSeconds();
            var gymsTotal = _smartRaidGyms.Count;
            var gymsWithActiveRaidsCount = _smartRaidGyms.Count(gym => gym.Value.RaidEndTimestamp > now);
            var gymsWithoutActiveRaidsCount = gymsTotal - gymsWithActiveRaidsCount;
            //var gymsWithRaidsUpdatedRecently = _smar
            //var gymsUpdated = _smartRaidPointsUpdated.Values.Count(gymUpdated => gymUpdated > 0 && (now - gymUpdated) <= Strings.ThirtyMinutesS);
            //var gymsNotUpdated = gymsTotal - gymsUpdated;

            uint? scansPerHour = null;
            lock (_statsLock)
            {
                if (_startDate > 0)
                {
                    var delta = now - _startDate;
                    // Prevent dividing by zero
                    if (_count > 0 && delta > 0)
                    {
                        scansPerHour = Convert.ToUInt32((double)_count / (double)delta * Strings.SixtyMinutesS);
                    }
                }
            }
            var scansStatus = scansPerHour > 0
                ? Convert.ToString(scansPerHour ?? 0)
                : Strings.DefaultInstanceStatus;
            //var gymsUpdatedCount = gymsTotal - (gymsToCheck.NoRaid.Count + gymsToCheck.NoBoss.Count);
            //var gymsStatus = $", (Gyms Updated: {gymsUpdated:N0}/{gymsTotal:N0})";
            //var gymsStatus = $", (Raids Updated: {gymsUpdatedCount:N0}/{gymsTotal:N0})";
            var gymsStatus = scansStatus == Strings.DefaultInstanceStatus
                ? ""
                : $", (Active: {gymsWithActiveRaidsCount:N0}/{gymsWithoutActiveRaidsCount:N0}|{gymsTotal:N0})";
            var status = $"Scans/h: {scansStatus}{gymsStatus}";
            return await Task.FromResult(status);
        }

        public Task ReloadAsync()
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

        public Task StopAsync()
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

            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            // TODO: Get S2 cells within multi polygon bounding box instead of generating route

            var allGyms = await GetGymsAsync();
            var routeGenerator = new RouteGenerator(_factory);
            var route = routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                CircleSize = 500, // TODO: Make 'CircleSize' configurable?
                MaximumPoints = 500,
                MultiPolygons = (List<MultiPolygon>)MultiPolygons,
                RouteType = RouteGenerationType.Bootstrap,
            });

            // Optimize bootstrapped route
            var routeCalculator = new RouteCalculator(new List<Coordinate>(route));
            var optimizedRoute = routeCalculator.CalculateShortestRoute();

            foreach (var coord in route)
            {
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

            //sw.Stop();
            //var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            //_logger.LogDebug($"Took: {totalSeconds}s");
        }

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
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            var gymIds = _smartRaidGyms.Keys.ToList();
            var gyms = await GetGymsAsync(gymIds);
            if ((gyms?.Count ?? 0) == 0)
            {
                // Failed to get gyms by ids
                _logger.LogWarning($"[{Name}] Nearby gyms list is empty.");
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
                // If count is at the maximum value, reset to 0
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

        #endregion

        private class SmartRaidGym
        {
            public Gym Gym { get; set; }

            public ulong Updated { get; set; }

            public Coordinate Coordinate { get; set; }
        }

        private class GymsResult
        {
            public List<SmartRaidGym> NoBoss { get; set; } = new();

            public List<SmartRaidGym> NoRaid { get; set; } = new();
        }
    }
}