namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Factories;
    using Chuck.Infrastructure.Data.Repositories;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.Geofence;
    using Chuck.Infrastructure.Geofence.Models;
    using Chuck.Infrastructure.JobControllers;
    using Chuck.Infrastructure.JobControllers.Tasks;

    public struct ScannedPokemon
    {
        public DateTime Date { get; set; }

        public Pokemon Pokemon { get; set; }
    }

    public class IVInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<IVInstanceController> _logger;

        private readonly PokemonRepository _pokemonRepository;
        private List<Pokemon> _pokemonQueue;
        private List<ScannedPokemon> _scannedPokemon;
        private readonly object _queueLock = new object();
        private readonly object _scannedLock = new object();
        private readonly System.Timers.Timer _timer;
        private bool _shouldExit;
        private int _count = 0;
        private DateTime _startDate;

        #endregion

        #region Properties

        public string Name { get; set; }

        public List<MultiPolygon> MultiPolygon { get; set; }

        public List<uint> PokemonList { get; set; }

        public ushort MinimumLevel { get; set; }

        public ushort MaximumLevel { get; set; }

        public int IVQueueLimit { get; set; }

        #endregion

        #region Constructor

        public IVInstanceController(string name, List<MultiPolygon> multiPolygon, List<uint> pokemonList, ushort minLevel, ushort maxLevel, int ivQueueLimit)
        {
            Name = name;
            MultiPolygon = multiPolygon;
            PokemonList = pokemonList;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            IVQueueLimit = ivQueueLimit;

            _logger = new Logger<IVInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _pokemonRepository = new PokemonRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _pokemonQueue = new List<Pokemon>();
            _scannedPokemon = new List<ScannedPokemon>();
            _timer = new System.Timers.Timer
            {
                Interval = 1000
            };
            // TODO: Maybe no ThreadPool
            _timer.Elapsed += (sender, e) => ThreadPool.QueueUserWorkItem(_ => LoopCache());
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTask(string uuid, string accountUsername, bool startup)
        {
            Pokemon pokemon = null;
            lock (_queueLock)
            {
                if (_pokemonQueue.Count == 0)
                    return null;

                pokemon = _pokemonQueue.PopFirst(out _pokemonQueue);
            }
            if (pokemon == null)
                return null;

            var timeDiff = DateTime.UtcNow.ToTotalSeconds() - pokemon.FirstSeenTimestamp;
            if (timeDiff >= 600)
            {
                return await GetTask(uuid, accountUsername, false).ConfigureAwait(false);
            }
            //lock (_scannedLock)
            //{
                _scannedPokemon.Add(new ScannedPokemon
                {
                    Date = DateTime.UtcNow,
                    Pokemon = pokemon,
                });
            //}
            return new IVTask
            {
                Area = Name,
                Action = ActionType.ScanIV,
                Latitude = pokemon.Latitude,
                Longitude = pokemon.Longitude,
                IsSpawnpoint = pokemon.SpawnId != null,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
        }

        public async Task<string> GetStatus() // TODO: Formatted
        {
            var ivh = -1d;
            if (_startDate != default)
            {
                ivh = _count / DateTime.UtcNow.Subtract(_startDate).TotalSeconds * 3600; // TODO: :thinking:
            }
            var ivhString = "-";
            if (ivh != -1)
            {
                ivhString = Math.Round(ivh).ToString("N0");
            }
            var text = $"<a href='/dashboard/instance/ivqueue/{Uri.EscapeDataString(Name)}'>Queue</a>: {_pokemonQueue.Count}, IV/h: {ivhString}";
            return await Task.FromResult(text).ConfigureAwait(false);
        }

        public void Reload()
        {
        }

        public void Stop()
        {
            _shouldExit = true;
            _timer.Stop();
        }

        public void AddPokemon(Pokemon pokemon)
        {
            if (!PokemonList.Contains(pokemon.PokemonId))
            {
                // Pokemon Id not in pokemon IV list
                return;
            }
            if (_pokemonQueue.Find(x => x.Id == pokemon.Id) != null)
            {
                // Queue already contains pokemon
                return;
            }
            if (pokemon.ExpireTimestamp <= DateTime.UtcNow.ToTotalSeconds())
            {
                // Pokemon already expired
                return;
            }
            // Check if Pokemon is within any of the instance area geofences
            if (!GeofenceService.InMultiPolygon(MultiPolygon, pokemon.Latitude, pokemon.Longitude))
                return;

            lock (_queueLock)
            {
                var index = LastIndexOf(pokemon.PokemonId);
                if (_pokemonQueue.Count >= IVQueueLimit && index == null)
                {
                    _logger.LogInformation($"[IVController] [{Name}] Queue is full!");
                }
                else if (_pokemonQueue.Count >= IVQueueLimit)
                {
                    if (index != null)
                    {
                        // Insert Pokemon at index
                        _pokemonQueue.Insert(index ?? 0, pokemon);
                        // Remove last pokemon from queue
                        _pokemonQueue.Remove(_pokemonQueue.LastOrDefault());
                    }
                }
                else if (index != null)
                {
                    _pokemonQueue.Insert(index ?? 0, pokemon);
                }
                else
                {
                    _pokemonQueue.Add(pokemon);
                }
            }
        }

        public void GotIV(Pokemon pokemon)
        {
            if (!GeofenceService.InMultiPolygon(MultiPolygon, pokemon.Latitude, pokemon.Longitude))
                return;

            lock (_queueLock)
            {
                var pkmn = _pokemonQueue.Find(x => x.Id == pokemon.Id);
                if (pkmn != null)
                {
                    _pokemonQueue.Remove(pkmn);
                }
                if (_startDate == default)
                {
                    _startDate = DateTime.UtcNow;
                }
                if (_count == int.MaxValue)
                {
                    _count = 0;
                    _startDate = DateTime.UtcNow;
                }
                else
                {
                    _count++;
                }
            }
        }

        public List<Pokemon> GetQueue()
        {
            lock (_queueLock)
            {
                return _pokemonQueue;
            }
        }

        #endregion

        #region Private Methods

        private int? LastIndexOf(uint pokemonId)
        {
            var targetPriority = PokemonList.IndexOf(pokemonId);
            for (var i = 0; i < _pokemonQueue.Count; i++)
            {
                var pokemon = _pokemonQueue[i];
                var priority = PokemonList.IndexOf(pokemon.PokemonId);
                if (targetPriority < priority)
                {
                    return i;
                }
            }
            return null;
        }

        private void LoopCache()
        {
            if (_shouldExit)
            {
                Stop();
                return;
            }
            /*
            lock (_queueLock)
            {
                for (var i = 0; i < _pokemonQueue.Count; i++)
                {
                    var pokemon = _pokemonQueue[i];
                    if (pokemon.ExpireTimestamp <= DateTime.UtcNow.ToTotalSeconds())
                    {
                        // Pokemon expired, remove from queue
                        _logger.LogDebug($"[{Name}] Pokemon {pokemon.Id} expired, removing from IV queue...");
                        _pokemonQueue.Remove(pokemon);
                    }
                }
            }
            */
            lock (_scannedLock)
            {
                if (_scannedPokemon.Count == 0)
                {
                    if (_shouldExit)
                        return;

                    return;
                }

                var first = _scannedPokemon.PopFirst(out _scannedPokemon);
                var timeSince = DateTime.UtcNow - first.Date;
                if (timeSince.TotalSeconds < 120)
                {
                    Thread.Sleep(Convert.ToInt32(120 - timeSince.TotalSeconds) * 1000);
                    if (_shouldExit)
                        return;
                }
                Pokemon pokemonReal = null;
                try
                {
                    pokemonReal = _pokemonRepository.GetByIdAsync(first.Pokemon.Id)
                                                    .ConfigureAwait(false)
                                                    .GetAwaiter()
                                                    .GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{Name}] Error: {ex}");
                    if (_shouldExit)
                        return;
                }
                if (pokemonReal != null)
                {
                    if (pokemonReal.AttackIV == null)
                    {
                        _logger.LogDebug($"[{Name}] Checked Pokemon {pokemonReal.Id} does not have IV");
                        AddPokemon(pokemonReal);
                    }
                    else
                    {
                        _logger.LogDebug($"[{Name}] Checked Pokemon {pokemonReal.Id} has IV");
                        //GotIV(pokemonReal);
                    }
                }
            }
        }

        #endregion
    }
}