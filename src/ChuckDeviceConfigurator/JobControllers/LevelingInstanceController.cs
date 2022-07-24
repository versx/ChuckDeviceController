namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using POGOProtos.Rpc;

    using ChuckDeviceConfigurator.Collections;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class PlayerLevelingData
    {
        public Dictionary<string, PokemonFortProto> UnspunPokestops { get; } = new();

        public object UnspunPokestopsLock { get; } = new();

        //public List<Coordinate> LastPokestopsSpun { get; } = new();
        public Dictionary<string, Coordinate> LastPokestopsSpun { get; } = new();

        public ulong LastSeen { get; set; }

        public ulong XP { get; set; }

        public ushort Level { get; set; }

        //public List<(ulong, ulong)> XpPerTime { get; } = new(); // timestamp, xp
        public PokemonPriorityQueue<(ulong, ulong)> XpPerTime { get; } = new(); // timestamp, xp

        public Coordinate? LastLocation { get; set; }
    }

    public class LevelingInstanceController : IJobController
    {
        private const ushort DefaultMinTargetLevel = 0;
        private const ushort DefaultMaxTargetLevel = 40;
        private const ushort DefaultLastPokestopsSpunCacheLimit = 10;

        #region Variables

        private static readonly IReadOnlyList<uint> LevelXp = new List<uint>()
        {
            0,
            0,
            1000,
            3000,
            6000,
            10000,
            15000,
            21000,
            28000,
            36000,
            45000,
            55000,
            65000,
            75000,
            85000,
            100000,
            120000,
            140000,
            160000,
            185000,
            210000,
            260000,
            335000,
            435000,
            560000,
            710000,
            900000,
            1100000,
            1350000,
            1650000,
            2000000,
            2500000,
            3000000,
            3750000,
            4750000,
            6000000,
            7500000,
            9500000,
            12000000,
            15000000,
            20000000,
        };

        private readonly ILogger<LevelingInstanceController> _logger;
        private readonly Dictionary<string, PlayerLevelingData> _players = new();
        private readonly object _playersLock = new();

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygons { get; internal set; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public Coordinate StartingCoordinate { get; }

        // NOTE: 'StoreLevelData' is used by proto processor to ignore encountered/received leveling data
        // TODO: Use gRPC to get 'StoreLevelData' property value from leveling job controller instance.
        public bool StoreLevelData { get; }

        public ulong Radius { get; set; }

        #endregion

        #region Constructor

        public LevelingInstanceController(
            Instance instance,
            List<MultiPolygon> multiPolygons,
            Coordinate startingCoord)
        {
            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;
            StartingCoordinate = startingCoord;
            StoreLevelData = instance.Data?.StoreLevelingData ?? Strings.DefaultStoreLevelingData;
            Radius = instance.Data?.LevelingRadius ?? Strings.DefaultLevelingRadius;

            _logger = new Logger<LevelingInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            if (string.IsNullOrEmpty(options.AccountUsername))
            {
                _logger.LogError($"[{Name}] [{options.Uuid}] No account assigned to device, unable to fetch leveling task...");
                return null;
            }

            if (options.Account == null)
            {
                _logger.LogError($"[{Name}] [{options.Uuid}] No account assigned to device, unable to fetch leveling task...");
                return null;
            }

            // Ensure player account level has not met maximum level, otherwise request account switch
            if (_players[options.AccountUsername].Level > MaximumLevel)
            {
                return CreateSwitchAccountTask();
            }

            var currentCoord = GetNextScanLocation(options.AccountUsername, options.Account);
            if (currentCoord == null)
            {
                _logger.LogError($"[{Name}] [{options.Uuid}] Failed to get next scan location for leveling instance");
                return null;
            }

            lock (_playersLock)
            {
                _players[options.AccountUsername].LastSeen = DateTime.UtcNow.ToTotalSeconds();
            }

            var delay = await GetDelayAsync(currentCoord, options.Uuid, options.Account);
            var task = CreateTask(currentCoord, delay, deployEgg: true);
            return await Task.FromResult(task);
        }

        public async Task<string> GetStatusAsync()
        {
            var status = string.Empty;

            lock (_playersLock)
            {
                // Get list of accounts that are actively leveling
                var now = DateTime.UtcNow.ToTotalSeconds();
                var players = _players.Where(pair => now - pair.Value.LastSeen <= Strings.SixtyMinutesS)
                                  .Select(pair => pair.Key)
                                  .ToList();

                var data = new List<LevelStats>();
                foreach (var player in players)
                {
                    var min = Math.Min(Math.Max(MaximumLevel + 1, DefaultMinTargetLevel), DefaultMaxTargetLevel);
                    var max = Math.Min(Math.Max(MinimumLevel, DefaultMinTargetLevel), DefaultMaxTargetLevel);
                    var xpTarget = LevelXp[min];
                    var xpStart = LevelXp[max];
                    var xpCurrent = _players[player].XP;
                    var xpReceived = Convert.ToDouble((double)xpCurrent - xpStart);
                    var xpRemaining = Convert.ToDouble((double)xpTarget - xpStart);
                    var xpPercentage = xpReceived / xpRemaining  * 100;

                    var startXp = 0ul;
                    var startTime = DateTime.UtcNow.ToTotalSeconds();

                    // Get latest player xp and time
                    var xpPerTime = _players[player].XpPerTime;
                    for (var i = 0; i < xpPerTime.Count; i++)

                    {
                        var (time, xp) = xpPerTime[i];
                        if ((now - time) <= Strings.TenMinutesS / 2) //Strings.SixtyMinutesS)
                        {
                            startXp = xp;
                            startTime = time;
                            break;
                        }

                        // Purge old XP stats older than 60 minutes
                        _ = xpPerTime.Pop();
                    }

                    var xpDelta = xpPerTime.LastOrDefault().Item2 - startXp;
                    var timeDelta = Math.Max(
                        1,
                        xpPerTime.LastOrDefault().Item1 - startTime
                    );
                    var xpPerHour = xpDelta == 0 || timeDelta == 0
                        ? 0
                        : Convert.ToInt32((double)xpDelta / timeDelta * Strings.SixtyMinutesS);
                    var timeLeft = xpTarget == 0 || xpCurrent == 0 || xpPerHour == 0
                        ? 999.0
                        : Convert.ToDouble((double)xpTarget - xpCurrent) / Convert.ToDouble(xpPerHour);

                    data.Add(new LevelStats
                    {
                        XpTarget = xpTarget,
                        XpStart = xpStart,
                        XpCurrent = xpCurrent,
                        XpPercentage = Math.Round(xpPercentage, 2),
                        Level = _players[player].Level,
                        Username = player,
                        XpPerHour = xpPerHour,
                        TimeLeft = timeLeft,
                    });
                }

                foreach (var player in data)
                {
                    if (!string.IsNullOrEmpty(status))
                    {
                        status += "<br />";
                    }
                    var isDefault = player.TimeLeft == int.MinValue || player.TimeLeft == int.MaxValue;
                    var timeLeftHours = isDefault
                        ? 999
                        : player.TimeLeft;
                    var timeLeftMinutes = isDefault
                        ? 0
                        : (player.TimeLeft - (double)timeLeftHours) * 60;

                    if (player.Level > MaximumLevel)
                    {
                        status += $"{player.Username}: Lvl {player.Level} Complete";
                    }
                    else
                    {
                        var timeLeftStatus = FormatTimeRemaining(timeLeftHours, timeLeftMinutes);
                        status += $"{player.Username}: {player.XpPercentage}% {player.XpPerHour:N0}XP/h {timeLeftStatus}";
                    }
                }
            }

            status = string.IsNullOrEmpty(status)
                ? Strings.DefaultInstanceStatus
                : status;
            return await Task.FromResult(status);
        }

        public Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // Clear all existing players data from cache
            _players.Clear();

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            // Clear all existing players data from cache
            _players.Clear();

            return Task.CompletedTask;
        }

        internal void SetPlayerInfo(string username, ushort level, ulong xp)
        {
            lock (_playersLock)
            {
                AddPlayer(username);

                var now = DateTime.UtcNow.ToTotalSeconds();
                _players[username].Level = level;
                _players[username].XP = xp;
                // Prevent multiple XpPerTime entries that are the same
                if (!_players[username].XpPerTime.Exists(tuple => tuple.Item1 == now))
                {
                    _players[username].XpPerTime.Add((now, xp));
                }
            }
        }

        internal void GotFort(PokemonFortProto fort, string username)
        {
            if (fort == null)
            {
                _logger.LogError($"[{Name}] Received null PokemonFortProto data from username '{username}', skipping...");
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                _logger.LogError($"[{Name}] PokemonFortProto data received but no username specified, skipping...");
                return;
            }

            if (fort.FortType == FortType.Gym)
            {
                // Do not process Pokestop forts, we should not be receiving them anyways
                // but better safe than sorry I guess
                return;
            }

            var coord = new Coordinate(fort.Latitude, fort.Longitude);
            // Check if distance between starting coordinate and fort is within configured
            // radius amount
            if (coord.DistanceTo(StartingCoordinate) > Radius)
                return;

            lock (_playersLock)
            {
                // Add player if it does not exist incase we receive the fort data before
                // the device asks for a task when it is added to the player cache
                if (!_players.ContainsKey(username))
                {
                    AddPlayer(username);
                }

                var player = _players[username];
                // Check if fort has been visited already and unspun Pokestop cache
                // for player still contains the fort
                if (fort.Visited)
                {
                    // Pokestop already spun, check if it's in our unspun cache
                    if (player.UnspunPokestops.ContainsKey(fort.FortId))
                    {
                        // Remove already visited Pokestop from unspun cache
                        player.UnspunPokestops.Remove(fort.FortId);
                    }
                }
                else
                {
                    // Pokestop has not been spun yet, add to unspun Pokestops cache
                    player.UnspunPokestops[fort.FortId] = fort;
                }
            }
        }

        #endregion

        #region Private Methods

        private LevelingTask CreateTask(Coordinate coord, double delay, bool deployEgg = true)
        {
            return new LevelingTask
            {
                Area = Name,
                Action = DeviceActionType.SpinPokestop,
                Delay = delay,
                DeployEgg = deployEgg,
                Latitude = coord.Latitude,
                Longitude = coord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
        }

        private SwitchAccountTask CreateSwitchAccountTask()
        {
            return new SwitchAccountTask
            {
                Area = Name,
                Action = DeviceActionType.SwitchAccount,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
        }

        private Coordinate? GetNextScanLocation(string username, Account? account = null)
        {
            AddPlayer(username);

            var player = _players[username];
            Coordinate? currentCoord = null;

            lock (player.UnspunPokestopsLock)
            {
                if (player.UnspunPokestops.Count == 0)
                {
                    // No unspun Pokestops received/cached yet, return starting location
                    return StartingCoordinate;
                }

                // Find the next closest Pokestop to spin
                var closestPokestop = FindClosestPokestop(player, account);
                if (closestPokestop != null)
                {
                    currentCoord = new Coordinate(closestPokestop.Latitude, closestPokestop.Longitude);
                    // Remove the next Pokestop to spin from the unspun Pokestops cache
                    player.UnspunPokestops.Remove(closestPokestop.FortId);
                    // Keep a cache list of the last 5 Pokestops spun and add the next closest
                    // Pokestop to the last Pokestops spun cache
                    var fortIds = player.LastPokestopsSpun.Keys.ToList();
                    // Purge last Pokestops spun cache if more than 10
                    while (player.LastPokestopsSpun.Count > DefaultLastPokestopsSpunCacheLimit)
                    {
                        // Remove from the bottom of the cache list
                        var fortId = fortIds[fortIds.Count - 1];
                        player.LastPokestopsSpun.Remove(fortId);
                    }
                    player.LastPokestopsSpun[closestPokestop.FortId] = currentCoord;
                }
            }

            if (currentCoord != null)
            {
                player.LastLocation = currentCoord;
            }
            return currentCoord;
        }

        private async Task<double> GetDelayAsync(Coordinate currentCoord, string uuid, Account? account = null)
        {
            double delay;
            ulong encounterTime;

            try
            {
                var result = Cooldown.SetCooldown(account, currentCoord);
                delay = result.Delay;
                encounterTime = result.EncounterTime;
            }
            catch (Exception ex)
            {
                // Failed to calculate cooldown for account to destination
                _logger.LogError($"[{Name}] [{uuid}] Failed to calculate cooldown for account '{account}' to destination '{currentCoord}'\nError: {ex}");
                return 0;
            }

            try
            {
                await Cooldown.SetEncounterAsync(/* TODO: _deviceFactory*/ null, account, currentCoord, encounterTime);
            }
            catch (Exception ex)
            {
                // Failed to save cooldown
                _logger.LogError($"[{Name}] [{uuid}] Failed to save cooldown for account '{account}' to destination '{currentCoord}'\nError: {ex}");
                return 0;
            }

            return delay;
        }

        private PokemonFortProto? FindClosestPokestop(PlayerLevelingData player, Account? account = null)
        {
            PokemonFortProto? closest = null;
            double closestDistance = Strings.DefaultDistance;
            // Try to get LastLocation from player stats cache, otherwise get it from account, and lastly
            // worst case return the starting coordinate of the route
            var currentCoord = player.LastLocation ?? new Coordinate
            (
                account?.LastEncounterLatitude ?? StartingCoordinate.Latitude,
                account?.LastEncounterLongitude ?? StartingCoordinate.Longitude
            );
            // Reverse the unspun Pokestops list which should sort based on closest/most
            // recently added to the list
            var reversedUnspunPokestops = player.UnspunPokestops.Values.Reverse().ToList();
            var excludeCoordinates = player.LastPokestopsSpun.Values.ToList();

            // Loop through player's unspun Pokestops cache to find the next closest target
            foreach (var stop in reversedUnspunPokestops)
            {
                var stopCoord = new Coordinate(stop.Latitude, stop.Longitude);
                // Loop through player's already spun Pokestops cache list and ignore
                // any nearby Pokestops already spun
                foreach (var lastCoord in excludeCoordinates)
                {
                    // Skip any Pokestops within range that have already been spun
                    if (stopCoord.DistanceTo(lastCoord) <= Strings.SpinRangeM)
                    {
                        continue;
                    }
                }

                // Calculate closest Pokestop distance from current location
                var dist = currentCoord.DistanceTo(stopCoord);
                if (dist < closestDistance)
                {
                    closest = stop;
                    closestDistance = dist;
                }
            }
            return closest;
        }

        private void AddPlayer(string username)
        {
            if (!_players.ContainsKey(username))
            {
                _players.Add(username, new());
            }
        }

        private static string FormatTimeRemaining(double hours, double minutes)
        {
            var roundedHours = Math.Round(hours, 1);
            var roundedMinutes = Math.Round(minutes, 1);
            var status = hours > 0 && minutes > 0
                // Return hours and minutes if both are set
                ? $"{roundedHours}h:{roundedMinutes}m"
                : hours > 0 && minutes == 0
                    // Return only hours if hours set but not minutes
                    ? $"{roundedHours}h"
                    : hours == 0 && minutes > 0
                        // Return only minutes if hours not set but minutes are
                        ? $"{roundedMinutes}m"
                        // Return default instance status `--`
                        : Strings.DefaultInstanceStatus;
            return status;
        }

        #endregion

        private class LevelStats
        {
            public ulong XpTarget { get; set; }

            public ulong XpStart { get; set; }

            public ulong XpCurrent { get; set; }

            public double XpPercentage { get; set; }

            public ushort Level { get; set; }

            public string Username { get; set; }

            public double XpPerHour { get; set; }

            public double TimeLeft { get; set; }
        }
    }
}