namespace DataConsumer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    using POGOProtos.Rpc;
    using StackExchange.Redis;

    using Chuck.Infrastructure.Common;
    using Chuck.Infrastructure.Configuration;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Factories;
    using Chuck.Infrastructure.Data.Repositories;
    using Chuck.Infrastructure.Extensions;

    class DataConsumer
    {
        private const string RedisQueueName = "*"; // TODO: Eventually change from wildcard
        // TODO: REMOVE
        private const uint MaxConcurrency = 100;

        #region Variables

        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly IDatabaseAsync _redisDatabase;
        private readonly Config _config;

        // Global lists
        private readonly List<Pokemon> _pokemon;
        private readonly List<Weather> _weather;
        private readonly List<Gym> _gyms;
        private readonly List<Pokestop> _pokestops;
        private readonly List<GymDefender> _gymDefenders;
        private readonly List<Trainer> _gymTrainers;
        private readonly List<Pokestop> _quests;
        private readonly List<Spawnpoint> _spawnpoints;
        private readonly List<Cell> _cells;
        private readonly List<Account> _playerData;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell;
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell;

        private static readonly object _gymCellsLock = new object();
        private static readonly object _stopCellsLock = new object();

        // Object locks
        private static readonly object _pokemonLock = new object();
        //private static readonly object _fortsLock = new object();
        private static readonly object _gymsLock = new object();
        private static readonly object _pokestopsLock = new object();
        private static readonly object _gymDefendersLock = new object();
        private static readonly object _gymTrainersLock = new object();
        private static readonly object _questsLock = new object();
        private static readonly object _weatherLock = new object();
        private static readonly object _cellsLock = new object();
        private static readonly object _accountsLock = new object();
        private static readonly object _spawnpointsLock = new object();

        private bool _shouldExit = false;

        #endregion

        public ushort ConsumeIntervalS { get; set; }

        #region Constructor

        public DataConsumer(Config config)
        {
            _pokemon = new List<Pokemon>();
            _weather = new List<Weather>();
            _gyms = new List<Gym>();
            _pokestops = new List<Pokestop>();
            _gymDefenders = new List<GymDefender>();
            _gymTrainers = new List<Trainer>();
            _quests = new List<Pokestop>();
            _spawnpoints = new List<Spawnpoint>();
            _cells = new List<Cell>();
            _playerData = new List<Account>();

            _gymIdsPerCell = new Dictionary<ulong, List<string>>();
            _stopIdsPerCell = new Dictionary<ulong, List<string>>();

            _config = config;

            ConsumeIntervalS = 5;

            var options = new ConfigurationOptions
            {
                EndPoints =
                {
                    { $"{_config.Redis.Host}:{_config.Redis.Port}" }
                },
                Password = _config.Redis.Password,
            };
            _redis = ConnectionMultiplexer.Connect(options);
            _redis.ConnectionFailed += RedisOnConnectionFailed;
            _redis.ErrorMessage += RedisOnErrorMessage;
            _redis.InternalError += RedisOnInternalError;
            if (_redis.IsConnected)
            {
                _subscriber = _redis.GetSubscriber();
                _redisDatabase = _redis.GetDatabase(_config.Redis.DatabaseNum);
            }
        }

        #endregion

        #region Redis Events

        private void RedisOnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            ConsoleExt.WriteError($"[DataConsumer] [Redis] {e.EndPoint}: {e.FailureType} {e.Exception}");
        }

        private void RedisOnErrorMessage(object sender, RedisErrorEventArgs e)
        {
            ConsoleExt.WriteError($"[DataConsumer] [Redis] {e.EndPoint}: {e.Message}");
        }

        private void RedisOnInternalError(object sender, InternalErrorEventArgs e)
        {
            ConsoleExt.WriteError($"[DataConsumer] [Redis] {e.EndPoint}: {e.Exception}");
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!_shouldExit)
            {
                return;
            }
            _shouldExit = false;

            // Register to redis queue
            RegisterQueueSubscriber();
            // Start database ingester
            DataIngester();

            var consumerTimer = new System.Timers.Timer(5 * 1000);
            consumerTimer.Elapsed += (sender, e) => DataIngester();
            consumerTimer.Start();

            var timer = new System.Timers.Timer(10 * 1000);
            timer.Elapsed += async (sender, e) =>
            {
                var length = await _redisDatabase.ListLengthAsync(RedisQueueName);
                if (length > 1000)
                {
                    ConsoleExt.WriteWarn($"[DataConsumer] Queue is current {length}");
                }
            };
            timer.Start();
        }

        public void Stop()
        {
            _shouldExit = true;
        }

        #endregion

        private void RegisterQueueSubscriber()
        {
            _subscriber.Subscribe(RedisQueueName, async (channel, message) => await SubscriptionHandler(channel, message));
        }

        private async Task SubscriptionHandler(RedisChannel channel, RedisValue message)
        {
            if (string.IsNullOrEmpty(message)) return;
            //ConsoleExt.WriteInfo($"[DataConsumer] Received from {channel}");
            try
            {
                switch (channel)
                {
                    case RedisChannels.ProtoWildPokemon:
                        {
                            var data = JsonSerializer.Deserialize<PokemonFound<WildPokemonProto>>(message);
                            if (data == null) return;

                            var spawnpoint = Spawnpoint.FromPokemon(data.Pokemon);
                            _spawnpoints.Add(spawnpoint);
                            _cells.Add(Cell.FromId(data.CellId));

                            var cell = data.CellId;
                            var wildPokemon = data.Pokemon;
                            var timestampMs = data.TimestampMs;
                            var username = data.Username;
                            //var id = wildPokemon.EncounterId;
                            //var pokemon = new Pokemon(wildPokemon, cell, timestampMs, username, false); // TODO: IsEvent
                            var pokemon = Pokemon.ParseFromWild(wildPokemon, spawnpoint);
                            pokemon.Username = username;
                            // TODO: Add pokemon to redis cache, check by getting by id, maybe maintain the cache to reduce sql queries
                            // if (!redis.contains(pokemon)) redis.set($"pokemon_{pokemon.Id}")
                            /*
                            var oldPokemon = await GetCacheData<Pokemon>("pokemon_" + pokemon.Id).ConfigureAwait(false);
                            if (pokemon.Update(oldPokemon, true)) // TODO: Check HasChanges property
                            {
                                _pokemon.Add(pokemon);
                            }
                            */
                            _pokemon.Add(pokemon);
                            await SetCacheData($"pokemon_{pokemon.Id}", pokemon);
                            break;
                        }
                    case RedisChannels.ProtoNearbyPokemon:
                        {
                            var data = JsonSerializer.Deserialize<PokemonFound<NearbyPokemonProto>>(message);
                            if (data == null) return;

                            _cells.Add(Cell.FromId(data.CellId));

                            var cell = data.CellId;
                            // data.timestamp_ms
                            var nearbyPokemon = data.Pokemon;
                            var username = data.Username;
                            var pokemon = new Pokemon(nearbyPokemon, cell, username, false); // TODO: IsEvent
                            if (pokemon.Latitude == 0 && string.IsNullOrEmpty(pokemon.PokestopId))
                            {
                                // Skip nearby pokemon without pokestop id set and no coordinate
                                return;
                            }
                            var pokestop = await GetCacheData<Pokestop>("pokestop_" + pokemon.PokestopId).ConfigureAwait(false);
                            if (pokestop == null)
                            {
                                // Unknown stop, skip pokemon
                                return;
                            }
                            pokemon.Latitude = pokestop.Latitude;
                            pokemon.Longitude = pokestop.Longitude;
                            var oldPokemon = await GetCacheData<Pokemon>("pokemon_" + pokemon.Id).ConfigureAwait(false);
                            if (pokemon.Update(oldPokemon)) // TODO: Check HasChanges property
                            {
                                _pokemon.Add(pokemon);
                            }
                            //_pokemon.Add(pokemon);
                            await SetCacheData($"pokemon_{pokemon.Id}", pokemon);
                            break;
                        }
                    case RedisChannels.ProtoEncounter:
                        {
                            var data = JsonSerializer.Deserialize<PokemonFound<EncounterOutProto>>(message);
                            if (data == null) return;

                            var encounter = data.Pokemon;
                            var username = data.Username;
                            var spawnpoint = Spawnpoint.FromPokemon(encounter.Pokemon);
                            _spawnpoints.Add(spawnpoint);
                            Pokemon pokemon;
                            _cells.Add(Cell.FromId((ulong)encounter.Pokemon.Pokemon.CapturedS2CellId));
                            try
                            {
                                //pokemon = await GetCacheData<Pokemon>("pokemon_" + encounter.Pokemon.EncounterId.ToString()).ConfigureAwait(false); // TODO: is_event
                                pokemon = Pokemon.ParseFromEncounter(encounter.Pokemon, spawnpoint);
                                pokemon.Username = username;
                            }
                            catch (Exception ex)
                            {
                                ConsoleExt.WriteError($"Error: {ex}");
                                pokemon = null;
                            }
                            if (pokemon != null)
                            {
                                // TODO: Pokemon from encounter
                                //await pokemon.AddEncounter(encounter, username).ConfigureAwait(false);

                            }
                            else
                            {
                                //var centerCoord = new Coordinate(encounter.Pokemon.Latitude, encounter.Pokemon.Longitude);
                                //var cellId = S2CellId.FromLatLng(S2LatLng.FromDegrees(centerCoord.Latitude, centerCoord.Longitude));
                                //var timestampMs = DateTime.UtcNow.ToTotalSeconds() * 1000;
                                pokemon = Pokemon.ParseFromEncounter(encounter.Pokemon, spawnpoint);
                                /*
                                var newPokemon = new Pokemon(encounter.Pokemon, cellId.Id, timestampMs, username, false); // TODO: IsEvent
                                newPokemon.AddEncounter(encounter, username)
                                          .ConfigureAwait(false)
                                          .GetAwaiter()
                                          .GetResult();
                                */
                                //if (pokemon.Update(null, true))
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
                            //if (pokemon.Update(pokemon, true))
                            lock (_pokemonLock)
                            {
                                _pokemon.Add(pokemon);
                            }
                            await SetCacheData($"pokemon_{pokemon.Id}", pokemon);
                            break;
                        }
                    case RedisChannels.ProtoFort:
                        {
                            var data = JsonSerializer.Deserialize<FortFound>(message);
                            if (data == null) return;

                            var cellId = data.CellId;
                            var fort = data.Fort;
                            switch (fort.FortType)
                            {
                                case FortType.Gym:
                                    var oldGym = await GetCacheData<Gym>($"gym_{fort.FortId}");//gymRepository.GetByIdAsync(fort.FortId).ConfigureAwait(false).GetAwaiter().GetResult();
                                    var gym = Gym.FromProto(cellId, fort);// new Gym(cellId, fort);
                                    if (gym.Update(oldGym)) // TODO: Check HasChanges property
                                    {
                                        _gyms.Add(gym);
                                    }
                                    lock (_gymCellsLock)
                                    {
                                        if (!_gymIdsPerCell.ContainsKey(cellId))
                                        {
                                            _gymIdsPerCell.Add(cellId, new List<string> { fort.FortId });
                                        }
                                        else
                                        {
                                            _gymIdsPerCell[cellId].Add(fort.FortId);
                                        }
                                    }
                                    await SetCacheData($"gym_{gym.Id}", gym);
                                    break;
                                case FortType.Checkpoint:
                                    var oldPokestop = await GetCacheData<Pokestop>($"pokestop_{fort.FortId}");//pokestopRepository.GetByIdAsync(fort.FortId).ConfigureAwait(false).GetAwaiter().GetResult();
                                    var pokestop = Pokestop.FromProto(cellId, fort);//new Pokestop(cellId, fort);
                                    if (pokestop.Update(oldPokestop)) // TODO: Check HasChanges property
                                    {
                                        _pokestops.Add(pokestop);
                                    }
                                    lock (_stopCellsLock)
                                    {
                                        if (!_stopIdsPerCell.ContainsKey(cellId))
                                        {
                                            _stopIdsPerCell.Add(cellId, new List<string> { fort.FortId });
                                        }
                                        else
                                        {
                                            _stopIdsPerCell[cellId].Add(fort.FortId);
                                        }
                                    }
                                    await SetCacheData($"pokestop_{pokestop.Id}", pokestop);
                                    break;
                            }
                            break;
                        }
                    case RedisChannels.ProtoGymDefender:
                        break;
                    case RedisChannels.ProtoGymTrainer:
                        break;
                    case RedisChannels.ProtoQuest:
                        break;
                    case RedisChannels.ProtoCell:
                        {
                            var cellId = Convert.ToUInt64(message);
                            if (cellId == 0) return;

                            lock (_cellsLock)
                            {
                                _cells.Add(Cell.FromId(cellId));
                            }
                            break;
                        }
                    case RedisChannels.ProtoWeather:
                        {
                            var weather = JsonSerializer.Deserialize<ClientWeatherProto>(message);
                            if (weather == null) return;

                            lock (_weatherLock)
                            {
                                _weather.Add(Weather.FromProto(weather));
                            }
                            break;
                        }
                    case RedisChannels.ProtoAccount:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private Spawnpoint HandleSpawnpointData(Spawnpoint spawnpoint)
        {
            if (spawnpoint.Id == 0)
            {
                // Add to spawnpoint cache
                // Add to spawnpoint list
                return spawnpoint; //GetSpawnById(spawnpoint.Id);
            }
            else
            {
                // Check if spawnpoint changed from old/new
                // If so, set new to old despawn_sec
                // Save to redis
            }
            return spawnpoint;
        }

        #region Database Ingesters

        private void DataIngester()
        {
            //ThreadPool.QueueUserWorkItem(x =>
            //new Thread(x =>
            //{
                //while (!_shouldExit) // TODO: Use some type of exit condition
                //{
                    if (_cells.Count > 0)
                    {
                        BenchmarkMethod(() => UpdateCells(), "S2Cells");
                    }
                    Thread.Sleep(5);
                    if (_weather.Count > 0)
                    {
                        BenchmarkMethod(() => UpdateWeather(), "Weather");
                    }
                    Thread.Sleep(5);
                    if (_gyms.Count > 0)
                    {
                        BenchmarkMethod(() => UpdateGyms(), "Gyms");
                    }
                    Thread.Sleep(5);
                    if (_pokestops.Count > 0)
                    {
                        BenchmarkMethod(() => UpdatePokestops(), "Pokestops");
                    }
                    Thread.Sleep(5);
                    var updatePokemon = true;
                    if (_spawnpoints.Count > 0)
                    {
                        try
                        {
                            BenchmarkMethod(() => UpdateSpawnpoints(), "Spawnpoints");
                        }
                        catch (Exception ex)
                        {
                            updatePokemon = false;
                            ConsoleExt.WriteError($"Failed to update spawnpoints, skipping Pokemon: {ex}");
                        }
                    }
                    Thread.Sleep(5);
                    if (_pokemon.Count > 0)
                    {
                        if (updatePokemon)
                        {
                            BenchmarkMethod(() => UpdatePokemon(), "Pokemon");
                        }
                    }
                    // Gyms? GymInfo?
                    // Accounts

                    // Consume data every x seconds
                    Thread.Sleep(ConsumeIntervalS * 1000);
                //}
            //}) { IsBackground = true }.Start();
        }

        private int UpdateSpawnpoints()
        {
            var spawnpointsCount = 0;
            if (_spawnpoints.Count == 0)
                return spawnpointsCount;

            lock (_spawnpointsLock)
            {
                var spawnpoints = _spawnpoints;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {spawnpoints.Count:N0} Spawnpoints");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var spawnpointRepository = new SpawnpointRepository(ctx);
                    spawnpointRepository.AddOrUpdateAsync(spawnpoints)
                                        .ConfigureAwait(false)
                                        .GetAwaiter()
                                        .GetResult();
                    spawnpointsCount = spawnpoints.Count;
                }
                _spawnpoints.Clear();
            }
            return spawnpointsCount;
        }

        private int UpdateCells()
        {
            var cellsCount = 0;
            if (_cells.Count == 0)
                return cellsCount;

            lock (_cellsLock)
            {
                //var cells = _cells.GetRange(0, _cells.Count);
                //var count = (int)Math.Min(MaxConcurrency, cells.Count);
                //_cells.RemoveRange(0, count);
                var cells = _cells;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {cells.Count:N0} S2Cells");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var cellRepository = new CellRepository(ctx);
                    cellRepository.AddOrUpdateAsync(cells)
                                  .ConfigureAwait(false)
                                  .GetAwaiter()
                                  .GetResult();
                    cellsCount = cells.Count;
                }
                _cells.Clear();

            }
            return cellsCount;
        }

        private int UpdateWeather()
        {
            var weatherCount = 0;
            if (_weather.Count == 0)
                return weatherCount;

            lock (_weatherLock)
            {
                var weather = _weather;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {weather.Count:N0} Weather");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var weatherRepository = new WeatherRepository(ctx);
                    weatherRepository.AddOrUpdateAsync(weather)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    weatherCount = weather.Count;
                }
                _weather.Clear();
            }
            return weatherCount;
        }

        private int UpdateGyms()
        {
            var gymCount = 0;
            if (_gyms.Count == 0)
                return gymCount;

            lock (_gymsLock)
            {
                var gyms = _gyms;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {gyms.Count:N0} Gyms");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var gymRepository = new GymRepository(ctx);
                    gymRepository.AddOrUpdateAsync(gyms)
                                 .ConfigureAwait(false)
                                 .GetAwaiter()
                                 .GetResult();
                    gymCount = gyms.Count;
                }
                _gyms.Clear();
            }
            return gymCount;
        }

        private int UpdatePokestops()
        {
            var pokestopCount = 0;
            if (_pokestops.Count == 0)
                return pokestopCount;

            lock (_pokestopsLock)
            {
                var pokestops = _pokestops;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {pokestops.Count:N0} Pokestops");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var pokestopRepository = new PokestopRepository(ctx);
                    pokestopRepository.AddOrUpdateAsync(pokestops)
                                      .ConfigureAwait(false)
                                      .GetAwaiter()
                                      .GetResult();
                    pokestopCount = pokestops.Count;
                }
                _pokestops.Clear();
            }
            return pokestopCount;
        }

        private int UpdatePokemon()
        {
            var pokemonCount = 0;
            if (_pokemon.Count == 0)
                return pokemonCount;

            lock (_pokemonLock)
            {
                var pokemon = _pokemon;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {pokemon.Count:N0} Pokemon");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var pokemonRepository = new PokemonRepository(ctx);
                    pokemonRepository.AddOrUpdateAsync(pokemon)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    pokemonCount = pokemon.Count;
                }
                _pokemon.Clear();
            }
            return pokemonCount;
        }

        #endregion

        #region Cache Helpers

        private async Task<T> GetCacheData<T>(string key)
        {
            var data = await _redisDatabase.StringGetAsync(key, CommandFlags.PreferMaster).ConfigureAwait(false);
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }
            return data.ToString().FromJson<T>();
        }

        private async Task SetCacheData<T>(string key, T data, TimeSpan expires = default)
        {
            await _redisDatabase.StringSetAsync(key, data.ToJson(), expires, When.Always, CommandFlags.FireAndForget).ConfigureAwait(false);
        }

        #endregion

        private void BenchmarkMethod(Func<int> method, string type)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var count = method();
            stopwatch.Stop();
            if (count > 0)
            {
                ConsoleExt.WriteInfo($"[DataConsumer] {type} Count: {count:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private Task PublishData<T>(string channel, T data)
        {
            try
            {
                if (data == null)
                {
                    return Task.CompletedTask;
                }
                _subscriber.PublishAsync(channel, data.ToJson(), CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex);
            }
            return Task.CompletedTask;
        }
    }

    public class PokemonFound<T>
    {
        [JsonPropertyName("cell")]
        public ulong CellId { get; set; }

        [JsonPropertyName("data")]
        public T Pokemon { get; set; }

        [JsonPropertyName("timestamp_ms")]
        public ulong TimestampMs { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class FortFound
    {
        [JsonPropertyName("cell")]
        public ulong CellId { get; set; }

        [JsonPropertyName("data")]
        public PokemonFortProto Fort { get; set; }
    }
}