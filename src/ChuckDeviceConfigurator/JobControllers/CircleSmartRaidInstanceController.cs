namespace ChuckDeviceConfigurator.JobControllers
{
    using Google.Common.Geometry;
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

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

        public IReadOnlyList<Coordinate> Coordinates { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        #endregion

        #region Constructor

        public CircleSmartRaidInstanceController(IDbContextFactory<MapDataContext> factory, Instance instance, List<Coordinate> coords)
        {
            Name = instance.Name;
            Coordinates = coords;
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

            _timer = new System.Timers.Timer
            {
                Interval = 30 * 1000, // 30 second interval
            };
            _timer.Elapsed += async (sender, e) => await UpdateGymInfoAsync();
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false)
        {
            var coord = GetNextScanLocation();
            if (coord == null)
            {
                // Unable to retrieve coordinate for next gym to check
                _logger.LogWarning($"[{Name}] [{uuid}] Failed to retrieve gym coordinates for next job task.");
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
            uint? scansPerHour = null;
            var now = DateTime.UtcNow.ToTotalSeconds();
            lock (_statsLock)
            {
                if (_startDate > 0)
                {
                    var delta = now - _startDate;
                    scansPerHour = Convert.ToUInt32(_count / delta * 3600);
                }
            }
            var scansStatus = scansPerHour == null
                ? "--" :
                Convert.ToString(scansPerHour ?? 0);
            var status = $"Scans/h: {scansStatus}";
            return await Task.FromResult(status);
        }

        public void Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // Clear gyms cache and load gyms again
            _smartRaidGyms.Clear();
            _smartRaidGymsInPoint.Clear();
            _smartRaidPointsUpdated.Clear();

            // Load/gym gyms list for smart raid cache
            LoadGymsAsync();
        }

        public void Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            _timer.Stop();
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
            // Roughly about 6 times faster.
            var allGyms = await GetAllGyms();
            foreach (var coord in Coordinates)
            {
                var latlng = S2LatLng.FromDegrees(coord.Latitude, coord.Longitude);
                var cellIds = latlng.GetLoadedS2CellIds()
                                    .Select(x => x.Id)
                                    .ToList();
                try
                {
                    //var gyms = await GetGymsByCellIdsAsync(cellIds);
                    var gyms = allGyms.Where(gym => cellIds.Contains(gym.CellId))
                                      .ToList();
                    var gymIds = gyms.Select(gym => gym.Id).ToList();
                    _smartRaidGymsInPoint[coord] = gymIds;
                    _smartRaidPointsUpdated[coord] = 0;

                    foreach (var gym in gyms)
                    {
                        if (!_smartRaidGyms.ContainsKey(gym.Id) || _smartRaidGyms[gym.Id] == null)
                        {
                            // REVIEW: Shoot, when did .NET allow setting dictionary value without
                            // adding key to dictionary first and just using key index.
                            // :thinking: Finally! <3
                            _smartRaidGyms[gym.Id] = gym;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"LoadGymsAsync: {ex}");
                    // Sleep for 5 seconds
                    Thread.Sleep(5000);
                }
            }
        }

        private GymsResult GetGymsToCheck()
        {
            var noRaid = new List<SmartRaidGym>();
            var noBoss = new List<SmartRaidGym>();
            var now = DateTime.UtcNow.ToTotalSeconds();

            // Loop through gym/raid list for specified coordinates
            foreach (var (point, gymIds) in _smartRaidGymsInPoint)
            {
                // Ch
                var updated = _smartRaidPointsUpdated[point];
                var shouldUpdateEgg = updated == 0 || now >= updated + IgnoreTimeEggS;
                var shouldUpdateBoss = updated == 0 || now >= updated + IgnoreTimeBossS;

                // Loop through gyms within proximity of gym coordinates
                foreach (var gymId in gymIds)
                {
                    if (!_smartRaidGyms.ContainsKey(gymId))
                    {
                        // TODO: Does not contain smart raid by gym id
                        continue;
                    }
                    var gym = _smartRaidGyms[gymId];
                    if ((shouldUpdateEgg && gym.RaidEndTimestamp == null) ||
                        now >= gym.RaidEndTimestamp + NoRaidTimeS)
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
                    // Set last updated timestamp for gym to now
                    _smartRaidPointsUpdated[raid.Coordinate] = now;
                    // Set return result to gym location which will be the next task
                    coord = raid.Coordinate;
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
                    // Set last updated timestamp for gym to now
                    _smartRaidPointsUpdated[raid.Coordinate] = now;
                    // Set return result to gym location which will be the next task
                    coord = raid.Coordinate;
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
            //var gyms = await GetGymsByIdsAsync(gymIds);
            var allGyms = await GetAllGyms();
            var gyms = allGyms.Where(gym => gymIds.Contains(gym.Id)).ToList();
            if (gyms == null)
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

        private async Task<List<Gym>> GetAllGyms()
        {
            using (var context = _factory.CreateDbContext())
            {
                var gyms = context.Gyms.ToList();
                return await Task.FromResult(gyms);
            }
        }

        /*
        private async Task<List<Gym>> GetGymsByIdsAsync(List<string> gymIds)
        {
            using (var context = _factory.CreateDbContext())
            {
                var gyms = context.Gyms.Where(gym => gymIds.Contains(gym.Id)).ToList();
                return await Task.FromResult(gyms);
            }
        }

        private async Task<List<Gym>> GetGymsByCellIdsAsync(List<ulong> cellIds)
        {
            using (var context = _factory.CreateDbContext())
            {
                var gyms = context.Gyms.Where(gym => cellIds.Contains(gym.CellId)).ToList();
                return await Task.FromResult(gyms);
            }
        }
        */

        #endregion
    }
}