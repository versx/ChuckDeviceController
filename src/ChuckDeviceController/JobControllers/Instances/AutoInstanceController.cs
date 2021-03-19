namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Google.Common.Geometry;
    using Microsoft.Extensions.Logging;

    using Chuck.Common.JobControllers;
    using Chuck.Common.JobControllers.Tasks;
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Factories;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using Chuck.Geometry.Geofence;
    using Chuck.Geometry.Geofence.Models;
    using ChuckDeviceController.Extensions;

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

        private readonly object _bootstrapLock = new();

        // Entity repositories
        private readonly AccountRepository _accountRepository;
        private readonly PokestopRepository _pokestopRepository;
        private readonly CellRepository _cellRepository;

        // TODO: Use lock for stops list

        #endregion

        #region Properties

        public string Name { get; }

        public List<MultiPolygon> MultiPolygon { get; }

        public AutoType Type { get; }

        public int TimezoneOffset { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public int SpinLimit { get; }

        public byte RetryLimit { get; } = 5;

        public string GroupName { get; }

        public bool IsEvent { get; }

        #endregion

        #region Constructor

        public AutoInstanceController(string name, List<MultiPolygon> multiPolygon, AutoType type, int timezoneOffset, ushort minLevel, ushort maxLevel, int spinLimit, byte retryLimit, string groupName = null, bool isEvent = false)
        {
            Name = name;
            MultiPolygon = multiPolygon;
            Type = type;
            TimezoneOffset = timezoneOffset;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            SpinLimit = spinLimit;
            RetryLimit = retryLimit;
            GroupName = groupName;
            IsEvent = isEvent;

            _logger = new Logger<AutoInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _allStops = new List<Pokestop>();
            _todayStops = new Dictionary<string, Pokestop>();
            _todayStopsTries = new Dictionary<string, int>();
            _bootstrapCellIds = new List<ulong>();

            _accountRepository = new AccountRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _pokestopRepository = new PokestopRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _cellRepository = new CellRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));

            var timeLeft = DateTime.Now.SecondsUntilMidnight();
            _timer = new System.Timers.Timer(timeLeft * 1000);
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

                    // Check if any stops in area
                    if (_allStops.Count == 0)
                        return null;

                    // Check if any pokestops without quests
                    if (_todayStops.Count == 0)
                    {
                        // Check last completed date
                        var now = DateTime.UtcNow.ToTotalSeconds();
                        if (now - _lastCompletionCheck >= 600)
                        {
                            await OnComplete().ConfigureAwait(false);
                            return null;
                        }
                        _lastCompletionCheck = now;

                        var ids = _allStops.ConvertAll(x => x.Id);
                        var newStops = new List<Pokestop>();
                        try
                        {
                            newStops = await _pokestopRepository.GetByIdsAsync(ids).ConfigureAwait(false);
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
                            var tryCount = _todayStopsTries[stop.Id];
                            if (stop.QuestType == null && stop.Enabled && tryCount <= RetryLimit)
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                        // Check if all stops have quests, if so call on complete
                        if (_todayStops.Count == 0)
                        {
                            await OnComplete().ConfigureAwait(false);
                            return null;
                        }
                    }

                    Coordinate lastCoord = null;
                    ulong lastTime = 0;
                    Account account = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(accountUsername))
                        {
                            account = await _accountRepository.GetByIdAsync(accountUsername).ConfigureAwait(false);
                            var lastLat = account.LastEncounterLatitude;
                            var lastLon = account.LastEncounterLongitude;
                            if (lastLat.HasValue && lastLon.HasValue)
                            {
                                lastCoord = new Coordinate(lastLat ?? 0, lastLon ?? 0);
                            }
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

                    ulong encounterTime;
                    Pokestop nextPokestop = null;
                    if (lastCoord != null)
                    {
                        // TODO: Lock stops
                        var todayStopsC = _todayStops;
                        if (todayStopsC.Count == 0)
                            return null;

                        Pokestop closest = null;
                        var closestDistance = 10000000000000000d;
                        foreach (var (stopId, stop) in todayStopsC)
                        {
                            var coord = new Coordinate(stop.Longitude, stop.Latitude);
                            var dist = coord.DistanceTo(lastCoord);
                            if (dist < closestDistance)
                            {
                                closest = stop;
                                closestDistance = dist;
                            }
                        }
                        if (closest == null)
                            return null;

                        nextPokestop = closest;

                        var nearbyPokestops = new List<Pokestop>();
                        var pokestopCoord = new Coordinate(nextPokestop.Latitude, nextPokestop.Longitude);
                        foreach (var (id, stop) in todayStopsC)
                        {
                            // Revert back to 40m once reverted ingame
                            if (pokestopCoord.DistanceTo(new Coordinate(stop.Latitude, stop.Longitude)) <= 80)
                            {
                                nearbyPokestops.Add(stop);
                            }
                        }
                        foreach (var stop in nearbyPokestops)
                        {
                            if (_todayStops.ContainsKey(stop.Id))
                            {
                                _todayStops.Remove(stop.Id);
                            }
                        }

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
                        if (nextPokestop != null)
                        {
                            _todayStops.Remove(nextPokestop.Id);
                        }
                    }
                    else
                    {
                        var stop = _todayStops.FirstOrDefault().Value;
                        if (stop == null)
                            return null;

                        nextPokestop = stop;
                        encounterTime = DateTime.UtcNow.ToTotalSeconds();
                        _todayStops.Remove(_todayStops.Keys.FirstOrDefault());
                    }
                    await _accountRepository.SpinAsync(accountUsername).ConfigureAwait(false);

                    if (_todayStopsTries.ContainsKey(nextPokestop.Id))
                    {
                        _todayStopsTries[nextPokestop.Id]++;
                    }

                    if (!string.IsNullOrEmpty(accountUsername) && account != null)
                    {
                        await _accountRepository.SetLastEncounterAsync(accountUsername, nextPokestop.Latitude, nextPokestop.Longitude, encounterTime).ConfigureAwait(false);
                    }
                    else
                    {
                        // TODO: Account cooldowns
                    }

                    var delayT = DateTime.UtcNow.ToTotalSeconds() - encounterTime;
                    var delay = Convert.ToDouble(delayT == 0 ? 0 : delayT + 1);
                    _logger.LogDebug($"[{Name}] Delaying by {delay}");
                    if (_todayStops.Count == 0)
                    {
                        _lastCompletionCheck = DateTime.UtcNow.ToTotalSeconds();
                        var ids = _allStops.ConvertAll(x => x.Id);
                        var newStops = new List<Pokestop>();
                        try
                        {
                            newStops = await _pokestopRepository.GetByIdsAsync(ids).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Failed to get list of Pokestops by ids {string.Join(",", ids)}: {ex}");
                        }
                        // Check if there's still any pokestops left that haven't had quest scanned
                        foreach (var stop in newStops)
                        {
                            if (stop.QuestType == null && stop.Enabled)
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                        // If there's no pokestops left that need quests, instance complete
                        if (_todayStops.Count == 0)
                        {
                            await OnComplete().ConfigureAwait(false);
                        }
                    }
                    return new QuestTask
                    {
                        Area = Name,
                        Action = ActionType.ScanQuest,
                        Latitude = nextPokestop.Latitude,
                        Longitude = nextPokestop.Longitude,
                        Delay = delay,
                        MinimumLevel = MinimumLevel,
                        MaximumLevel = MaximumLevel,
                    };
            }
            return null;
        }

        private async Task OnComplete()
        {
            if (_completionDate == default)
            {
                _completionDate = DateTime.UtcNow;
            }
            _logger.LogInformation($"[{Name}] Instance done");
            await AssignmentController.Instance.InstanceControllerDone(Name).ConfigureAwait(false);
        }

        public async Task<string> GetStatus()
        {
            switch (Type)
            {
                case AutoType.Quest:
                    bool needsBootstrap;
                    int totalBootstrapCount;
                    int count;
                    lock (_bootstrapLock)
                    {
                        needsBootstrap = _bootstrapCellIds.Count > 0;
                        totalBootstrapCount = _bootstrapTotalCount;
                        count = _bootstrapCellIds.Count;
                    }
                    if (needsBootstrap)
                    {
                        var bootstrapCount = totalBootstrapCount - count;
                        var bootstrapPercentage = totalBootstrapCount > 0
                            ? Convert.ToDouble((double)bootstrapCount / totalBootstrapCount) * 100.0
                            : 100d;
                        return $"Bootstrapping {bootstrapCount:N0}/{totalBootstrapCount:N0} ({Math.Round(bootstrapPercentage, 2)}%)";
                    }
                    var ids = _allStops.ConvertAll(x => x.Id);
                    var currentCountDb = await _pokestopRepository.GetQuestCount(ids).ConfigureAwait(false);
                    var currentCount = _allStops.Count - _todayStops.Count;
                    var percentage = _allStops.Count > 0
                        ? Convert.ToDouble((double)currentCount / _allStops.Count) * 100.0
                        : 100d;
                    var percentageReal = _allStops.Count > 0
                        ? Convert.ToDouble((double)currentCountDb / _allStops.Count) * 100.0
                        : 100d;
                    return $"Status: {currentCountDb:N0}|{currentCount:N0}/{_allStops.Count:N0} ({Math.Round(percentageReal, 2)}|{Math.Round(percentage, 2)}%{(_completionDate != default ? $", Completed: @ {_completionDate}" : "")})";
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
            var start = DateTime.UtcNow;
            var totalCount = 0;
            var missingCellIds = new List<ulong>();
            // Loop all geofences and get s2cells within each
            foreach (var polygon in MultiPolygon)
            {
                // Get max amount of s2 level 15 cells within this geofence
                var s2Cells = polygon.GetS2CellIds(15, int.MaxValue);
                var s2CellIds = s2Cells.ConvertAll(x => x.Id);
                totalCount += s2CellIds.Count;
                // Get all known cells from the database
                var cells = await _cellRepository.GetByIdsAsync(s2CellIds, false).ConfigureAwait(false);
                // Map to just s2cell ids
                var existingCellIds = cells.Select(x => x.Id).ToList();
                // Loop all s2cells within geofence and check if any are missing
                foreach (var s2cellId in s2CellIds)
                {
                    // Check if we don't have the s2cell in the database
                    if (!existingCellIds.Contains(s2cellId))
                    {
                        // Add to bootstrap s2cell list
                        missingCellIds.Add(s2cellId);
                    }
                }
            }
            missingCellIds = missingCellIds.Distinct().ToList();
            missingCellIds.Sort();
            _logger.LogInformation($"[{Name}] Bootstrap Status: {totalCount - missingCellIds.Count}/{totalCount} after {DateTime.UtcNow.Subtract(start).TotalSeconds:N0} seconds");
            lock (_bootstrapLock)
            {
                _bootstrapCellIds = missingCellIds;
                _bootstrapTotalCount = totalCount;
            }
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
                            // Get all existing Pokestops within geofence bounds
                            var bbox = polygon.GetBoundingBox();
                            var stops = await _pokestopRepository.GetAllAsync(
                                bbox.MinimumLatitude,
                                bbox.MinimumLongitude,
                                bbox.MaximumLatitude,
                                bbox.MaximumLongitude
                            ).ConfigureAwait(false);
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
                    _allStops.Sort((a, b) =>
                    {
                        var coordA = new Coordinate(a.Latitude, a.Longitude);
                        var coordB = new Coordinate(b.Latitude, b.Longitude);
                        var distanceA = coordA.DistanceTo(coordB);
                        var distanceB = coordB.DistanceTo(coordA);
                        return Convert.ToInt32(((distanceA + distanceB) * 100) / 2);
                        //return Convert.ToInt32(distance * 100.0);
                    });
                    foreach (var stop in _allStops)
                    {
                        // Check that stop does not have quest and is enabled
                        if (stop.QuestType == null && stop.Enabled)
                        {
                            // Add stop if it's not already in the dictionary
                            if (!_todayStops.ContainsKey(stop.Id))
                            {
                                _todayStops.Add(stop.Id, stop);
                            }
                        }
                    }
                    // TODO: Sort stops
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
            //_logger.LogDebug($"[{Name}] Getting pokestop ids");
            //var ids = _allStops.Select(x => x.Id);
            _logger.LogInformation($"[{Name}] Clearing Quests");// for ids: {string.Join(",", ids)}");
            try
            {
                await _pokestopRepository.ClearQuestsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{Name}] Failed to clear quests: {ex}");
                if (_shouldExit)
                    return;
            }
            await Update().ConfigureAwait(false);
        }

        private async Task<BootstrapTask> GetBootstrapTask()
        {
            ulong target;
            lock (_bootstrapLock)
            {
                target = _bootstrapCellIds.FirstOrDefault();
                if (target == default)
                    return null;
                _bootstrapCellIds.Remove(target);
            }

            var cell = new S2Cell(new S2CellId(target));
            var latlng = new S2LatLng(cell.Center);

            double radius;
            if (latlng.LatDegrees <= 39)
                radius = 715;
            else if (latlng.LatDegrees >= 69)
                radius = 330;
            else
                radius = (-13 * latlng.LatDegrees) + 1225;

            var radians = radius / 6378137;
            var centerNormalizedPoint = latlng.Normalized.ToPoint();
            var circle = S2Cap.FromAxisHeight(centerNormalizedPoint, (radians * radians) / 2);
            var coverer = new S2RegionCoverer
            {
                MinLevel = 15,
                MaxLevel = 15,
                MaxCells = 100
            };
            var nearbyCellIds = coverer.GetCovering(circle).Select(x => x.Id);
            lock (_bootstrapCellIds)
            {
                _bootstrapCellIds.RemoveAll(cell => nearbyCellIds.Contains(cell));
            }
            if (_bootstrapCellIds.Count == 0)
            {
                await Bootstrap().ConfigureAwait(false);
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

        private static double GetCooldownAmount(double distanceM)
        {
            return Math.Min(Convert.ToUInt32(distanceM / 9.8), 7200);
        }

        #endregion
    }
}