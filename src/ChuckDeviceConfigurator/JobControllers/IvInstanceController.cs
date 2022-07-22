namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Collections;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry;
    using ChuckDeviceController.Geometry.Models;

    public class IvInstanceController : IJobController, IScanNextInstanceController
    {
        private const uint CheckScanHistoryIntervalS = 5;
        private static readonly List<ushort> EventAttackIV = new()
        {
            0,
            1,
            15,
        };

        #region Variables

        private readonly ILogger<IvInstanceController> _logger;
        //private readonly IndexedPriorityQueue<Pokemon> _pokemonQueue;
        private readonly PokemonPriorityQueue<Pokemon> _pokemonQueue;
        private readonly List<ScannedPokemon> _scannedPokemon;
        private readonly object _queueLock = new();
        private readonly object _scannedLock = new();
        private readonly object _statsLock = new();
        private readonly System.Timers.Timer _timer;
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

        public IvInstanceController(Instance instance, List<MultiPolygon> multiPolygons, List<uint> pokemonIds)
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

            _pokemonQueue = new PokemonPriorityQueue<Pokemon>(QueueLimit);
            _scannedPokemon = new List<ScannedPokemon>();
            _startDate = DateTime.UtcNow.ToTotalSeconds();
            _logger = new Logger<IvInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _timer = new System.Timers.Timer(CheckScanHistoryIntervalS * 1000); // 5 second interval
            _timer.Elapsed += (sender, e) => CheckScannedPokemonHistory();
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            if (ScanNextCoordinates.Count > 0)
            {
                var currentCoord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateScanNextTask(currentCoord);
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
                    ? _count / (now - _startDate) * Strings.SixtyMinutesS
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

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            _timer.Stop();
            return Task.CompletedTask;
        }

        #endregion

        #region IV Queue Management

        internal void GotPokemonIV(Pokemon pokemon)
        {
            var pkmnCoord = new Coordinate(pokemon.Latitude, pokemon.Longitude);
            if (!GeofenceService.InMultiPolygon((List<MultiPolygon>)MultiPolygons, pkmnCoord))
            {
                // Pokemon outside of geofence area for job controller, skipping...
                return;
            }

            lock (_queueLock)
            {
                var index = _pokemonQueue.IndexOf(pokemon);
                if (index > -1)
                {
                    _pokemonQueue.RemoveAt(index);
                }

                // Checks if instance is for event as well as the Pokemon has not been re-scanned
                // yet and it also meets the desired IV stats for event re-scans.
                if (IsEvent && !pokemon.IsEvent && 
                    EventAttackIV.Contains(pokemon.AttackIV ?? 999) &&
                    pokemon.DefenseIV == 15 && pokemon.StaminaIV == 15)
                {
                    pokemon.IsEvent = true;
                    // Push Pokemon to top of queue
                    _pokemonQueue.Insert(0, pokemon);
                }
            }

            UpdateStats();
        }

        internal void GotPokemon(Pokemon pokemon)
        {
            var isNotLure = !string.IsNullOrEmpty(pokemon.PokestopId) || pokemon.SpawnId > 0;
            var matchesEvent = pokemon.IsEvent == IsEvent;
            var isDesiredPokemon = IsDesiredPokemon(pokemon);
            var pkmnCoord = new Coordinate(pokemon.Latitude, pokemon.Longitude);
            var inGeofence = GeofenceService.InMultiPolygon((List<MultiPolygon>)MultiPolygons, pkmnCoord);
            if (!(isNotLure && matchesEvent && isDesiredPokemon && inGeofence))
                return;

            lock (_queueLock)
            {
                if (_pokemonQueue.Contains(pokemon))
                {
                    return;
                }

                var index = GetLastIndexOf(pokemon.PokemonId, pokemon.Form ?? 0);
                if (_pokemonQueue.Count >= QueueLimit && index == null)
                {
                    _logger.LogWarning($"[{Name}] Queue is full!");
                }
                else if (_pokemonQueue.Count >= QueueLimit)
                {
                    if (index != null)
                    {
                        _pokemonQueue.Insert((int)index, pokemon);
                        //_pokemonQueue.Set((int)index, pokemon);
                        // Remove last item in the queue
                        _ = _pokemonQueue.PopLast();
                    }
                }
                else if (index != null)
                {
                    _pokemonQueue.Insert((int)index, pokemon);
                }
                else
                {
                    _pokemonQueue.Insert(0, pokemon);
                }
            }
        }

        internal void RemoveFromQueue(string encounterId)
        {
            // Remove Pokemon from IV queue based on encounterId
            lock (_queueLock)
            {
                int index = -1;
                for (var i = 0; i < _pokemonQueue.Count; i++)
                {
                    var pokemon = _pokemonQueue[i];
                    if (pokemon.Id == encounterId)
                    {
                        index = i;
                        break;
                    }
                }
                if (index > -1)
                {
                    _pokemonQueue.RemoveAt(index);
                }
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

        private void CheckScannedPokemonHistory()
        {
            ScannedPokemon? scannedPokemon = null;
            lock (_scannedLock)
            {
                if (_scannedPokemon.Count == 0)
                {
                    return;
                }

                scannedPokemon = _scannedPokemon.FirstOrDefault();
                _scannedPokemon.Remove(scannedPokemon);
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            var timeSince = now - scannedPokemon.DateScanned;
            if (timeSince < 120)
            {
                Thread.Sleep(Convert.ToInt32(120 - timeSince) * 1000);
                // TODO: Should exit
            }

            // TODO: Spawn new thread instead of timer since it is continously checked within this scope
            var success = false;
            Pokemon? pokemonReal = null;
            while (!success)
            {
                try
                {
                    // TODO: pokemonReal = Pokemon.GeByIdAsync(scannedPokemon.Pokemon.Id);
                    success = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex}");
                    Thread.Sleep(1000);
                    // TODO: Should exit
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
                    _logger.LogInformation($"[{Name}]Checked Pokemon {pokemonReal.Id} has IV");
                }
            }
        }

        private bool IsDesiredPokemon(Pokemon pokemon)
        {
            if (PokemonIds.Contains(pokemon.PokemonId.ToString()))
                return true;

            if (pokemon.Form != null && pokemon.Form > 0)
            {
                var key = $"{pokemon.PokemonId}_f{pokemon.Form}";
                return PokemonIds.Contains(key);
            }

            return false;
        }

        private int? GetPriorityIndex(uint pokemonId, ushort formId)
        {
            var key = formId > 0
                ? $"{pokemonId}_f{formId}"
                : $"{pokemonId}";
            var priority = PokemonIds.IndexOf(key);
            if (priority != -1)
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
                var i = 0u;
                foreach (var pokemon in _pokemonQueue)
                {
                    var priority = GetPriorityIndex(pokemon.PokemonId, formId);
                    if (targetPriority < priority)
                    {
                        return i;
                    }
                    i++;
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