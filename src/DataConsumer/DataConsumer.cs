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

    // TODO: Load all gyms/stops/cells/spawnpoints/pokemon in mysql to redis
    // TODO: If entity not in redis, check mysql
    // TODO: Flush redis database of expired Pokemon
    // TODO: Change table `pokemon`.`id` type to ulong instead of string

    class DataConsumer
    {
        private const string RedisQueueName = "*"; // TODO: Eventually change from wildcard
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
                if (!_redis.IsConnected)
                {
                    ConsoleExt.WriteWarn($"[DataConsumer] Not connected to redis server");
                    return;
                }
                var length = await _redisDatabase.ListLengthAsync(RedisQueueName);
                if (length > 1000)
                {
                    ConsoleExt.WriteWarn($"[DataConsumer] Queue is current {length}");
                }
            };
            timer.Start();

            ConsoleExt.WriteInfo($"[DataConsumer] Started");
        }

        public void Stop()
        {
            _shouldExit = true;
            ConsoleExt.WriteInfo($"[DataConsumer] Started");
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
                            lock (_spawnpointsLock)
                            {
                                _spawnpoints.Add(spawnpoint);
                            }
                            lock (_cellsLock)
                            {
                                _cells.Add(Cell.FromId(data.CellId));
                            }

                            var cell = data.CellId;
                            var wildPokemon = data.Pokemon;
                            var timestampMs = data.TimestampMs;
                            var username = data.Username;
                            //var id = wildPokemon.EncounterId;
                            //var pokemon = new Pokemon(wildPokemon, cell, timestampMs, username, false); // TODO: IsEvent
                            var pokemon = Pokemon.ParseFromWild(wildPokemon, spawnpoint);
                            pokemon.Username = username;
                            var oldPokemon = await GetEntity<Pokemon>(pokemon.Id, "pokemon").ConfigureAwait(false);
                            if (pokemon.Update(oldPokemon, true)) // TODO: Check HasChanges property
                            {
                                // TODO: Webhook
                                // TODO: await PublishData(RedisChannels.WebhookPokemon, pokemon);
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
                            await SetCacheData($"pokemon_{pokemon.Id}", pokemon);
                            break;
                        }
                    case RedisChannels.ProtoNearbyPokemon:
                        {
                            var data = JsonSerializer.Deserialize<PokemonFound<NearbyPokemonProto>>(message);
                            if (data == null) return;

                            lock (_cellsLock)
                            {
                                _cells.Add(Cell.FromId(data.CellId));
                            }

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
                            var pokestop = await GetEntity<Pokestop>(pokemon.PokestopId, "pokestop").ConfigureAwait(false);
                            if (pokestop == null)
                            {
                                // Unknown stop, skip pokemon
                                return;
                            }
                            pokemon.Latitude = pokestop.Latitude;
                            pokemon.Longitude = pokestop.Longitude;
                            var oldPokemon = await GetEntity<Pokemon>(pokemon.Id, "pokemon").ConfigureAwait(false);
                            if (pokemon.Update(oldPokemon)) // TODO: Check HasChanges property
                            {
                                // TODO: Webhook
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
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
                            lock (_spawnpointsLock)
                            {
                                _spawnpoints.Add(spawnpoint);
                            }
                            Pokemon pokemon;
                            lock (_cellsLock)
                            {
                                _cells.Add(Cell.FromId((ulong)encounter.Pokemon.Pokemon.CapturedS2CellId));
                            }
                            try
                            {
                                var oldPokemon = await GetEntity<Pokemon>(encounter.Pokemon.EncounterId.ToString(), "pokemon").ConfigureAwait(false); // TODO: is_event
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
                                //pokemon = Pokemon.ParseFromEncounter(encounter.Pokemon, spawnpoint);
                                /*
                                var newPokemon = new Pokemon(encounter.Pokemon, cellId.Id, timestampMs, username, false); // TODO: IsEvent
                                newPokemon.AddEncounter(encounter, username)
                                          .ConfigureAwait(false)
                                          .GetAwaiter()
                                          .GetResult();
                                */
                                //if (pokemon.Update(null, true))
                                //{
                                //    _pokemon.Add(pokemon);
                                //}
                            }
                            //if (pokemon.Update(pokemon, true))
                            lock (_pokemonLock)
                            {
                                // TODO: Webhook
                                // TODO: await PublishData(RedisChannels.WebhookPokemon, pokemon);
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
                                        // TODO: Webhook
                                        lock (_gymsLock)
                                        {
                                            _gyms.Add(gym);
                                        }
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
                                    var oldPokestop = await GetEntity<Pokestop>(fort.FortId, "pokestop");
                                    var pokestop = Pokestop.FromProto(cellId, fort);//new Pokestop(cellId, fort);
                                    if (pokestop.Update(oldPokestop)) // TODO: Check HasChanges property
                                    {
                                        // TODO: Webhook
                                        lock (_pokestopsLock)
                                        {
                                            _pokestops.Add(pokestop);
                                        }
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
                        {
                            var quest = JsonSerializer.Deserialize<QuestFound>(message);
                            if (quest == null) return;
                            var fs = FortSearchOutProto.Parser.ParseFrom(Convert.FromBase64String(quest.Raw));
                            if (fs == null) return;

                            // Get existing pokestop, and add quest to it
                            var pokestop = await GetEntity<Pokestop>(fs.FortId, "pokestop");
                            // Skip quests we don't have stops for yet
                            if (pokestop == null)
                                return;
                            /*
                            if (await pokestop.TriggerWebhook(true))
                            {
                                _logger.LogDebug($"[Quest] Found a quest belonging to a new stop, skipping..."); // :face_with_raised_eyebrow:
                                continue;
                            }
                            */
                            pokestop.AddQuest(fs.ChallengeQuest.Quest);
                            if (pokestop.Update(pokestop, true)) // TODO: Check HasChanges property
                            {
                                // TODO: Webhook
                                _quests.Add(pokestop);
                            }
                            await SetCacheData($"pokestop_{pokestop.Id}", pokestop);
                            break;
                        }
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
                        {
                            var now = DateTime.UtcNow.ToTotalSeconds();
                            var playerData = JsonSerializer.Deserialize<AccountFound>(message);
                            if (playerData == null) return;
                            // Get account
                            var account = await GetEntity<Account>(playerData.Username, "account").ConfigureAwait(false);
                            // Skip account if we failed to get it
                            if (account == null)
                                return;

                            account.CreationTimestamp = (ulong)playerData.Player.Player.CreationTimeMs / 1000;
                            account.Warn = playerData.Player.Warn;
                            var warnExpireTimestamp = (ulong)playerData.Player.WarnExpireMs / 1000;
                            if (warnExpireTimestamp > 0)
                            {
                                account.WarnExpireTimestamp = warnExpireTimestamp;
                            }
                            account.WarnMessageAcknowledged = playerData.Player.WarnMessageAcknowledged;
                            account.SuspendedMessageAcknowledged = playerData.Player.SuspendedMessageAcknowledged;
                            account.WasSuspended = playerData.Player.WasSuspended;
                            account.Banned = playerData.Player.Banned;
                            if (playerData.Player.Warn && string.IsNullOrEmpty(account.Failed))
                            {
                                account.Failed = "GPR_RED_WARNING";
                                if (account.FirstWarningTimestamp == null)
                                {
                                    account.FirstWarningTimestamp = now;
                                }
                                account.FailedTimestamp = now;
                                ConsoleExt.WriteWarn($"[ConsumerService] Account {account.Username}|{playerData.Player.Player.Name} - Red Warning: {playerData.Player.Banned}");
                            }
                            if (playerData.Player.Banned)
                            {
                                account.Failed = "GPR_BANNED";
                                account.FailedTimestamp = now;
                                ConsoleExt.WriteWarn($"[ConsumerService] Account {account.Username}|{playerData.Player.Player.Name} - Banned: {playerData.Player.Banned}");
                            }
                            _playerData.Add(account);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"[DataConsumer] SubscriptionHandler: {ex.Message}");
            }
        }

        #region Database Ingesters

        private void DataIngester()
        {
            //ThreadPool.QueueUserWorkItem(x =>
            //new Thread(x =>
            //{
            //while (!_shouldExit) // TODO: Use some type of exit condition
            //{
            //BenchmarkMethod(() => UpdateCells(), "S2Cells");
            UpdateCells();
            var updatePokemon = true;
            try
            {
                //BenchmarkMethod(() => UpdateSpawnpoints(), "Spawnpoints");
                UpdateSpawnpoints();
            }
            catch (Exception ex)
            {
                updatePokemon = false;
                ConsoleExt.WriteError($"Failed to update spawnpoints, skipping Pokemon: {ex}");
            }
            //BenchmarkMethod(() => UpdateWeather(), "Weather");
            //BenchmarkMethod(() => UpdatePokestops(), "Pokestops");
            UpdateWeather();
            UpdatePokestops();
            if (updatePokemon)
            {
                //BenchmarkMethod(() => UpdatePokemon(), "Pokemon");
                UpdatePokemon();
            }
            UpdateQuests();
            //BenchmarkMethod(() => UpdateGyms(), "Gyms");
            UpdateGyms();
            // TODO: UpdateGymInfo
            // TODO: UpdateGymDefenders
            // TODO: UpdateGymTrainers
            UpdateAccounts();

            // Consume data every x seconds
            Thread.Sleep(ConsumeIntervalS * 1000);
            //}
            //}) { IsBackground = true }.Start();
        }

        private int UpdateCells()
        {
            var cellsCount = 0;
            if (_cells.Count == 0)
                return cellsCount;

            //var cells = _cells.GetRange(0, _cells.Count);
            //var count = (int)Math.Min(MaxConcurrency, cells.Count);
            //_cells.RemoveRange(0, count);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //List<Cell> cells;
            //lock (_cellsLock)
            //{
            //    cells = _cells;
            //    cellsCount = cells.Count;
            //}
            lock (_cellsLock)
            {
                var cells = _cells;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {cells.Count:N0} S2Cells");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var cellRepository = new CellRepository(ctx);
                    cellRepository.InsertOrUpdate(cells)
                                  .ConfigureAwait(false)
                                  .GetAwaiter()
                                  .GetResult();
                    cellsCount = cells.Count;
                }
                _cells.Clear();
            }
            stopwatch.Stop();
            ConsoleExt.WriteInfo($"[DataConsumer] S2Cells Count: {cellsCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            return cellsCount;
        }

        private int UpdateSpawnpoints()
        {
            lock (_spawnpointsLock)
            {
                var spawnpointsCount = 0;
                if (_spawnpoints.Count == 0)
                    return spawnpointsCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var spawnpoints = _spawnpoints;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {spawnpoints.Count:N0} Spawnpoints");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var spawnpointRepository = new SpawnpointRepository(ctx);
                    //while (spawnpoints.Count > 0)
                    //{
                    //var spawnpoint = spawnpoints.Dequeue();
                    //if (spawn)
                    //}
                    spawnpointRepository.InsertOrUpdate(spawnpoints)
                                        .ConfigureAwait(false)
                                        .GetAwaiter()
                                        .GetResult();
                    /*
                    spawnpointRepository.AddOrUpdateAsync(spawnpoints)
                                        .ConfigureAwait(false)
                                        .GetAwaiter()
                                        .GetResult();
                    */
                    spawnpointsCount = spawnpoints.Count;
                }
                _spawnpoints.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Spawnpoints Count: {spawnpointsCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return spawnpointsCount;
            }
        }

        private int UpdateWeather()
        {
            lock (_weatherLock)
            {
                var weatherCount = 0;
                if (_weather.Count == 0)
                    return weatherCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var weather = _weather;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {weather.Count:N0} Weather");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var weatherRepository = new WeatherRepository(ctx);
                    weatherRepository.InsertOrUpdate(weather)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    /*
                    weatherRepository.AddOrUpdateAsync(weather)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    */
                    weatherCount = weather.Count;
                }
                _weather.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Weather Count: {weatherCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return weatherCount;
            }
        }

        private int UpdateGyms()
        {
            lock (_gymsLock)
            {
                var gymCount = 0;
                if (_gyms.Count == 0)
                    return gymCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var gyms = _gyms;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {gyms.Count:N0} Gyms");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var gymRepository = new GymRepository(ctx);
                    try
                    {
                        gymRepository.InsertOrUpdate(gyms)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateGyms: {ex.Message}");
                    }
                    /*
                    gymRepository.AddOrUpdateAsync(gyms)
                                 .ConfigureAwait(false)
                                 .GetAwaiter()
                                 .GetResult();
                    */
                    gymCount = gyms.Count;
                }
                _gyms.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Gyms Count: {gymCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return gymCount;
            }
        }

        private int UpdatePokestops()
        {
            lock (_pokestopsLock)
            {
                var pokestopCount = 0;
                if (_pokestops.Count == 0)
                    return pokestopCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var pokestops = _pokestops;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {pokestops.Count:N0} Pokestops");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var pokestopRepository = new PokestopRepository(ctx);
                    try
                    {
                        pokestopRepository.InsertOrUpdate(pokestops)
                                          .ConfigureAwait(false)
                                          .GetAwaiter()
                                          .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdatePokestops: {ex.Message}");
                    }
                    /*
                    pokestopRepository.AddOrUpdateAsync(pokestops)
                                      .ConfigureAwait(false)
                                      .GetAwaiter()
                                      .GetResult();
                    */
                    pokestopCount = pokestops.Count;
                }
                _pokestops.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Pokestops Count: {pokestopCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return pokestopCount;
            }
        }

        private int UpdatePokemon()
        {
            lock (_pokemonLock)
            {
                var pokemonCount = 0;
                if (_pokemon.Count == 0)
                    return pokemonCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var pokemon = _pokemon;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {pokemon.Count:N0} Pokemon");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var pokemonRepository = new PokemonRepository(ctx);
                    try
                    {
                        pokemonRepository.InsertOrUpdate(pokemon)
                                         .ConfigureAwait(false)
                                         .GetAwaiter()
                                         .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdatePokemon: {ex.Message}");
                    }
                    /*
                    foreach (var pkmn in _pokemon)
                    {
                        try
                        {
                            pokemonRepository.InsertOrUpdate(pkmn)
                                             .ConfigureAwait(false)
                                             .GetAwaiter()
                                             .GetResult();
                        }
                        catch (MySqlConnector.MySqlException ex)
                        {
                            ConsoleExt.WriteError($"[DataConsumer] UpdatePokemon: {ex.Message}");
                        }
                    }
                    */
                    /*
                    pokemonRepository.AddOrUpdateAsync(pokemon)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    */
                    pokemonCount = pokemon.Count;
                }
                _pokemon.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Pokemon Count: {pokemonCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return pokemonCount;
            }
        }

        private int UpdateQuests()
        {
            lock (_questsLock)
            {
                var questCount = 0;
                if (_quests.Count == 0)
                    return questCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var quests = _quests;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {quests.Count:N0} Quests");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var pokestopRepository = new PokestopRepository(ctx);
                    try
                    {
                        pokestopRepository.InsertOrUpdate(quests)
                        //pokestopRepository.AddOrUpdateAsync(quests, false)
                                          .ConfigureAwait(false)
                                          .GetAwaiter()
                                          .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateQuests: {ex.Message}");
                    }
                    questCount = quests.Count;
                }
                _quests.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Quests Count: {questCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return questCount;
            }
        }

        private int UpdateAccounts()
        {
            lock (_accountsLock)
            {
                var accountCount = 0;
                if (_playerData.Count == 0)
                    return accountCount;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var accounts = _playerData;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {accounts.Count:N0} Account Data");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    var accountRepository = new AccountRepository(ctx);
                    try
                    {
                        accountRepository.InsertOrUpdate(accounts)
                                         .ConfigureAwait(false)
                                         .GetAwaiter()
                                         .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateAccounts: {ex.Message}");
                    }
                    accountCount = accounts.Count;
                }
                _playerData.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Account Count: {accountCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
                return accountCount;
            }
        }

        #endregion

        private async Task<T> GetEntity<T>(string id, string prefix) where T : BaseEntity
        {
            var oldEntity = await GetCacheData<T>(prefix + "_" + id).ConfigureAwait(false);
            if (oldEntity == null)
            {
                // Entity does not exist in redis database cache, get from sql database
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Database.ToString()))
                {
                    oldEntity = await ctx.Set<T>().FindAsync(new object[] { id }).ConfigureAwait(false);
                }
            }
            return oldEntity;
        }

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
            if (expires == default)
            {
                await _redisDatabase.StringSetAsync(key, data.ToJson(), null, When.Always, CommandFlags.FireAndForget).ConfigureAwait(false);
            }
            else
            {
                await _redisDatabase.StringSetAsync(key, data.ToJson(), expires, When.Always, CommandFlags.FireAndForget).ConfigureAwait(false);
            }
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

    public class QuestFound
    {
        [JsonPropertyName("raw")]
        public string Raw { get; set; }
    }

    public class AccountFound
    {
        [JsonPropertyName("gpr")]
        public GetPlayerOutProto Player { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }
}