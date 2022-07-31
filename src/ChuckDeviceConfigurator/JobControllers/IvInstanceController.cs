namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.JobControllers.Contracts;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Models;

    public class IvInstanceController : IJobController, ILureInstanceController, IScanNextInstanceController
    {
        #region Constants

        //private const uint CheckScanHistoryIntervalS = 5;
        private const ushort DefaultTerminateThreadTimeoutMs = 5 * 1000; // 5 seconds
        private const ushort MaximumRetryCount = 10;

        #endregion

        #region Variables

        private readonly ILogger<IvInstanceController> _logger;
        private readonly IDbContextFactory<MapDataContext> _mapFactory;
        private readonly PokemonPriorityQueue<Pokemon> _pokemonQueue;
        private readonly PokemonPriorityQueue<ScannedPokemon> _scannedPokemon;
        private static readonly List<ushort> EventAttackIV = new() { 0, 1, 15 };

        private readonly object _queueLock = new();
        private readonly object _scannedLock = new();
        private readonly object _statsLock = new();

        private bool _shouldExitThread = false;
        private Thread? _checkThread;
        //private readonly System.Timers.Timer _timer;
        private ulong _count = 0;
        private ulong _startDate;

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public ushort QueueLimit { get; }

        public List<string> PokemonIds { get; }

        public bool EnableLureEncounters { get; }

        public Queue<Coordinate> ScanNextCoordinates { get; } = new();

        #endregion

        #region Constructor

        public IvInstanceController(
            IDbContextFactory<MapDataContext> mapFactory,
            Instance instance,
            List<MultiPolygon> multiPolygons,
            List<uint> pokemonIds)
        {
            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;
            QueueLimit = instance.Data?.IvQueueLimit ?? Strings.DefaultIvQueueLimit;
            PokemonIds = pokemonIds.Select(id => Convert.ToString(id))
                                   .ToList();
            EnableLureEncounters = instance.Data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters;

            _logger = new Logger<IvInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _mapFactory = mapFactory;
            _pokemonQueue = new PokemonPriorityQueue<Pokemon>(QueueLimit);
            _scannedPokemon = new PokemonPriorityQueue<ScannedPokemon>();
            _startDate = DateTime.UtcNow.ToTotalSeconds();

            //_timer = new System.Timers.Timer(CheckScanHistoryIntervalS * 1000); // 5 second interval
            //_timer.Elapsed += (sender, e) => CheckScannedPokemonHistory();
            //_timer.Start();

            _checkThread = new Thread(new ThreadStart(CheckScannedPokemonHistory))
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest,
            };
            _checkThread.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            if (ScanNextCoordinates.Count > 0)
            {
                var coord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateScanNextTask(coord);
                _logger.LogInformation($"[{Name}] [{options.Uuid}] Executing ScanNext API job at '{coord}'");
                return await Task.FromResult(scanNextTask);
            }

            Pokemon? pokemon = null;
            lock (_queueLock)
            {
                if (_pokemonQueue.Count == 0)
                {
                    return null;
                }

                pokemon = _pokemonQueue.Pop();
            }
            if (pokemon == null)
            {
                return null;
            }

            // Check if Pokemon is close to expiring, if so fetch another
            // IV task from the Pokemon queue.
            if (pokemon.IsExpirationSoon)
            {
                return await GetTaskAsync(options);
            }

            lock (_scannedLock)
            {
                _scannedPokemon.Add(new ScannedPokemon
                {
                    DateScanned = DateTime.UtcNow.ToTotalSeconds(),
                    Pokemon = pokemon,
                });
            }

            var task = CreateIvTask(pokemon);
            return await Task.FromResult(task);
        }

        public async Task<string> GetStatusAsync()
        {
            var ivh = -1d;
            if (_startDate > 0)
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                // Prevent dividing by zero
                ivh = _count > 0
                    ? (double)_count / (now - _startDate) * Strings.SixtyMinutesS
                    : 0;
            }
            var ivhStr = Strings.DefaultInstanceStatus;
            if (ivh != -1)
            {
                ivhStr = Math.Round(ivh).ToString("N0");
            }
            var html = $"<a href='/Instance/IvQueue/{Uri.EscapeDataString(Name)}'>Queue</a>";
            var status = $"{html}: {_pokemonQueue.Count}, IV/h: {ivhStr}";
            return await Task.FromResult(status);
        }

        public IReadOnlyList<Pokemon> GetQueue() => _pokemonQueue.ToList();

        public Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // Clear existing lists
            _pokemonQueue.Clear();
            _scannedPokemon.Clear();

            //if (!_timer.Enabled)
            //{
            //    _timer.Start();
            //}

            _shouldExitThread = false;

            if (_checkThread != null)
            {
                if (!_checkThread.Join(DefaultTerminateThreadTimeoutMs))
                {
                    _logger.LogError($"[{Name}] Failed to terminate IV queue thread");
                }
            }
            _checkThread = new Thread(new ThreadStart(CheckScannedPokemonHistory));
            _checkThread.Start();

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            //_timer.Stop();

            _shouldExitThread = true;

            if (_checkThread != null)
            {
                // Attempt to terminate thread, wait timeout before forcefully terminating
                if (!_checkThread.Join(DefaultTerminateThreadTimeoutMs))
                {
                    // Unable to terminate thread after timeout period
                    _logger.LogError($"[{Name}] Failed to terminate IV queue thread");
                }
                _checkThread = null;
            }

            return Task.CompletedTask;
        }

        #endregion

        #region IV Queue Management

        /// <summary>
        /// Handles scanned Pokemon encounter processed by controller. Used to
        /// determine IVs scanned per hour.
        /// </summary>
        /// <param name="pokemon">Pokemon encounter</param>
        /// <param name="hasIv">Pokemon has IVs</param>
        internal void GotPokemon(Pokemon pokemon, bool hasIv)
        {
            // First thing to do is ensure that the received Pokemon is within this IV job
            // controller's geofence bounds.
            var pkmnCoord = pokemon.ToCoordinate();
            if (!GeofenceService.InMultiPolygon((List<MultiPolygon>)MultiPolygons, pkmnCoord))
            {
                // Pokemon outside of geofence area for job controller, skipping...
                return;
            }

            // Check if incoming encounter IVs were found
            if (hasIv && pokemon.AttackIV != null)
            {
                // Pokemon has IVs scanned, attempt to remove it from the Pokemon queue if it's in it.
                lock (_queueLock)
                {
                    // Remove Pokemon from scan queue
                    var index = _pokemonQueue.IndexOf(pokemon);
                    if (index > -1)
                    {
                        // Remove Pokemon from queue at index
                        _pokemonQueue.RemoveAt(index);
                    }
                    // Checks if instance is for event as well as the Pokemon has not been re-scanned
                    // yet and that it also meets the desired IV stats for event re-scans.
                    if (IsEvent && !pokemon.IsEvent &&
                        EventAttackIV.Contains(pokemon.AttackIV ?? 999) &&
                        pokemon.DefenseIV == 15 && pokemon.StaminaIV == 15)
                    {
                        pokemon.IsEvent = true;
                        // Push Pokemon to top of queue
                        _pokemonQueue.Insert(0, pokemon);
                    }
                }

                // Update internal IV stats for job controller status
                UpdateStats();
                return;
            }

            // Pokemon does not have IVs scanned, check if we can add it to this job controller
            // Pokemon queue depending if it matches some conditions, otherwise ignore it.
            var isNotLure = !string.IsNullOrEmpty(pokemon.PokestopId) || pokemon.SpawnId > 0;
            var isEventSpawn = pokemon.IsEvent == IsEvent;
            var isDesiredPokemon = IsDesiredPokemon(pokemon);
            if (!(isNotLure && isEventSpawn && isDesiredPokemon))
                return;

            lock (_queueLock)
            {
                // Check if Pokemon without IVs is still in queue pending
                if (_pokemonQueue.Contains(pokemon))
                {
                    return;
                }

                // Find the last index of the same Pokemon in the queue to insert pending encounter
                var index = GetLastIndexOf(pokemon.PokemonId, pokemon.Form ?? 0);
                if (_pokemonQueue.Count >= QueueLimit && index == null)
                {
                    _logger.LogWarning($"[{Name}] Queue is full!");
                }
                else if (_pokemonQueue.Count >= QueueLimit)
                {
                    if (index != null)
                    {
                        // Insert in queue at index
                        _pokemonQueue.Insert((int)index, pokemon);

                        // Remove last item in the queue
                        _ = _pokemonQueue.PopLast();
                    }
                }
                else if (index != null)
                {
                    // Insert in queue at index
                    _pokemonQueue.Insert((int)index, pokemon);
                }
                else
                {
                    // Add to the end of the queue
                    _pokemonQueue.Add(pokemon);
                }
            }
        }

        /// <summary>
        /// Remove Pokemon from IV queue based on encounter ID.
        /// </summary>
        /// <param name="encounterId">Pokemon encounter ID</param>
        internal void RemoveFromQueue(string encounterId)
        {
            lock (_queueLock)
            {
                // Find index of Pokemon encounter to remove by encounter id
                var index = _pokemonQueue.FindIndex(pokemon => pokemon.Id == encounterId);
                if (index > -1)
                {
                    // Remove encounter from queue by index
                    _pokemonQueue.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Clear all pending Pokemon encounters from IV queue.
        /// </summary>
        internal void ClearQueue()
        {
            lock (_queueLock)
            {
                _pokemonQueue.Clear();
            }
        }

        #endregion

        #region Private Methods

        private IvTask CreateIvTask(Pokemon pokemon)
        {
            return new IvTask
            {
                Area = Name,
                Action = DeviceActionType.ScanIV,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
                EncounterId = pokemon.Id,
                Latitude = pokemon.Latitude,
                Longitude = pokemon.Longitude,
                IsSpawnpoint = pokemon.SpawnId > 0,
                LureEncounter = EnableLureEncounters,
            };
        }

        private CircleTask CreateScanNextTask(Coordinate currentCoord)
        {
            return new CircleTask
            {
                Area = Name,
                Action = DeviceActionType.ScanPokemon,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                LureEncounter = EnableLureEncounters,
            };
        }

        private async void CheckScannedPokemonHistory()
        {
            const ushort TwoMinutesS = 120;

            while (!_shouldExitThread)
            {
                ScannedPokemon? scannedPokemon = null;
                lock (_scannedLock)
                {
                    if (_scannedPokemon.Count == 0)
                    {
                        Thread.Sleep(5 * 1000);
                        continue;
                    }

                    scannedPokemon = _scannedPokemon.Pop();
                }

                var now = DateTime.UtcNow.ToTotalSeconds();
                var timeSince = now - scannedPokemon?.DateScanned;
                // Check if scanned Pokemon has been seen within the last 2 minutes,
                // if so then give IV workers time to encounter
                if (timeSince < TwoMinutesS)
                {
                    var timeDelta = Convert.ToInt32(TwoMinutesS - timeSince);
                    Thread.Sleep(timeDelta * 1000);
                    continue;
                }

                var success = false;
                var retryCount = 0;
                Pokemon? pokemonReal = null;
                using (var context = _mapFactory.CreateDbContext())
                {
                    while (!success)
                    {
                        if (retryCount >= MaximumRetryCount)
                        {
                            // Max retry count exceeded, skip
                            break;
                        }
                        retryCount++;

                        try
                        {
                            if (scannedPokemon != null)
                            {
                                pokemonReal = await context.Pokemon.FindAsync(scannedPokemon.Pokemon.Id);
                                success = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error: {ex}");
                            Thread.Sleep(1000);
                            continue;
                        }

                        Thread.Sleep(1000);
                    }
                }

                if (pokemonReal != null)
                {
                    if (pokemonReal.AttackIV == null)
                    {
                        _logger.LogInformation($"[{Name}] Checked Pokemon {pokemonReal.Id} doesn't have IV");
                    }
                    else
                    {
                        _logger.LogInformation($"[{Name}] Checked Pokemon {pokemonReal.Id} has IV");
                    }
                }

                // TODO: Check usage
                Thread.Sleep(100);
            }
        }

        private bool IsDesiredPokemon(Pokemon pokemon)
        {
            if (PokemonIds.Contains(pokemon.PokemonId.ToString()))
                return true;

            var key = $"{pokemon.PokemonId}_f{pokemon.Form}";
            var result = pokemon.Form > 0 && PokemonIds.Contains(key);
            return result;
        }

        private int? GetPriorityIndex(uint pokemonId, ushort formId)
        {
            var key = formId > 0
                ? $"{pokemonId}_f{formId}"
                : $"{pokemonId}";
            var priority = PokemonIds.IndexOf(key);
            if (priority > -1)
            {
                return priority;
            }
            if (formId > 0)
            {
                var index = PokemonIds.IndexOf($"{pokemonId}");
                return index;
            }
            return null;
        }

        private uint? GetLastIndexOf(uint pokemonId, ushort formId)
        {
            var targetPriority = GetPriorityIndex(pokemonId, formId);
            if (targetPriority == null)
            {
                return null;
            }

            lock (_queueLock)
            {
                for (var i = 0; i < _pokemonQueue.Count; i++)
                {
                    var pokemon = _pokemonQueue[i];
                    var priority = GetPriorityIndex(pokemon.PokemonId, formId);
                    if (priority > -1 && targetPriority < priority)
                    {
                        return (uint)i;
                    }
                }
            }
            return null;
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
                    // Increment IV stat count
                    _count++;
                }
            }
        }

        #endregion

        private class ScannedPokemon
        {
            public ulong DateScanned { get; set; }

            public Pokemon Pokemon { get; set; }
        }
    }
}