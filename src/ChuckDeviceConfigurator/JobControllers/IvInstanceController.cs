namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class ScannedPokemon
    {
        public DateTime Date { get; set; }

        public Pokemon Pokemon { get; set; }
    }

    public class IvInstanceController : IJobController, IScanNext
    {
        #region Variables

        private readonly ILogger<IvInstanceController> _logger;
        private readonly Queue<Pokemon> _pokemonQueue;
        private readonly List<ScannedPokemon> _scannedPokemon;
        private readonly object _queueLock = new();
        private readonly object _scannedLock = new();
        private readonly System.Timers.Timer _timer;
        private ulong _count = 0;
        private ulong _startDate;

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygon { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public ushort QueueLimit { get; }

        public IReadOnlyList<uint> PokemonIds { get; }

        public bool EnableLureEncounters { get; }

        public Queue<Coordinate> ScanNextCoordinates { get; } = new();

        #endregion

        #region Constructor

        public IvInstanceController(Instance instance, List<MultiPolygon> multiPolygon, List<uint> pokemonIds)
        {
            Name = instance.Name;
            MultiPolygon = multiPolygon;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;
            QueueLimit = instance.Data?.IvQueueLimit ?? Strings.DefaultIvQueueLimit;
            PokemonIds = pokemonIds;
            EnableLureEncounters = instance.Data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters;

            _pokemonQueue = new Queue<Pokemon>();
            _scannedPokemon = new List<ScannedPokemon>();
            _startDate = DateTime.UtcNow.ToTotalSeconds();
            _logger = new Logger<IvInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _timer = new System.Timers.Timer(5 * 1000); // 5 second interval
            _timer.Elapsed += (sender, e) => CheckScannedPokemonHistory();
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(GetTaskOptions options)
        {
            if (ScanNextCoordinates.Count > 0)
            {
                var currentCoord = ScanNextCoordinates.Dequeue();
                var scanNextTask = new IvTask
                {
                    Area = Name,
                    Action = DeviceActionType.ScanPokemon,
                    MinimumLevel = MinimumLevel,
                    MaximumLevel = MaximumLevel,
                    Latitude = currentCoord.Latitude,
                    Longitude = currentCoord.Longitude,
                    LureEncounter = EnableLureEncounters,
                };
                return await Task.FromResult(scanNextTask);
            }

            Pokemon? pokemon = null;
            lock (_queueLock)
            {
                if (_pokemonQueue.Count == 0)
                {
                    return null;
                }

                //pokemon = _pokemonQueue.FirstOrDefault();
                //_pokemonQueue.Remove(pokemon);
                pokemon = _pokemonQueue.Dequeue();
            }
            if (pokemon == null)
            {
                return null;
            }

            // Check if Pokemon was first seen more than 10 minutes ago,
            // if so call the GetTask method again to fetch a new Pokemon
            // to IV scan.
            var now = DateTime.UtcNow.ToTotalSeconds();
            if (now - (pokemon.FirstSeenTimestamp ?? 1) >= 600)
            {
                return await GetTaskAsync(options);
            }

            lock (_scannedLock)
            {
                _scannedPokemon.Add(new ScannedPokemon
                {
                    Date = DateTime.UtcNow,
                    Pokemon = pokemon,
                });
            }

            var task = new IvTask
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
            return await Task.FromResult(task);
        }

        public async Task<string> GetStatusAsync()
        {
            var ivh = -1d;
            if (_startDate > 0)
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                // Prevent dividing by zero
                ivh = _count == 0
                    ? _count / (now - _startDate) * 3600
                    : 0;
            }
            var ivhStr = "--";
            if (ivh != -1)
            {
                ivhStr = Math.Round(ivh).ToString("N0");
            }
            var status = $"<a href='/Instance/IvQueue/{Uri.EscapeDataString(Name)}'>Queue</a>: {_pokemonQueue.Count}, IV/h: {ivhStr}";
            return await Task.FromResult(status);
        }

        public void Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");
        }

        public void Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");

            _timer.Stop();
        }

        #endregion

        #region Private Methods

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
            var timeSince = now - scannedPokemon.Date.ToTotalSeconds();
            if (timeSince < 120)
            {
                Thread.Sleep(Convert.ToInt32(120 - timeSince) * 1000);
                // TODO: Should exit
            }

            // TODO: Spawn new thread instead of timer
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

        #endregion
    }
}