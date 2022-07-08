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

    public class CircleSmartRaidInstanceController : IJobController
    {
        #region Constants

        private const ushort RaidInfoBeforeHatch = 120; // 2 minutes
        private const ushort IgnoreTimeEgg = 150; // 2.5 minutes
        private const ushort IgnoreTimeBoss = 60; // 1 minute
        private const ushort NoRaidTime = 1800; // 30 minutes

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

        public string Name { get; set; }

        public IReadOnlyList<Coordinate> Coordinates { get; set; }

        public ushort MinimumLevel { get; set; }

        public ushort MaximumLevel { get; set; }

        public string GroupName { get; set; }

        public bool IsEvent { get; set; }

        #endregion

        #region Constructor

        public CircleSmartRaidInstanceController(IDbContextFactory<MapDataContext> factory, Instance instance, List<Coordinate> coords)
        {
            Name = instance.Name;
            Coordinates = coords;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? null;
            IsEvent = instance.Data?.IsEvent ?? false;

            _factory = factory;
            _logger = new Logger<CircleSmartRaidInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _smartRaidGyms = new Dictionary<string, Gym>();
            _smartRaidGymsInPoint = new Dictionary<Coordinate, List<string>>();
            _smartRaidPointsUpdated = new Dictionary<Coordinate, ulong>();

            LoadGyms();

            _timer = new System.Timers.Timer();
            _timer.Interval = 30 * 1000; // 30 second interval
            _timer.Elapsed += async (sender, e) => await RaidUpdateHandlerAsync();
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false)
        {
            // Build list of gyms to check
            var gymsNoRaid = new List<SmartRaidGym>();
            var gymsNoBoss = new List<SmartRaidGym>();
            var now = DateTime.UtcNow.ToTotalSeconds();
            foreach (var (point, gyms) in _smartRaidGymsInPoint)
            {
                var updated = _smartRaidPointsUpdated[point];
                var shouldUpdateEgg = updated == 0 || now >= updated + IgnoreTimeEgg;
                var shouldUpdateBoss = updated == 0 || now >= updated + IgnoreTimeBoss;
                foreach (var id in gyms)
                {
                    if (!_smartRaidGyms.ContainsKey(id))
                    {
                        // TODO: Does not contain smart raid by gym id
                        continue;
                    }
                    var gym = _smartRaidGyms[id];
                    if (shouldUpdateEgg && gym.RaidEndTimestamp == null ||
                        now >= gym.RaidEndTimestamp + NoRaidTime)
                    {
                        gymsNoRaid.Add(new SmartRaidGym
                        {
                            Gym = gym,
                            Updated = updated,
                            Coordinate = point,
                        });
                    } else if (shouldUpdateBoss &&
                        (gym.RaidPokemonId == null || gym.RaidPokemonId == 0) &&
                        gym.RaidBattleTimestamp != null &&
                        gym.RaidEndTimestamp != null &&
                        now > gym.RaidBattleTimestamp -
                        RaidInfoBeforeHatch &&
                        now <= gym.RaidEndTimestamp)
                    {
                        gymsNoBoss.Add(new SmartRaidGym
                        {
                            Gym = gym,
                            Updated = updated,
                            Coordinate = point,
                        });
                    }
                }
            }

            Coordinate? coord = null;
            var timestamp = DateTime.UtcNow.ToTotalSeconds();
            if (gymsNoBoss.Count > 0)
            {
                gymsNoBoss.Sort((gym1, gym2) => gym1.Updated.CompareTo(gym2.Updated));
                var raid = gymsNoBoss.FirstOrDefault();
                if (raid != null)
                {
                    _smartRaidPointsUpdated[raid.Coordinate] = timestamp;
                    coord = raid.Coordinate;
                }
            }
            else if (gymsNoRaid.Count > 0)
            {
                gymsNoRaid.Sort((gym1, gym2) => gym1.Updated.CompareTo(gym2.Updated));
                var raid = gymsNoRaid.FirstOrDefault();
                if (raid != null)
                {
                    _smartRaidPointsUpdated[raid.Coordinate] = timestamp;
                    coord = raid.Coordinate;
                }
            }

            if (coord == null)
            {
                return null;
            }

            lock (_statsLock)
            {
                if (_startDate == 0)
                {
                    _startDate = timestamp;
                }
                if (_count == ulong.MaxValue)
                {
                    _count = 0;
                    _startDate = timestamp;
                }
                else
                {
                    _count++;
                }
            }

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
                    double start = _startDate;
                    double count = _count;
                    scansPerHour = Convert.ToUInt32(_count / (now - start) * 3600);
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
            // TODO: Clear gyms and load gyms again
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
        }

        #endregion

        #region Private Methods

        private async void LoadGyms()
        {
            foreach (var coord in Coordinates)
            {
                var latlng = S2LatLng.FromDegrees(coord.Latitude, coord.Longitude);
                var cellIds = latlng.GetLoadedS2CellIds()
                                    .Select(x => x.Id)
                                    .ToList();
                try
                {
                    var gyms = await GetGymsByCellIdsAsync(cellIds);
                    var gymIds = gyms.Select(gym => gym.Id).ToList();
                    _smartRaidGymsInPoint[coord] = gymIds;
                    _smartRaidPointsUpdated[coord] = 0;
                    foreach (var gym in gyms)
                    {
                        if (!_smartRaidGyms.ContainsKey(gym.Id) || _smartRaidGyms[gym.Id] == null)
                        {
                            _smartRaidGyms[gym.Id] = gym;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"LoadGyms: {ex}");
                    // Sleep for 5 seconds
                    Thread.Sleep(5000);
                }
            }
        }

        private async Task RaidUpdateHandlerAsync()
        {
            var gymIds = _smartRaidGyms.Keys.ToList();
            var gyms = await GetGymsByIdsAsync(gymIds);
            if (gyms == null)
            {
                // TODO: Failed to get gyms with ids
                Thread.Sleep(5000);
                return;
            }

            foreach (var gym in gyms)
            {
                _smartRaidGyms[gym.Id] = gym;
            }
        }

        #endregion

        #region Database Helpers

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

        #endregion
    }
}