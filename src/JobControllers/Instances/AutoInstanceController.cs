namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Google.Common.Geometry;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geofence;
    using ChuckDeviceController.Geofence.Models;
    using ChuckDeviceController.JobControllers.Tasks;
    using Microsoft.Extensions.Logging;

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

            var timeLeft = DateTime.Now.SecondsUntilMidnight();
            _timer = new System.Timers.Timer
            {
                Interval = timeLeft * 1000,
            };
            _timer.Elapsed += async (sender, e) => await ClearQuests();
            _timer.Start();
            // TODO: Get 12am DateTime
            _logger.LogInformation($"[{Name}] Clearing Quests in {timeLeft:N0}s at 12:00 AM (Currently: {DateTime.Now}");

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
                        return await GetBootstrapTask();
                    }

                    // TODO: Check InstanceController.NoRequireAccount (username == null and account == null)

                    if (_allStops.Count == 0)
                        return null;

                    if (_todayStops.Count == 0)
                    {
                        // Check last completed date
                        var now = DateTime.UtcNow.ToTotalSeconds();
                        if (now - _lastCompletionCheck >= 600)
                        {
                            if (_completionDate == default)
                            {
                                _completionDate = DateTime.UtcNow;
                            }
                            await AssignmentController.Instance.InstanceControllerDone(Name);
                            return null;
                        }
                        _lastCompletionCheck = now;

                        var ids = _allStops.Select(x => x.Id).ToList();
                        var newStops = new List<Pokestop>();
                        try
                        {
                            newStops = await _pokestopRepository.GetByIdsAsync(ids);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Failed to get list of Pokestops with ids {string.Join(",", ids)}: {ex}");
                        }

                        foreach (var stop in newStops)
                        {
                            if (!_todayStopsTries.ContainsKey(stop.Id))
                            {
                                _todayStopsTries.Add(stop.Id, 0);
                            }
                            var count = _todayStopsTries[stop.Id];
                            if (!stop.QuestType.HasValue && stop.Enabled && count <= 5)
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
                            await AssignmentController.Instance.InstanceControllerDone(Name);
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
                            account = await _accountRepository.GetByIdAsync(accountUsername);
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
                        var current = new Coordinate(lastLat.Value, lastLon.Value);
                        var todayStopsC = _todayStops;
                        if (todayStopsC.Count == 0)
                            return null;

                        Pokestop closest = null;
                        var closestDistance = 10000000000000000d;
                        foreach (var (stopId, stop) in todayStopsC)
                        {
                            var coord = new Coordinate(stop.Longitude, stop.Latitude);
                            var dist = current.DistanceTo(coord);
                            if (dist < closestDistance)
                            {
                                closest = stop;
                                closestDistance = dist;
                            }
                        }
                        if (closest == null)
                            return null;

                        newLat = closest.Latitude;
                        newLon = closest.Longitude;
                        pokestop = closest;
                        var now = DateTime.UtcNow.ToTotalSeconds();
                        if (lastTime == 0)
                        {
                            encounterTime = now;
                        }
                        else
                        {
                            var encounterTimeT = lastTime + GetCooldownAmount(closestDistance);
                            if (encounterTimeT < now)
                                encounterTime = now;
                            else
                                encounterTime = (ulong)encounterTimeT;
                            if (encounterTime - now >= 7200)
                                encounterTime = now + 7200;
                        }
                        if (pokestop != null)
                        {
                            _todayStops.Remove(pokestop.Id);
                        }
                    }
                    else
                    {
                        var stop = _todayStops.FirstOrDefault().Value;
                        if (stop == null)
                            return null;
                        newLat = stop.Latitude;
                        newLon = stop.Longitude;
                        pokestop = stop;
                        encounterTime = DateTime.UtcNow.ToTotalSeconds();
                        _todayStops.Remove(_todayStops.Keys.FirstOrDefault());//Last());
                    }
                    await _accountRepository.SpinAsync(accountUsername);

                    if (_todayStopsTries.ContainsKey(pokestop.Id))
                    {
                        _todayStopsTries[pokestop.Id]++;
                    }

                    if (!string.IsNullOrEmpty(accountUsername) && account != null)
                    {
                        await _accountRepository.SetLastEncounterAsync(accountUsername, pokestop.Latitude, pokestop.Longitude, encounterTime);
                    }
                    else
                    {
                        // TODO: 
                    }

                    var delayT = DateTime.UtcNow.ToTotalSeconds() - encounterTime;
                    var delay = Convert.ToDouble(delayT < 0 ? 0 : delayT + 1);
                    _logger.LogDebug($"[{Name}] Delaying by {delay}");
                    if (_todayStops.Count == 0)
                    {
                        _lastCompletionCheck = DateTime.UtcNow.ToTotalSeconds();
                        var ids = _allStops.Select(x => x.Id).ToList();
                        var newStops = new List<Pokestop>();
                        try
                        {
                            newStops = await _pokestopRepository.GetByIdsAsync(ids);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Failed to get list of Pokestops by ids {string.Join(",", ids)}: {ex}");
                        }
                        foreach (var stop in newStops)
                        {
                            if (stop.QuestType == null && stop.Enabled)
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                        if (_todayStops.Count == 0)
                        {
                            _logger.LogInformation($"[{Name}] Instance done");
                            await AssignmentController.Instance.InstanceControllerDone(Name);
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
                        var totalCount = (double)_bootstrapTotalCount;
                        var count = (double)(totalCount - _bootstrapCellIds.Count);
                        var bootstrapPercentage = totalCount > 0
                            ? count / totalCount * 100.0
                            : 100d;
                        return $"Bootstrapping {count}/{totalCount} ({Math.Round(bootstrapPercentage, 2)}%)";
                    }
                    var ids = _allStops.Select(x => x.Id).ToList();
                    var currentCountDb = (double)await GetQuestCount(ids);
                    var maxCount = (double)_allStops.Count;
                    var currentCount = (double)(maxCount - _todayStops.Count);
                    var percentage = maxCount > 0
                        ? currentCount / maxCount * 100.0
                        : 100d;
                    var percentageReal = maxCount > 0
                        ? currentCountDb / maxCount * 100.0
                        : 100d;
                    return $"Status: {currentCountDb}|{currentCount}/{maxCount} ({Math.Round(percentageReal, 2)}|{Math.Round(percentage, 2)}%{(_completionDate != default ? $", Completed: @ {_completionDate}" : "")})";
            }
            return null;
        }

        public async void Reload()
        {
            await Update();
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
            var start = DateTime.UtcNow;
            var totalCount = 0;
            var missingCellIds = new List<ulong>();
            foreach (var polygon in MultiPolygon)
            {
                var first = polygon.FirstOrDefault();
                var last = polygon.LastOrDefault();
                // Make sure first and last coords are the same
                if (first[0] != last[0] ||
                    first[1] != last[1])
                {
                    polygon.Add(first);
                }

                var cellIds = polygon.GetS2CellIDs(15, 15, int.MaxValue);
                totalCount += cellIds.Count;
                var cells = new List<Cell>();
                try
                {
                    cells = await GetCellsByIDs(cellIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{Name}] Error: {ex}");
                }
                var existingCellIds = cells.Select(x => x.Id);
                foreach (var cellId in cellIds)
                {
                    // If Cell Id doesn't exist, add to bootstrap list
                    if (!existingCellIds.Contains(cellId))
                    {
                        missingCellIds.Add(cellId);
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
                    foreach (var polygon in MultiPolygon)
                    {
                        try
                        {
                            // Make sure first and last coords are the same
                            if (polygon[0][0] != polygon[0][^1]) // NOTE: polygon[0][polygon[0].Count - 1]
                            {
                                polygon[0].Add(polygon[0][0]);
                            }
                            // Get all existing Pokestops within geofence bounds
                            var bounds = polygon.GetBoundingBox();
                            var stops = await _pokestopRepository.GetWithin(bounds); // TODO: Updated
                            foreach (var stop in stops)
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
                    foreach (var stop in _allStops)
                    {
                        if ((!stop.QuestType.HasValue || stop.QuestType == 0) && stop.Enabled)
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
            var now = DateTime.Now.SecondsUntilMidnight();
            _timer.Interval = now == 0 ? 1000 : now * 1000;
            _timer.Start();

            if (_shouldExit)
                return;

            if (_allStops.Count == 0)
            {
                _logger.LogWarning($"[{Name}] Tried clearing quests but no pokestops.");
                return;
            }
            _logger.LogDebug($"[{Name}] Getting pokestop ids");
            var ids = _allStops.Select(x => x.Id);
            _logger.LogInformation($"[{Name}] Clearing Quests for ids: {string.Join(",", ids)}");
            try
            {
                await _pokestopRepository.ClearQuestsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{Name}] Failed to clear quests: {ex}");
                if (_shouldExit)
                    return;
            }
            await Update();
        }

        private async Task<BootstrapTask> GetBootstrapTask()
        {
            var target = _bootstrapCellIds.PopLast(out _bootstrapCellIds);
            if (target == default)
                return null;

            var cell = new S2Cell(new S2CellId(target));
            var center = cell.Center;
            var point = new S2Point(center.X, center.Y, center.Z);
            var latlng = new S2LatLng(point);
            // Get all cells touching a 630 (-5m for error) circle at center
            var radians = 0.00009799064306948; // 625m
            var circle = S2Cap.FromAxisHeight(center, (radians * radians) / 2);
            var coverer = new S2RegionCoverer
            {
                MinLevel = 15,
                MaxLevel = 15,
                MaxCells = 100
            };
            var cellIds = coverer.GetCovering(circle);
            foreach (var cellId in cellIds)
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
                    await Update();
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
                var list = new List<Cell>();
                var count = Math.Ceiling(ids.Count / MAX_COUNT);
                for (var i = 0; i < count; i++)
                {
                    var start = (int)MAX_COUNT * i;
                    var end = (int)Math.Min(MAX_COUNT * (i + 1), ids.Count - 1);
                    var slice = ids.Slice(start, end);
                    var result = await GetCellsByIDs(slice);
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
            return (List<Cell>)await _cellRepository.GetByIdsAsync(ids, true);
        }

        private async Task<ulong> GetQuestCount(List<string> ids)
        {
            const double MAX_COUNT = 10000.0;
            if (ids.Count > MAX_COUNT)
            {
                var result = 0UL;
                var count = Math.Ceiling(ids.Count / MAX_COUNT);
                for (var i = 0; i < count; i++)
                {
                    var start = (int)MAX_COUNT * i;
                    var end = (int)Math.Min(MAX_COUNT * (i + 1), ids.Count - 1);
                    var slice = ids.Slice(start, end);
                    var qResult = await GetQuestCount(slice);
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
            var pokestops = await _pokestopRepository.GetByIdsAsync(ids);
            var questCount = (ulong)pokestops.Where(x => !x.Deleted &&
                                                         x.QuestType.HasValue &&
                                                         x.QuestType != null).ToList().Count;
            return questCount;
        }

        private static double GetCooldownAmount(double distanceM)
        {
            return Math.Min(Convert.ToUInt32(distanceM / 9.8), 7200);
        }

        #endregion
    }
}