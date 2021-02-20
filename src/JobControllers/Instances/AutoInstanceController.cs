namespace ChuckDeviceController.JobControllers.Instances
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geofence;
    using ChuckDeviceController.Geofence.Models;
    using ChuckDeviceController.JobControllers.Tasks;
    using Google.Common.Geometry;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public enum AutoType
    {
        Quest,
    }

    public class AutoInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<AutoInstanceController> _logger;

        private readonly List<Pokestop> _allStops;
        private readonly Dictionary<string, Pokestop> _todayStops;
        private readonly Dictionary<string, int> _todayStopsTries;
        private List<ulong> _bootstrapCellIds;
        private readonly System.Timers.Timer _timer;
        private int _bootstrapTotalCount = 0;
        private DateTime _completionDate;
        private ulong _lastCompletionCheck = DateTime.UtcNow.ToTotalSeconds() - 3600;
        private bool _shouldExit;

        // Entity repositories
        private readonly AccountRepository _accountRepository;
        private readonly PokestopRepository _pokestopRepository;
        private readonly CellRepository _cellRepository;

        // TODO: Use lock for stops list

        #endregion

        #region Properties

        public string Name { get; set; }

        public List<MultiPolygon> MultiPolygon { get; set; }

        public AutoType Type { get; set; }

        public int TimezoneOffset { get; set; }

        public ushort MinimumLevel { get; set; }

        public ushort MaximumLevel { get; set; }

        public int SpinLimit { get; set; }

        #endregion

        #region Constructor

        public AutoInstanceController(string name, List<MultiPolygon> multiPolygon, AutoType type, int timezoneOffset, ushort minLevel, ushort maxLevel, int spinLimit)
        {
            Name = name;
            MultiPolygon = multiPolygon;
            Type = type;
            TimezoneOffset = timezoneOffset;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            SpinLimit = spinLimit;

            _logger = new Logger<AutoInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _allStops = new List<Pokestop>();
            _todayStops = new Dictionary<string, Pokestop>();
            _todayStopsTries = new Dictionary<string, int>();
            _bootstrapCellIds = new List<ulong>();

            _accountRepository = new AccountRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _pokestopRepository = new PokestopRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _cellRepository = new CellRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));

            ulong timeLeft = DateTime.Now.SecondsUntilMidnight();
            _timer = new System.Timers.Timer
            {
                Interval = timeLeft * 1000,
            };
            _timer.Elapsed += async (sender, e) => await ClearQuests().ConfigureAwait(false);
            _timer.Start();
            // TODO: Get 12am DateTime
            _logger.LogInformation($"[{Name}] Clearing Quests in {timeLeft:N0}s at 12:00 AM (Currently: {DateTime.Now})");

            Update().ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            Bootstrap().ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTask(string uuid, string accountUsername, bool startup)
        {
            switch (Type)
            {
                case AutoType.Quest:
                    if (_bootstrapCellIds.Count > 0)
                    {
                        return await GetBootstrapTask().ConfigureAwait(false);
                    }

                    // TODO: Check InstanceController.NoRequireAccount (username == null and account == null)

                    if (_allStops.Count == 0)
                    {
                        return null;
                    }

                    if (_todayStops.Count == 0)
                    {
                        // Check last completed date
                        ulong now = DateTime.UtcNow.ToTotalSeconds();
                        if (now - _lastCompletionCheck >= 600)
                        {
                            if (_completionDate == default)
                            {
                                _completionDate = DateTime.UtcNow;
                            }
                            await AssignmentController.Instance.InstanceControllerDone(Name).ConfigureAwait(false);
                            return null;
                        }
                        _lastCompletionCheck = now;

                        List<string> ids = _allStops.ConvertAll(x => x.Id);
                        List<Pokestop> newStops = new List<Pokestop>();
                        try
                        {
                            newStops = await _pokestopRepository.GetByIdsAsync(ids).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Failed to get list of Pokestops with ids {string.Join(",", ids)}: {ex}");
                        }

                        foreach (Pokestop stop in newStops)
                        {
                            if (!_todayStopsTries.ContainsKey(stop.Id))
                            {
                                _todayStopsTries.Add(stop.Id, 0);
                            }
                            int count = _todayStopsTries[stop.Id];
                            if (stop.QuestType == null && stop.Enabled && count <= 5)
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                        // Check if all stops have quests, if so call on complete
                        if (_todayStops.Count == 0)
                        {
                            if (_completionDate == default)
                            {
                                _completionDate = DateTime.UtcNow;
                            }
                            _logger.LogInformation($"[{Name}] Instance done");
                            await AssignmentController.Instance.InstanceControllerDone(Name).ConfigureAwait(false);
                            return null;
                        }
                    }

                    double? lastLat = null;
                    double? lastLon = null;
                    ulong lastTime = 0;
                    Account account = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(accountUsername))
                        {
                            account = await _accountRepository.GetByIdAsync(accountUsername).ConfigureAwait(false);
                            lastLat = account.LastEncounterLatitude ?? 0;
                            lastLon = account.LastEncounterLongitude ?? 0;
                            lastTime = account.LastEncounterTime ?? 0;
                        }
                        else
                        {
                            //lastLat = Double(DBController.global.getValueForKey(key: "AIC_\(uuid)_last_lat") ?? "")
                            //lastLon = Double(DBController.global.getValueForKey(key: "AIC_\(uuid)_last_lon") ?? "")
                            //lastTime = UInt32(DBController.global.getValueForKey(key: "AIC_\(uuid)_last_time") ?? "")
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[{Name}] Error: {ex}");
                    }

                    // TODO: Check cooldown

                    if (!string.IsNullOrEmpty(accountUsername) && account != null)
                    {
                        if (account.Spins >= SpinLimit)
                        {
                            _logger.LogWarning($"[{Name}] {accountUsername} hit max spin limit {account.Spins}/{SpinLimit}, switching accounts...");
                            return new QuestTask
                            {
                                Action = ActionType.SwitchAccount,
                                MinimumLevel = MinimumLevel,
                                MaximumLevel = MaximumLevel,
                            };
                        }
                    }

                    double newLat;
                    double newLon;
                    ulong encounterTime;
                    Pokestop pokestop = null;
                    if (lastLat.HasValue && lastLon.HasValue)
                    {
                        Coordinate current = new Coordinate(lastLat.Value, lastLon.Value);
                        Dictionary<string, Pokestop> todayStopsC = _todayStops;
                        if (todayStopsC.Count == 0)
                        {
                            return null;
                        }

                        Pokestop closest = null;
                        double closestDistance = 10000000000000000d;
                        foreach ((string stopId, Pokestop stop) in todayStopsC)
                        {
                            Coordinate coord = new Coordinate(stop.Longitude, stop.Latitude);
                            double dist = current.DistanceTo(coord);
                            if (dist < closestDistance)
                            {
                                closest = stop;
                                closestDistance = dist;
                            }
                        }
                        if (closest == null)
                        {
                            return null;
                        }

                        newLat = closest.Latitude;
                        newLon = closest.Longitude;
                        pokestop = closest;

                        List<Pokestop> nearbyPokestops = new List<Pokestop>();
                        Coordinate pokestopCoord = new Coordinate(pokestop.Latitude, pokestop.Longitude);
                        foreach ((string id, Pokestop stop) in todayStopsC)
                        {
                            if (pokestopCoord.DistanceTo(new Coordinate(stop.Latitude, stop.Longitude)) <= 80)
                            {
                                nearbyPokestops.Add(stop);
                            }
                        }
                        foreach (Pokestop stop in nearbyPokestops)
                        {
                            int index = _todayStops.Keys.ToList().IndexOf(stop.Id);
                            if (index >= 0)
                            {
                                _todayStops.Remove(stop.Id);
                            }
                        }

                        ulong now = DateTime.UtcNow.ToTotalSeconds();
                        if (lastTime == 0)
                        {
                            encounterTime = now;
                        }
                        else
                        {
                            double encounterTimeT = lastTime + GetCooldownAmount(closestDistance);
                            if (encounterTimeT < now)
                            {
                                encounterTime = now;
                            }
                            else
                            {
                                encounterTime = (ulong)encounterTimeT;
                            }

                            if (encounterTime - now >= 7200)
                            {
                                encounterTime = now + 7200;
                            }
                        }
                        if (pokestop != null)
                        {
                            _todayStops.Remove(pokestop.Id);
                        }
                    }
                    else
                    {
                        Pokestop stop = _todayStops.FirstOrDefault().Value;
                        if (stop == null)
                        {
                            return null;
                        }

                        newLat = stop.Latitude;
                        newLon = stop.Longitude;
                        pokestop = stop;
                        encounterTime = DateTime.UtcNow.ToTotalSeconds();
                        _todayStops.Remove(_todayStops.Keys.FirstOrDefault());//Last());
                    }
                    await _accountRepository.SpinAsync(accountUsername).ConfigureAwait(false);

                    if (_todayStopsTries.ContainsKey(pokestop.Id))
                    {
                        _todayStopsTries[pokestop.Id]++;
                    }

                    if (!string.IsNullOrEmpty(accountUsername) && account != null)
                    {
                        await _accountRepository.SetLastEncounterAsync(accountUsername, pokestop.Latitude, pokestop.Longitude, encounterTime).ConfigureAwait(false);
                    }
                    else
                    {
                        // TODO: 
                    }

                    ulong delayT = DateTime.UtcNow.ToTotalSeconds() - encounterTime;
                    double delay = Convert.ToDouble(delayT == 0 ? 0 : delayT + 1);
                    _logger.LogDebug($"[{Name}] Delaying by {delay}");
                    if (_todayStops.Count == 0)
                    {
                        _lastCompletionCheck = DateTime.UtcNow.ToTotalSeconds();
                        List<string> ids = _allStops.ConvertAll(x => x.Id);
                        List<Pokestop> newStops = new List<Pokestop>();
                        try
                        {
                            newStops = await _pokestopRepository.GetByIdsAsync(ids).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Failed to get list of Pokestops by ids {string.Join(",", ids)}: {ex}");
                        }
                        foreach (Pokestop stop in newStops)
                        {
                            if (stop.QuestType == null && stop.Enabled)
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                        if (_todayStops.Count == 0)
                        {
                            _logger.LogInformation($"[{Name}] Instance done");
                            if (_lastCompletionCheck == default)
                            {
                                _lastCompletionCheck = DateTime.UtcNow.ToTotalSeconds();
                            }
                            await AssignmentController.Instance.InstanceControllerDone(Name).ConfigureAwait(false);
                        }
                    }
                    return new QuestTask
                    {
                        Area = Name,
                        Action = ActionType.ScanQuest,
                        Latitude = newLat,
                        Longitude = newLon,
                        Delay = delay,
                        MinimumLevel = MinimumLevel,
                        MaximumLevel = MaximumLevel,
                    };
            }
            return null;
        }

        public async Task<string> GetStatus()
        {
            switch (Type)
            {
                case AutoType.Quest:
                    if (_bootstrapCellIds.Count > 0)
                    {
                        double totalCount = _bootstrapTotalCount;
                        double count = (double)(totalCount - _bootstrapCellIds.Count);
                        double bootstrapPercentage = totalCount > 0
                            ? count / totalCount * 100.0
                            : 100d;
                        return $"Bootstrapping {count:N0}/{totalCount:N0} ({Math.Round(bootstrapPercentage, 2)}%)";
                    }
                    List<string> ids = _allStops.ConvertAll(x => x.Id);
                    double currentCountDb = await GetQuestCount(ids).ConfigureAwait(false);
                    double maxCount = _allStops.Count;
                    double currentCount = (double)(maxCount - _todayStops.Count);
                    double percentage = maxCount > 0
                        ? currentCount / maxCount * 100.0
                        : 100d;
                    double percentageReal = maxCount > 0
                        ? currentCountDb / maxCount * 100.0
                        : 100d;
                    return $"Status: {currentCountDb:N0}|{currentCount:N0}/{maxCount:N0} ({Math.Round(percentageReal, 2)}|{Math.Round(percentage, 2)}%{(_completionDate != default ? $", Completed: @ {_completionDate}" : "")})";
            }
            return null;
        }

        public async void Reload()
        {
            await Update().ConfigureAwait(false);
        }

        public void Stop()
        {
            _shouldExit = true;
            _timer.Stop();
        }

        #endregion

        #region Private Methods

        private async Task Bootstrap()
        {
            _logger.LogInformation($"[{Name}] Checking Bootstrap Status...");
            DateTime start = DateTime.UtcNow;
            int totalCount = 0;
            List<ulong> missingCellIds = new List<ulong>();
            foreach (MultiPolygon polygon in MultiPolygon)
            {
                Polygon first = polygon.FirstOrDefault();
                Polygon last = polygon.LastOrDefault();
                // Check null's
                if (first == null || last == null)
                {
                    continue;
                }
                // Make sure first and last coords are the same
                if (first[0] != last[0] ||
                    first[1] != last[1])
                {
                    polygon.Add(first);
                }

                List<ulong> s2CellIds = polygon.GetS2CellIDs(15, 15, int.MaxValue);
                totalCount += s2CellIds.Count;
                List<Cell> cells = new List<Cell>();
                try
                {
                    cells = await GetCellsByIDs(s2CellIds).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{Name}] Error: {ex}");
                }
                IEnumerable<ulong> existingCellIds = cells.Select(x => x.Id);
                foreach (ulong s2cellId in s2CellIds)
                {
                    // If Cell Id doesn't exist, add to bootstrap list
                    if (!existingCellIds.Contains(s2cellId))
                    {
                        missingCellIds.Add(s2cellId);
                    }
                }
            }
            _logger.LogInformation($"[{Name}] Bootstrap Status: {totalCount - missingCellIds.Count}/{totalCount} after {DateTime.UtcNow.Subtract(start).TotalSeconds:N0} seconds");
            _bootstrapCellIds = missingCellIds;
            _bootstrapTotalCount = totalCount;
        }

        private async Task Update()
        {
            switch (Type)
            {
                case AutoType.Quest:
                    _allStops.Clear();
                    foreach (MultiPolygon polygon in MultiPolygon)
                    {
                        try
                        {
                            // Make sure first and last coords are the same
                            Polygon first = polygon.FirstOrDefault();
                            Polygon last = polygon.LastOrDefault();
                            // Check null's
                            if (first == null || last == null)
                            {
                                continue;
                            }
                            // Make sure first and last coords are the same
                            if (first[0] != last[0] ||
                                first[1] != last[1])
                            {
                                polygon.Add(first);
                            }
                            // Get all existing Pokestops within geofence bounds
                            BoundingBox bounds = polygon.GetBoundingBox();
                            List<Pokestop> stops = await _pokestopRepository.GetAllAsync(bounds).ConfigureAwait(false);
                            foreach (Pokestop stop in stops)
                            {
                                // Check if Pokestop is within geofence
                                if (GeofenceService.InPolygon(polygon, stop.Latitude, stop.Longitude))
                                {
                                    _allStops.Add(stop);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Error: {ex}");
                        }
                    }
                    _todayStops.Clear();
                    _todayStopsTries.Clear();
                    _completionDate = default;
                    foreach (Pokestop stop in _allStops)
                    {
                        if (stop.QuestType == null && stop.Enabled)
                        {
                            if (!_todayStops.ContainsKey(stop.Id))
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                    }
                    break;
            }
        }

        private async Task ClearQuests()
        {
            _timer.Stop();
            ulong now = DateTime.Now.SecondsUntilMidnight();
            _timer.Interval = now == 0 ? 1000 : now * 1000;
            _timer.Start();

            if (_shouldExit)
            {
                return;
            }

            if (_allStops.Count == 0)
            {
                _logger.LogWarning($"[{Name}] Tried clearing quests but no pokestops.");
                return;
            }
            _logger.LogDebug($"[{Name}] Getting pokestop ids");
            IEnumerable<string> ids = _allStops.Select(x => x.Id);
            _logger.LogInformation($"[{Name}] Clearing Quests for ids: {string.Join(",", ids)}");
            try
            {
                await _pokestopRepository.ClearQuestsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{Name}] Failed to clear quests: {ex}");
                if (_shouldExit)
                {
                    return;
                }
            }
            await Update().ConfigureAwait(false);
        }

        private async Task<BootstrapTask> GetBootstrapTask()
        {
            ulong target = _bootstrapCellIds.PopLast(out _bootstrapCellIds);
            if (target == default)
            {
                return null;
            }

            S2Cell cell = new S2Cell(new S2CellId(target));
            S2Point center = cell.Center;
            S2Point point = new S2Point(center.X, center.Y, center.Z);
            S2LatLng latlng = new S2LatLng(point);
            // Get all cells touching a 630 (-5m for error) circle at center
            const double radians = 0.00009799064306948; // 625m
            S2Cap circle = S2Cap.FromAxisHeight(center, (radians * radians) / 2);
            S2RegionCoverer coverer = new S2RegionCoverer
            {
                MinLevel = 15,
                MaxLevel = 15,
                MaxCells = 100
            };
            S2CellUnion cellIds = coverer.GetCovering(circle);
            foreach (S2CellId cellId in cellIds)
            {
                if (_bootstrapCellIds.Contains(cellId.Id))
                {
                    _bootstrapCellIds.Remove(cellId.Id);
                }
            }
            if (_bootstrapCellIds.Count == 0)
            {
                // TODO: await Bootstrap();
                if (_bootstrapCellIds.Count == 0)
                {
                    await Update().ConfigureAwait(false);
                }
            }
            return new BootstrapTask
            {
                Action = ActionType.ScanRaid,
                Area = Name,
                Latitude = latlng.LatDegrees,
                Longitude = latlng.LngDegrees,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
        }

        private async Task<List<Cell>> GetCellsByIDs(List<ulong> ids)
        {
            const double MAX_COUNT = 10000.0;
            if (ids.Count > MAX_COUNT)
            {
                List<Cell> list = new List<Cell>();
                double count = Math.Ceiling(ids.Count / MAX_COUNT);
                for (int i = 0; i < count; i++)
                {
                    int start = (int)MAX_COUNT * i;
                    int end = (int)Math.Min(MAX_COUNT * (i + 1), ids.Count - 1);
                    List<ulong> slice = ids.Slice(start, end);
                    List<Cell> result = await GetCellsByIDs(slice).ConfigureAwait(false);
                    if (result.Count > 0)
                    {
                        result.ForEach(x => list.Add(x));
                    }
                }
                return list;
            }
            if (ids.Count == 0)
            {
                return new List<Cell>();
            }
            return (List<Cell>)await _cellRepository.GetByIdsAsync(ids, true).ConfigureAwait(false);
        }

        private async Task<ulong> GetQuestCount(List<string> ids)
        {
            const double MAX_COUNT = 10000.0;
            if (ids.Count > MAX_COUNT)
            {
                ulong result = 0UL;
                double count = Math.Ceiling(ids.Count / MAX_COUNT);
                for (int i = 0; i < count; i++)
                {
                    int start = (int)MAX_COUNT * i;
                    int end = (int)Math.Min(MAX_COUNT * (i + 1), ids.Count - 1);
                    List<string> slice = ids.Slice(start, end);
                    ulong qResult = await GetQuestCount(slice).ConfigureAwait(false);
                    if (qResult > 0)
                    {
                        result += qResult;
                    }
                }
                return result;
            }
            if (ids.Count == 0)
            {
                return 0;
            }
            List<Pokestop> pokestops = await _pokestopRepository.GetByIdsAsync(ids).ConfigureAwait(false);
            return (ulong)pokestops.Where(x => !x.Deleted &&
                                               x.QuestType.HasValue &&
                                               x.QuestType != null).ToList().Count;
        }

        private static double GetCooldownAmount(double distanceM)
        {
            return Math.Min(Convert.ToUInt32(distanceM / 9.8), 7200);
        }

        #endregion
    }
}