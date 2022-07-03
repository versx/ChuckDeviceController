namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class PokestopWithMode
    {
        public Pokestop Pokestop { get; set; }

        public bool IsAlternative { get; set; }
    }

    public sealed class InstanceCompleteEventArgs : EventArgs
    {
        public string InstanceName { get; }

        public ulong CompletionTimestamp { get; }

        public InstanceCompleteEventArgs(string instanceName, ulong completionTimestamp)
        {
            InstanceName = instanceName;
            CompletionTimestamp = completionTimestamp;
        }
    }

    public class AutoInstanceController : IJobController
    {
        private const uint OneHourS = 3600;
        private const ushort MaxSpinAttempts = 5; // TODO: Make configurable
        private const uint DelayLogout = 900; // TODO: Make configurable

        #region Variables

        private readonly ILogger<AutoInstanceController> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _factory;

        private readonly List<PokestopWithMode> _allStops;
        private readonly List<PokestopWithMode> _todayStops;
        private readonly Dictionary<PokestopWithMode, byte> _todayStopsAttempts;
        private List<ulong> _bootstrapCellIds;
        private readonly System.Timers.Timer _timer;
        private int _bootstrapTotalCount = 0;
        private ulong _completionDate = 0;
        private ulong _lastCompletionCheck = DateTime.UtcNow.ToTotalSeconds() - OneHourS;

        private readonly short _timezoneOffset;
        private bool _shouldExit;
        private readonly Dictionary<string, string> _accounts;
        private readonly Dictionary<string, bool> _lastMode;
        private bool _useRwForQuest = true; // TODO: Make 'UseRwAccountsForQuests' configurable

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygon { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName  { get; }

        public bool IsEvent  { get; }

        public short TimeZoneOffset { get; }

        public AutoInstanceType Type { get; }

        public uint SpinLimit { get; }

        public bool IgnoreS2CellBootstrap { get; }

        public bool RequireAccountEnabled { get; set; } // TODO: Make configurable

        public QuestMode QuestMode { get; }

        #endregion

        #region Events

        public event EventHandler<InstanceCompleteEventArgs> InstanceComplete;
        private void OnInstanceComplete(string instanceName, ulong completionTimestamp)
        {
            InstanceComplete?.Invoke(this, new InstanceCompleteEventArgs(instanceName, completionTimestamp));
        }

        #endregion

        #region Constructor

        public AutoInstanceController(
            IDbContextFactory<DeviceControllerContext> factory,
            Instance instance,
            List<MultiPolygon> multiPolygon,
            short timezoneOffset,
            bool ignoreBootstrap = false) // TODO: Make 'IgnoreBootstrap' configurable via Instance.Data
        {
            Name = instance.Name;
            MultiPolygon = multiPolygon;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup;
            IsEvent = instance.Data?.IsEvent ?? false;
            SpinLimit = instance.Data?.SpinLimit ?? 1000;
            IgnoreS2CellBootstrap = ignoreBootstrap;

            _logger = new Logger<AutoInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _factory = factory;

            _allStops = new List<PokestopWithMode>();
            _todayStops = new List<PokestopWithMode>();
            _todayStopsAttempts = new Dictionary<PokestopWithMode, byte>();
            _bootstrapCellIds = new List<ulong>();

            _timezoneOffset = timezoneOffset;
            _accounts = new Dictionary<string, string>();
            _lastMode = new Dictionary<string, bool>();

            var timeLeft = DateTime.Now.SecondsUntilMidnight(); // TODO: Utc or Local?
            _timer = new System.Timers.Timer(timeLeft * 1000);
            _timer.Elapsed += async (sender, e) => await ClearQuests();
            _timer.Start();

            // TODO: Get 12am DateTime
            _logger.LogInformation($"[{Name}] Clearing Quests in {timeLeft:N0}s at 12:00 AM (Currently: {DateTime.Now})");

            UpdateAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (!IgnoreS2CellBootstrap)
            {
                BootstrapAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false)
        {
            switch (Type)
            {
                case AutoInstanceType.Quest:
                    if (_bootstrapCellIds.Count > 0)
                    {
                        var bootstrapTask = await GetBootstrapTaskAsync();
                        return bootstrapTask;
                    }

                    if (string.IsNullOrEmpty(accountUsername) && RequireAccountEnabled)
                    {
                        _logger.LogWarning($"[{Name}] No username specified for device '{uuid}', ignoring...");
                        return null;
                    }

                    if (account == null && RequireAccountEnabled)
                    {
                        _logger.LogWarning($"[{Name}] No account specified for device '{uuid}', ignoring...");
                        return null;
                    }

                    // TODO: Lock _todayStops
                    if (_allStops.Count == 0)
                    {
                        return null;
                    }

                    await CheckIfCompletedAsync(uuid);

                    PokestopWithMode? pokestop = null;
                    Coordinate? lastCoord = null;
                    try
                    {
                        lastCoord = Cooldown.GetLastLocation(account, uuid);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[{Name}] Failed to get last location for device '{uuid}'");
                        return null;
                    }

                    if (lastCoord == null)
                    {
                        PokestopWithMode? closestOverall = null;
                        double closestOverallDistance = 10000000000000000;

                        PokestopWithMode? closestNormal = null;
                        double closestNormalDistance = 10000000000000000;

                        PokestopWithMode? closestAlternative = null;
                        double closestAlternativeDistance = 10000000000000000;

                        // TODO: Lock _todayStops
                        var todayStopsC = _todayStops;
                        if (todayStopsC.Count == 0)
                        {
                            return null;
                        }

                        foreach (var stop in todayStopsC)
                        {
                            var coord = new Coordinate(stop.Pokestop.Latitude, stop.Pokestop.Longitude);
                            var dist = lastCoord.DistanceTo(coord);
                            if (dist < closestOverallDistance)
                            {
                                closestOverall = stop;
                                closestOverallDistance = dist;
                            }
                            if (!stop.IsAlternative && dist < closestNormalDistance)
                            {
                                closestNormal = stop;
                                closestNormalDistance = dist;
                            }
                            if (stop.IsAlternative && dist < closestAlternativeDistance)
                            {
                                closestAlternative = stop;
                                closestAlternativeDistance = dist;
                            }
                        }

                        PokestopWithMode? closest = null;
                        // TODO: Lock _lastMode
                        var key = accountUsername ?? uuid;
                        bool? mode = _lastMode.ContainsKey(key)
                            ? _lastMode[key]
                            : null;
                        if (mode == null)
                        {
                            closest = closestOverall;
                        }
                        else if (_lastMode.ContainsKey(key) && !_lastMode[key])
                        {
                            closest = closestNormal ?? closestOverall;
                        }
                        else
                        {
                            closest = closestAlternative ?? closestOverall;
                        }

                        if (closest == null)
                        {
                            return null;
                        }

                        if ((mode ?? false) && !(closest?.IsAlternative ?? false))
                        {
                            _logger.LogDebug($"[{Name}] [{accountUsername ?? "?"}] Switching quest mode from {((mode ?? false) ? "alternative" : "none")} to normal.");
                            PokestopWithMode? closestAr = null;
                            double closestArDistance = 10000000000000000;
                            var arStops = _allStops.Where(x => x.Pokestop.IsArScanEligible).ToList();
                            foreach (var stop in arStops)
                            {
                                var coord = new Coordinate(stop.Pokestop.Latitude, stop.Pokestop.Longitude);
                                var dist = lastCoord.DistanceTo(coord);
                                if (dist < closestArDistance)
                                {
                                    closestAr = stop;
                                    closestArDistance = dist;
                                }
                            }

                            if (closestAr != null && closestAr?.Pokestop != null)
                            {
                                closestAr.IsAlternative = closest?.IsAlternative ?? false;
                                closest = closestAr;
                                _logger.LogDebug($"[{Name}] [{accountUsername ?? "?"}] Scanning AR eligible Pokestop {closest?.Pokestop?.Id}");
                            }
                            else
                            {
                                _logger.LogDebug($"[{Name}] [{accountUsername ?? "?"}] No AR eligible Pokestop found to scan");
                            }
                        }

                        pokestop = closest;

                        var nearbyStops = new List<PokestopWithMode> { pokestop };
                        var pokestopCoord = new Coordinate(pokestop.Pokestop.Latitude, pokestop.Pokestop.Longitude);
                        foreach (var stop in todayStopsC)
                        {
                            // TODO: Revert back to 40m once reverted ingame
                            if (pokestop.IsAlternative == stop.IsAlternative &&
                                pokestopCoord.DistanceTo(new Coordinate(stop.Pokestop.Latitude, stop.Pokestop.Longitude)) <= 80)
                            {
                                nearbyStops.Add(stop);
                            }
                        }

                        // TODO: Lock nearbyStops
                        foreach (var stop in nearbyStops)
                        {
                            var index = _todayStops.IndexOf(stop);
                            if (index > -1)
                            {
                                _todayStops.RemoveAt(index);
                            }
                        }
                    }
                    else
                    {
                        // TODO: Lock _todayStops
                        PokestopWithMode? stop = _todayStops.FirstOrDefault();
                        if (stop == null)
                        {
                            return null;
                        }

                        pokestop = stop;
                        _todayStops.RemoveAt(0);
                    }

                    double delay;
                    uint encounterTime;
                    try
                    {
                        var result = Cooldown.SetCooldown(
                            account,
                            uuid,
                            new Coordinate(pokestop.Pokestop.Latitude, pokestop.Pokestop.Longitude)
                        );
                        delay = result.Delay;
                        encounterTime = result.EncounterTime;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[{Name}] [{uuid}] Failed to calculate cooldown time for device");
                        // TODO: Lock _todayStops
                        _todayStops.Add(pokestop);
                        return null;
                    }

                    string newUsername;
                    if (delay >= DelayLogout && account != null)
                    {
                        // TODO: Lock _todayStops
                        _todayStops.Add(pokestop);
                        // TODO: Lock _accounts
                        try
                        {
                            var newAccount = await GetAccountAsync(
                                uuid,
                                new Coordinate(pokestop.Pokestop.Latitude, pokestop.Pokestop.Longitude)
                            );
                            if (!_accounts.ContainsKey(uuid))
                            {
                                newUsername = newAccount?.Username;
                                _accounts.Add(uuid, newAccount?.Username);
                                _logger.LogDebug($"[{Name}] [{uuid}] Over logout delay. Switching account from {accountUsername ?? "?"} to {newUsername ?? "?"}");
                            }
                            else
                            {
                                newUsername = _accounts[uuid];
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] [{uuid}] Failed to get account for device in advance");
                        }

                        return new SwitchAccountTask
                        {
                            Area = Name,
                            Action = DeviceActionType.SwitchAccount,
                            MinimumLevel = MinimumLevel,
                            MaximumLevel = MaximumLevel,
                        };
                    }
                    else if (delay >= DelayLogout)
                    {
                        _logger.LogWarning($"[{Name}] [{uuid}] Ignoring over logout delay, because no account is specified");
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(accountUsername))
                        {
                            // TODO: Set spin count await Account.Spin(accountUsername);
                        }
                        Cooldown.Encounter(
                            account,
                            uuid,
                            new Coordinate(pokestop.Pokestop.Latitude, pokestop.Pokestop.Longitude),
                            encounterTime
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[{Name}] [{uuid}] Failed to store cooldown");
                        // TODO: Lock _todayStops
                        _todayStops.Add(pokestop);
                        return null;
                    }

                    // TODO: Lock _todayStopsAttempts
                    if (_todayStopsAttempts.ContainsKey(pokestop))
                    {
                        var tries = _todayStopsAttempts[pokestop];
                        _todayStopsAttempts[pokestop] = Convert.ToByte(tries == byte.MaxValue ? 10 : tries + 1);
                    }
                    else
                    {
                        _todayStopsAttempts.Add(pokestop, 1);
                    }

                    await CheckIfCompletedAsync(uuid);

                    // TODO: Lock _lastMode
                    var modeKey = accountUsername ?? uuid;
                    if (!_lastMode.ContainsKey(modeKey))
                    {
                        _lastMode.Add(modeKey, pokestop.IsAlternative);
                    }
                    else
                    {
                        _lastMode[modeKey] = pokestop.IsAlternative;
                    }
                    // TODO: setArQuestTarget(uuid, timestamp, pokestop.IsAlternative);
                    return new QuestTask
                    {
                        Area = Name,
                        Action = DeviceActionType.ScanQuest,
                        DeployEgg = false,
                        Latitude = pokestop.Pokestop.Latitude,
                        Longitude = pokestop.Pokestop.Longitude,
                        Delay = delay,
                        MinimumLevel = MinimumLevel,
                        MaximumLevel = MaximumLevel,
                        QuestType = pokestop.IsAlternative
                            ? "ar"
                            : "normal",
                    };
                    break;
            }
            return null;
        }

        public async Task<string> GetStatusAsync()
        {
            switch (Type)
            {
                case AutoInstanceType.Quest:
                    // TODO: Lock _boostrapCellIds
                    if (_bootstrapCellIds.Count > 0)
                    {
                        var totalCount = _bootstrapTotalCount;
                        var count = totalCount - _bootstrapCellIds.Count;
                        var percentage = totalCount > 0
                            ? Convert.ToDouble((double)count / (double)totalCount) * 100
                            : 100;

                        var bootstrapStatus = $"Bootstrapping {count:N0}/{totalCount:N0} ({Math.Round(percentage, 1)}%)";
                        return bootstrapStatus;
                    }

                    // TODO: Lock _allStops
                    var ids = _allStops.Select(x => x.Pokestop.Id).ToList();
                    var currentCountDb = 0;// TODO: await Pokestop.GetQuestCountIn(ids, QuestMode);
                    // TODO: Lock _allStops
                    var maxCount = _allStops.Count;
                    var currentCount = maxCount - _allStops.Count;

                    var percent = maxCount > 0
                        ? Convert.ToDouble((double)currentCount / (double)maxCount) * 100
                        : 100;
                    var percentReal = maxCount > 0
                        ? Convert.ToDouble((double)currentCountDb / (double)maxCount) * 100
                        : 100;

                    var completedDate = _completionDate.FromSeconds();
                    var status = $"Status: {currentCountDb:N0}|{currentCount:N0}/{maxCount:N0} " +
                        $"({Math.Round(percentReal, 1)})|" +
                        $"{Math.Round(percent, 1)}%)" +
                        $"{(_completionDate != default ? $", Completed @ {completedDate}" : ")")}";
                    return status;
            }
            return null;
        }

        public void Reload()
        {
            UpdateAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Stop()
        {
            _shouldExit = true;
            _timer.Stop();
        }

        #endregion

        #region Private Methods

        private async Task BootstrapAsync()
        {
            _logger.LogInformation($"[{Name}] Checking Bootstrap Status...");

            var start = DateTime.UtcNow;
            var totalCount = 0;
            var missingCellIds = new List<ulong>();

            // Loop through all geofences and get s2cells within each geofence
            foreach (var polygon in MultiPolygon)
            {
                // Get maximum amount of S2 level 15 cells within this geofence
                var s2Cells = polygon.GetS2CellIds(15, int.MaxValue);
                var s2CellIds = s2Cells.Select(cell => cell.Id).ToList();
                totalCount += s2CellIds.Count;

                // TODO: Get all known cells from the database
                var existingCells = new List<Cell>();
                var existingCellIds = existingCells.Select(cell => cell.Id).ToList();

                // Loop through all S2Cells within the geofence and filter any missing
                foreach (var s2CellId in s2CellIds)
                {
                    // Check if we don't already have the S2Cell in the database
                    if (!existingCellIds.Contains(s2CellId))
                    {
                        missingCellIds.Add(s2CellId);
                    }
                }
            }
            missingCellIds = missingCellIds.Distinct().ToList();
            missingCellIds.Sort();

            _logger.LogInformation($"[{Name}] Bootstrap Status: {totalCount - missingCellIds.Count}/{totalCount} after {DateTime.UtcNow.Subtract(start).TotalSeconds:N0} seconds");
            // TODO: Lock _bootstrapCellIds
            _bootstrapCellIds = missingCellIds;
            _bootstrapTotalCount = totalCount;
        }

        private async Task UpdateAsync()
        {
            switch (Type)
            {
                case AutoInstanceType.Quest:
                    _allStops.Clear();
                    foreach (var polygon in MultiPolygon)
                    {
                        try
                        {
                            // TODO: Use Polygon instead of bbox
                            var bbox = polygon.GetBoundingBox();
                            var stops = await GetPokestopsInBoundsAsync(bbox);
                            foreach (var stop in stops)
                            {
                                if (GeofenceService.InPolygon(polygon, stop.Latitude, stop.Longitude))
                                {
                                    if (QuestMode == QuestMode.Normal || QuestMode == QuestMode.Both)
                                    {
                                        _allStops.Add(new PokestopWithMode
                                        {
                                            Pokestop = stop,
                                            IsAlternative = false,
                                        });
                                    }
                                    if (QuestMode == QuestMode.Alternative || QuestMode == QuestMode.Both)
                                    {
                                        _allStops.Add(new PokestopWithMode
                                        {
                                            Pokestop = stop,
                                            IsAlternative = true,
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[{Name}] Error: {ex}");
                        }
                    }

                    _todayStops.Clear();
                    _todayStopsAttempts.Clear();
                    _completionDate = 0;
                    _allStops.Sort((a, b) =>
                    {
                        var coordA = new Coordinate(a.Pokestop.Latitude, a.Pokestop.Longitude);
                        var coordB = new Coordinate(b.Pokestop.Latitude, b.Pokestop.Longitude);
                        var distanceA = coordA.DistanceTo(coordB);
                        var distanceB = coordA.DistanceTo(coordA);
                        return Convert.ToInt32(((distanceA + distanceB) * 100) / 2);
                    });
                    foreach (var stop in _allStops)
                    {
                        // Check that the Pokestop does not have quests already found and that it is enabled
                        if (stop.Pokestop.QuestType == null && stop.Pokestop.Enabled)
                        {
                            // Add Pokestop if it's not already in the dictionary
                            if (!_todayStops.Contains(stop))
                            {
                                _todayStops.Add(stop);
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

            _logger.LogInformation($"[{Name}] Clearing Pokestop Quests...");
            // TODO: Clear quests await Pokestop.ClearQuestsAsync();
            await UpdateAsync();
        }

        private async Task<BootstrapTask> GetBootstrapTaskAsync()
        {
            var target = _bootstrapCellIds.LastOrDefault();
            if (target == default)
            {
                return null;
            }

            _bootstrapCellIds.RemoveAt(_bootstrapCellIds.Count - 1);

            var center = target.S2LatLngFromId();
            var coord = new Coordinate(center.LatDegrees, center.LngDegrees);
            var cellIds = center.GetLoadedS2CellIds();

            // TODO: Lock _bootstrapCellIds
            foreach (var cellId in cellIds)
            {
                var index = _bootstrapCellIds.IndexOf(cellId.Id);
                if (index > 0)
                {
                    _bootstrapCellIds.RemoveAt(index);
                }
            }

            if (_bootstrapCellIds.Count == 0)
            {
                // TODO: Lock _bootstrapCellIds
                await BootstrapAsync();
                if (_bootstrapCellIds.Count == 0)
                {
                    await UpdateAsync();
                }
            }

            return new BootstrapTask
            {
                Area = Name,
                Action = DeviceActionType.ScanRaid,
                Latitude = coord.Latitude,
                Longitude = coord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
        }

        private async Task<Account> GetAccountAsync(string uuid, Coordinate encounterTarget)
        {
            /*
            TODO: Get account
            if (_accounts.ContainsKey(uuid))
            {
                var username = _accounts[uuid];
                _accounts.Remove(uuid);
                return await Account.GetById(username);
            }
            return Account.GetNewAccountAsync(
                MinimumLevel,
                MaximumLevel,
                _useRwForQuest,
                SpinLimit,
                noCooldown: true,
                encounterTarget,
                uuid,
                GroupName
            );
            */
            return null;
        }

        private async Task<List<Pokestop>> GetPokestopsInCellsAsync(List<string> pokestopIds)
        {
            if (pokestopIds.Count > 10000)
            {
                var list = new List<Pokestop>();
                var count = Convert.ToInt64(Math.Ceiling(Convert.ToDouble(pokestopIds.Count) / 10000.0));
                for (var i = 0; i < count; i++)
                {
                    var start = 10000 * i;
                    var end = Math.Min(10000 * (i + 1) - 1, pokestopIds.Count - 1);
                    var splice = pokestopIds.GetRange(start, end);
                    var spliceResult = await GetPokestopsInCellsAsync(splice);
                    if (spliceResult != null)
                    {
                        list.AddRange(spliceResult);
                    }
                }
                return list;
            }

            if (pokestopIds.Count == 0)
            {
                return new List<Pokestop>();
            }

            /*
            using (var context = _factory.CreateDbContext())
            {
                var pokestops = context.Pokestops
            }
            */
            return new List<Pokestop>();
        }

        private async Task<List<Pokestop>> GetPokestopsInBoundsAsync(BoundingBox bbox)
        {
            /*
            using (var context = _factory.CreateDbContext())
            {
                var pokestops = context.Pokestops.Where((Pokestop stop) =>
                {
                    return stop.Latitude >= bbox.MinimumLatitude &&
                        stop.Latitude <= bbox.MaximumLatitude &&
                        stop.Longitude >= bbox.MinimumLongitude &&
                        stop.Longitude <= bbox.MaximumLongitude &&
                        // TODO: stop.Updated >= updated &&
                        !stop.Deleted;
                }).ToList();
                return pokestops;
            }
            */
            return new List<Pokestop>();
        }

        private async Task CheckIfCompletedAsync(string uuid)
        {
            if (_todayStops.Count > 0)
            {
                return;
            }
            var now = DateTime.UtcNow.ToTotalSeconds();
            _lastCompletionCheck = now;
            var ids = _allStops.Select(x => x.Pokestop.Id).ToList();

            var newStops = new List<Pokestop>();
            try
            {
                // Get Pokestops within S2 cells
                newStops = await GetPokestopsInCellsAsync(ids);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{Name}] [{uuid}] Failed to get today stops");
                return;
            }

            // TODO: Lock newStops
            foreach (var stop in newStops)
            {
                if (QuestMode == QuestMode.Normal || QuestMode == QuestMode.Both)
                {
                    var pokestopWithMode = new PokestopWithMode
                    {
                        Pokestop = stop,
                        IsAlternative = false,
                    };
                    var count = _todayStopsAttempts.ContainsKey(pokestopWithMode)
                        ? _todayStopsAttempts[pokestopWithMode]
                        : 0;
                    if (stop.QuestType == null && stop.Enabled && count <= MaxSpinAttempts)
                    {
                        _todayStops.Add(pokestopWithMode);
                    }
                }

                if (QuestMode == QuestMode.Alternative || QuestMode == QuestMode.Both)
                {
                    var pokestopWithMode = new PokestopWithMode
                    {
                        Pokestop = stop,
                        IsAlternative = true,
                    };
                    var count = _todayStopsAttempts.ContainsKey(pokestopWithMode)
                        ? _todayStopsAttempts[pokestopWithMode]
                        : 0;
                    if (stop.AlternativeQuestType == null && stop.Enabled && count <= MaxSpinAttempts)
                    {
                        _todayStops.Add(pokestopWithMode);
                    }
                }
            }

            if (_todayStops.Count == 0)
            {
                _logger.LogInformation($"[{Name}] [{uuid}] Quest instance complete");
                if (_completionDate == 0)
                {
                    _completionDate = DateTime.UtcNow.ToTotalSeconds();
                }

                // Call OnInstanceComplete event
                OnInstanceComplete(Name, now);
            }
        }

        #endregion
    }

    public class CooldownResult
    {
        public double Delay { get; set; }

        public uint EncounterTime { get; set; }
    }

    public class Cooldown
    {
        public static double GetCooldownAmount(double distanceM)
        {
            return Math.Min(Convert.ToInt32(distanceM / 9.8), 7200);
        }

        public static Coordinate GetLastLocation(Account account, string uuid)
        {
            double? lat = null;
            double? lon = null;
            if (account != null)
            {
                lat = account.LastEncounterLatitude;
                lon = account.LastEncounterLongitude;
            }
            if (lat == null || lon == null)
            {
                return null;
            }
            return new Coordinate(lat.Value, lon.Value);
        }

        public static CooldownResult SetCooldown(Account account, string uuid, Coordinate location)
        {
            // TODO: SetCooldown
            return new CooldownResult();
        }

        public static void Encounter(Account account, string uuid, Coordinate location, uint encounterTime)
        {
            if (account != null)
            {
                // TODO: Cooldown.Encounter
                /*
                Account.DidEncounter(
                    account.Username,
                    location.Latitude,
                    location.Longitude,
                    encounterTime
                );
                */
            }
        }
    }
}