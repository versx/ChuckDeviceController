namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using POGOProtos.Rpc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class PlayerLevelingData
    {
        public Dictionary<string, PokemonFortProto> UnspunPokestops { get; set; } = new();

        public object UnspunPokestopsLock { get; set; } = new();

        public List<Coordinate> LastPokestopsSpun { get; set; } = new();

        public ulong LastSeen { get; set; }

        public ulong XP { get; set; }

        public ushort Level { get; set; }

        public List<(ulong, ulong)> XpPerTime { get; set; } = new(); // time, xp

        public Coordinate LastLocation { get; set; }
    }

    public class LevelingInstanceController : IJobController
    {
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

        private readonly ILogger<CircleInstanceController> _logger;
        private readonly Dictionary<string, PlayerLevelingData> _players;
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

        // NOTE: 'StorePlayerData' is used by proto processor to ignore encountered/received leveling data
        public bool StorePlayerData { get; } // TODO: Make 'StorePlayerData' configurable via Instance.Data

        public ulong Radius { get; set; } // TODO: Make 'Radius' configurable via Instance.Data

        #endregion

        // TODO: OnComplete event

        #region Constructor

        public LevelingInstanceController(Instance instance, List<MultiPolygon> multiPolygons, Coordinate startingCoord, bool storePlayerData, ulong radius)
        {
            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;

            StartingCoordinate = startingCoord;
            StorePlayerData = storePlayerData;
            Radius = radius;

            _logger = new Logger<CircleInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _players = new Dictionary<string, PlayerLevelingData>();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            if (string.IsNullOrEmpty(options.AccountUsername))
            {
                // TODO: Throw error
                return null;
            }

            if (options.Account == null)
            {
                // TODO: Throw error
                return null;
            }

            var currentCoord = GetNextScanLocation(options.AccountUsername, options.Account);
            if (currentCoord == null)
            {
                _logger.LogError($"[{Name}] [{options.Uuid}] Failed to get next scan location for leveling instance");
                return null;
            }

            var delay = await GetDelayAsync(currentCoord, options.Uuid, options.Account);

            lock (_playersLock)
            {
                _players[options.AccountUsername].LastSeen = DateTime.UtcNow.ToTotalSeconds();
            }

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
                    var xpTarget = LevelXp[Math.Min(Math.Max(MaximumLevel + 1, 0), 40)];
                    var xpStart = LevelXp[Math.Min(Math.Max(MinimumLevel, (ushort)0), (ushort)40)];
                    var xpCurrent = _players[player].XP;
                    var xpReceived = Convert.ToDouble((double)xpCurrent - (double)xpStart);
                    var xpRemaining = Convert.ToDouble((double)xpTarget - (double)xpStart);
                    var xpPercentage = xpReceived / xpRemaining  * 100;

                    var startXp = 0ul;
                    var startTime = DateTime.UtcNow.ToTotalSeconds();

                    var xpPerTime = _players[player].XpPerTime;
                    // Get latest player xp and time
                    foreach (var (time, xp) in xpPerTime)
                    {
                        if ((now - time) <= Strings.SixtyMinutesS)
                        {
                            startXp = xp;
                            startTime = time;
                            break;
                        }

                        // TODO: Remove first?
                        //_playerXpPerTime.PopFirst();
                    }

                    var xpDelta = xpPerTime.LastOrDefault().Item2 - startXp;
                    var timeDelta = Math.Max(
                        1,
                        xpPerTime.LastOrDefault().Item1 - startTime
                    );
                    var xpPerHour = Convert.ToInt32((double)xpDelta / (double)timeDelta * Strings.SixtyMinutesS);
                    var timeLeft = xpPerHour == 0
                        ? 999.0
                        : Convert.ToDouble((double)xpTarget - (double)xpCurrent) / Convert.ToDouble(xpPerHour);

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
                    var timeLeftHours = 0d;
                    var timeLeftMinutes = 0d;
                    if (timeLeftHours == int.MinValue || timeLeftHours == int.MaxValue)
                    {
                        timeLeftHours = 999;
                        timeLeftMinutes = 0;
                    }
                    else
                    {
                        timeLeftHours = player.TimeLeft;
                        timeLeftMinutes = (player.TimeLeft - (double)timeLeftHours) * 60;
                    }

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

        public async Task Reload()
        {
            await Task.CompletedTask;
        }

        public async Task Stop()
        {
            await Task.CompletedTask;
        }

        internal void SetPlayerInfo(string username, ushort level, ulong xp)
        {
            lock (_playersLock)
            {
                AddPlayer(username);

                var now = DateTime.UtcNow.ToTotalSeconds();
                _players[username].Level = level;
                _players[username].XP = xp;
                _players[username].XpPerTime.Add((now, xp));
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

            if (fort.FortType != FortType.Gym)
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
                // the device asks for a task and is added to the player cache
                if (!_players.ContainsKey(username))
                {
                    AddPlayer(username);
                }

                var player = _players[username];
                lock (player.UnspunPokestopsLock)
                {
                    // Check if fort has been visited already and unspun Pokestop cache
                    // for player still contains the fort
                    if (fort.Visited && player.UnspunPokestops.ContainsKey(fort.FortId))
                    {
                        // Pokestop already spun, remove fort
                        player.UnspunPokestops.Remove(fort.FortId);
                    }
                    else
                    {
                        // Pokestop has not been spun yet, add to unspun Pokestop cache
                        player.UnspunPokestops[fort.FortId] = fort;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private ITask CreateTask(Coordinate coord, double delay, bool deployEgg = true)
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

                var reversedUnspunPokestops = player.UnspunPokestops.Values.Reverse().ToList();
                var closestPokestop = FindClosestPokestop(
                    reversedUnspunPokestops,
                    player.LastPokestopsSpun,
                    account
                );
                if (closestPokestop != null)
                {
                    currentCoord = new Coordinate(closestPokestop.Latitude, closestPokestop.Longitude);
                    // Remove Pokestop from unspun Pokestop cache and add to last Pokestops
                    // spun cache
                    player.UnspunPokestops.Remove(closestPokestop.FortId);
                    while (player.LastPokestopsSpun.Count > 5)
                    {
                        player.LastPokestopsSpun.RemoveAt(0);
                    }
                    player.LastPokestopsSpun.Add(currentCoord);
                }
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

        private PokemonFortProto? FindClosestPokestop(List<PokemonFortProto> unspunPokestops, List<Coordinate> excludeCoordinates, Account? account = null)
        {
            PokemonFortProto? closest = null;
            double closestDistance = Strings.DefaultDistance;
            var currentCoord = new Coordinate(
                account?.LastEncounterLatitude ?? StartingCoordinate.Latitude,
                account?.LastEncounterLongitude ?? StartingCoordinate.Longitude
            );

            foreach (var stop in unspunPokestops)
            {
                var stopCoord = new Coordinate(stop.Latitude, stop.Longitude);
                foreach (var lastCoord in excludeCoordinates)
                {
                    // TODO: Hmm, not sure if this is correct logic or not,
                    // skipping Pokestop if it's not within spin range. :thinking:
                    // Need to double check
                    if (stopCoord.DistanceTo(lastCoord) <= Strings.SpinRangeM)
                    {
                        continue;
                    }
                }

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