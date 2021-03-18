namespace DataConsumer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Google.Common.Geometry;
    using POGOProtos.Rpc;
    using StackExchange.Redis;

    using Chuck.Common;
    using Chuck.Configuration;
    using Chuck.Data.Entities;
    using Chuck.Data.Factories;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using Chuck.Pvp;

    using Models;

    // TODO: Load all gyms/stops/cells/spawnpoints from mysql to redis
    // TODO: Flush redis database of expired Pokemon

    class DataConsumer
    {
        //private const uint MaxConcurrency = 100;

        #region Variables

        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly IDatabaseAsync _redisDatabase;
        private readonly Config _config;
        private readonly PvpRankCalculator _pvpCalculator;

        // Global lists
        private readonly List<Pokemon> _pokemon;
        private readonly List<Weather> _weather;
        private readonly List<Gym> _gyms;
        private readonly List<Pokestop> _pokestops;
        private readonly List<Gym> _gymInfo;
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
        private static readonly object _gymsLock = new object();
        private static readonly object _pokestopsLock = new object();
        private static readonly object _gymInfoLock = new object();
        private static readonly object _gymDefendersLock = new object();
        private static readonly object _gymTrainersLock = new object();
        private static readonly object _questsLock = new object();
        private static readonly object _weatherLock = new object();
        private static readonly object _cellsLock = new object();
        private static readonly object _accountsLock = new object();
        private static readonly object _spawnpointsLock = new object();

        private bool _shouldExit = false;

        #endregion

        #region Properties

        public ushort ConsumeIntervalS { get; set; }

        #endregion

        #region Constructor

        public DataConsumer(Config config)
        {
            _pokemon = new List<Pokemon>();
            _weather = new List<Weather>();
            _gyms = new List<Gym>();
            _pokestops = new List<Pokestop>();
            _gymInfo = new List<Gym>();
            _gymDefenders = new List<GymDefender>();
            _gymTrainers = new List<Trainer>();
            _quests = new List<Pokestop>();
            _spawnpoints = new List<Spawnpoint>();
            _cells = new List<Cell>();
            _playerData = new List<Account>();

            _gymIdsPerCell = new Dictionary<ulong, List<string>>();
            _stopIdsPerCell = new Dictionary<ulong, List<string>>();

            _config = config;
            _pvpCalculator = new PvpRankCalculator();

            ConsumeIntervalS = 5;

            var options = new ConfigurationOptions
            {
                EndPoints =
                {
                    { $"{_config.Root["Redis:Host"]}:{_config.Root["Redis:Port"]}" }
                },
                Password = _config.Root["Redis:Password"],
            };
            _redis = ConnectionMultiplexer.Connect(options);
            _redis.ConnectionFailed += RedisOnConnectionFailed;
            _redis.ErrorMessage += RedisOnErrorMessage;
            _redis.InternalError += RedisOnInternalError;
            if (_redis.IsConnected)
            {
                _subscriber = _redis.GetSubscriber();
                _redisDatabase = _redis.GetDatabase(int.Parse(_config.Root["Redis:DatabaseNum"]));
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
            StartQueueListener();
            // Start database ingester
            DataIngester();

            //var consumerTimer = new System.Timers.Timer(5 * 1000);
            //consumerTimer.Elapsed += (sender, e) => DataIngester();
            //consumerTimer.Start();

            var timer = new System.Timers.Timer(10 * 1000);
            timer.Elapsed += async (sender, e) =>
            {
                if (!_redis.IsConnected)
                {
                    ConsoleExt.WriteWarn($"[DataConsumer] Not connected to redis server");
                    return;
                }
                try
                {
                    var length = await _redisDatabase.ListLengthAsync(_config.Root["Redis:QueueName"]);
                    if (length > 1000)
                    {
                        ConsoleExt.WriteWarn($"[DataConsumer] Queue is current {length}");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleExt.WriteError(ex);
                }
            };
            timer.Start();

            ConsoleExt.WriteInfo($"[DataConsumer] Started");

            // Publish available webhooks to redis event
            try
            {
                using var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]);
                PublishData(RedisChannels.WebhookReload, ctx.Webhooks.ToList());
            }
            catch (MySqlConnector.MySqlException ex)
            {
                ConsoleExt.WriteError($"[DataConsumer] UpdateSpawnpoints: {ex.Message}");
            }
        }

        public void Stop()
        {
            _shouldExit = true;
            ConsoleExt.WriteInfo($"[DataConsumer] Started");
        }

        #endregion

        #region Queue Handler

        private void StartQueueListener()
        {
            new Thread(async () =>
            {
                while (!_shouldExit)
                {
                    var data = await GetData(_config.Root["Redis:QueueName"]);
                    if (data == default)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    var obj = JsonSerializer.Deserialize<dynamic>(data.ToString());
                    string channel = Convert.ToString(obj.GetProperty("channel"));
                    string message = Convert.ToString(obj.GetProperty("data"));
                    await SubscriptionHandler(channel, message);
                    Thread.Sleep(10);
                }
            })
            { IsBackground = true }.Start();
            //_subscriber.Subscribe(RedisQueueName, async (channel, message) => await SubscriptionHandler(channel, message));
        }

        private async Task SubscriptionHandler(RedisChannel channel, RedisValue message)
        {
            if (string.IsNullOrEmpty(message)) return;

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

                            var cell = data.CellId;
                            lock (_cellsLock)
                            {
                                _cells.Add(new Cell(cell));
                            }
                            var wildPokemon = data.Pokemon;
                            var timestampMs = data.TimestampMs;
                            var username = data.Username;
                            //var id = wildPokemon.EncounterId;
                            var pokemon = new Pokemon(wildPokemon, cell, timestampMs, username, false); // TODO: IsEvent
                            var oldPokemon = await GetEntity<Pokemon>(pokemon.Id, "pokemon").ConfigureAwait(false);
                            var changesResult = pokemon.Update(oldPokemon);
                            SetPvpRanks(pokemon);
                            if (changesResult.IsNewOrHasChanges)
                            {
                                // Send pokemon_added event for InstanceController.Instance.GotPokemon();
                                await PublishData(RedisChannels.PokemonAdded, pokemon);
                                await PublishData(RedisChannels.WebhookPokemon, pokemon.GetWebhookValues("pokemon"));
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
                            if (changesResult.GotIV)
                            {
                                // Send pokemon_updated event for InstanceController.Instance.GotIV();
                                await PublishData(RedisChannels.PokemonUpdated, pokemon);
                                await PublishData(RedisChannels.WebhookPokemon, pokemon.GetWebhookValues("pokemon"));
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
                            /*
                            if (pokemon.Update(oldPokemon, true)) // TODO: Check HasChanges property
                            {
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
                            */
                            await SetCacheData($"pokemon_{pokemon.Id}", pokemon);
                            break;
                        }
                    case RedisChannels.ProtoNearbyPokemon:
                        {
                            var data = JsonSerializer.Deserialize<PokemonFound<NearbyPokemonProto>>(message);
                            if (data == null) return;

                            /*
                            lock (_cellsLock)
                            {
                                _cells.Add(Cell.FromId(data.CellId));
                            }
                            */

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
                            var changesResult = pokemon.Update(oldPokemon);
                            //SetPvpRanks(pokemon);

                            if (changesResult.IsNewOrHasChanges)
                            {
                                // Send pokemon_added event for InstanceController.Instance.GotPokemon();
                                await PublishData(RedisChannels.PokemonAdded, pokemon);
                                await PublishData(RedisChannels.WebhookPokemon, pokemon.GetWebhookValues("pokemon"));
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
                            if (changesResult.GotIV)
                            {
                                // Send pokemon_updated event for InstanceController.Instance.GotIV();
                                await PublishData(RedisChannels.PokemonUpdated, pokemon);
                                await PublishData(RedisChannels.WebhookPokemon, pokemon.GetWebhookValues("pokemon"));
                                lock (_pokemonLock)
                                {
                                    _pokemon.Add(pokemon);
                                }
                            }
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
                                var cellId = S2CellIdFromLatLng(data.Pokemon.Pokemon.Latitude, data.Pokemon.Pokemon.Longitude);
                                _cells.Add(new Cell(cellId.Id));
                            }
                            try
                            {
                                pokemon = await GetEntity<Pokemon>(encounter.Pokemon.EncounterId, "pokemon").ConfigureAwait(false); // TODO: is_event
                            }
                            catch (Exception ex)
                            {
                                ConsoleExt.WriteError($"Error: {ex}");
                                pokemon = null;
                            }
                            if (pokemon != null)
                            {
                                await pokemon.AddEncounter(encounter, username).ConfigureAwait(false);
                            }
                            else
                            {
                                var cellId = S2CellId.FromLatLng(S2LatLng.FromDegrees(encounter.Pokemon.Latitude, encounter.Pokemon.Longitude));
                                var timestampMs = DateTime.UtcNow.ToTotalSeconds() * 1000;
                                pokemon = new Pokemon(encounter.Pokemon, cellId.Id, timestampMs, username, false); // TODO: IsEvent
                                await pokemon.AddEncounter(encounter, username).ConfigureAwait(false);
                            }
                            SetPvpRanks(pokemon);

                            await PublishData(RedisChannels.PokemonUpdated, pokemon);
                            await PublishData(RedisChannels.WebhookPokemon, pokemon.GetWebhookValues("pokemon"));
                            // TODO: Check for changes
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
                                    var oldGym = await GetEntity<Gym>(fort.FortId, "gym");
                                    var gym = new Gym(cellId, fort);
                                    var gymResult = gym.Update(oldGym);
                                    await PublishGym(gymResult, gym);
                                    if (gymResult.IsNewOrHasChanges)
                                    {
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
                                    var pokestop = new Pokestop(cellId, fort);
                                    var stopResult = pokestop.Update(oldPokestop);
                                    await PublishPokestop(stopResult, pokestop);
                                    if (stopResult.IsNewOrHasChanges)
                                    {
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
                    case RedisChannels.ProtoGymInfo:
                        {
                            /*
                            var gym = GetEntity<Gym>(id, "gym").ConfigureAwait(false)
                                                               .GetAwaiter()
                                                               .GetResult();
                            if (gym != null)
                            {
                                gym.AddDetails(gymInfo);
                                if (gym.Update(gym))
                                {
                                    _gymInfo.Add(gym);
                                }
                            }
                            */
                            break;
                        }
                    case RedisChannels.ProtoGymDefender:
                        {
                            var defender = JsonSerializer.Deserialize<GymDefender>(message);
                            if (defender == null) return;

                            lock (_gymDefendersLock)
                            {
                                _gymDefenders.Add(defender);
                            }
                            break;
                        }
                    case RedisChannels.ProtoGymTrainer:
                        {
                            var trainer = JsonSerializer.Deserialize<Trainer>(message);
                            if (trainer == null) return;

                            lock (_gymTrainersLock)
                            {
                                _gymTrainers.Add(trainer);
                            }
                            break;
                        }
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
                            //if (pokestop.Update(pokestop, true)) // TODO: Check HasChanges property
                            //{
                                await PublishData(RedisChannels.WebhookQuest, pokestop.GetWebhookValues("quest"));
                                lock (_questsLock)
                                {
                                    _quests.Add(pokestop);
                                }
                            //}
                            await SetCacheData($"pokestop_{pokestop.Id}", pokestop);
                            break;
                        }
                    case RedisChannels.ProtoCell:
                        {
                            var cellId = Convert.ToUInt64(message);
                            if (cellId == 0) return;

                            lock (_cellsLock)
                            {
                                _cells.Add(new Cell(cellId));
                            }
                            break;
                        }
                    case RedisChannels.ProtoWeather:
                        {
                            var weather = JsonSerializer.Deserialize<Weather>(message);
                            if (weather == null) return;

                            // TODO: Check for changes
                            await PublishData(RedisChannels.WebhookWeather, weather.GetWebhookValues("weather"));
                            lock (_weatherLock)
                            {
                                _weather.Add(weather);
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
                            // TODO: Send account webhooks
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

        #endregion

        #region Database Ingesters

        private void DataIngester()
        {
            //ThreadPool.QueueUserWorkItem(x =>
            new Thread(x =>
            {
                while (!_shouldExit)
                {
                    UpdateCells();
                    var updatePokemon = true;
                    try
                    {
                        UpdateSpawnpoints();
                    }
                    catch (Exception ex)
                    {
                        updatePokemon = false;
                        ConsoleExt.WriteError($"Failed to update spawnpoints, skipping Pokemon: {ex}");
                    }
                    UpdateWeather();
                    UpdatePokestops();
                    if (updatePokemon)
                    {
                        UpdatePokemon();
                    }
                    UpdateQuests();
                    UpdateGyms();
                    UpdateGymTrainers();
                    UpdateGymDefenders();
                    UpdateAccounts();

                    // Consume data every x seconds
                    Thread.Sleep(ConsumeIntervalS * 1000);
                }
            })
            { IsBackground = true }.Start();
        }

        private void UpdateCells()
        {
            //var cells = _cells.GetRange(0, _cells.Count);
            //var count = (int)Math.Min(MaxConcurrency, cells.Count);
            //_cells.RemoveRange(0, count);
            lock (_cellsLock)
            {
                if (_cells.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var cellsCount = 0;
                var cells = _cells;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {cells.Count:N0} S2Cells");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var cellRepository = new CellRepository(ctx);
                        cellRepository.InsertOrUpdate(cells)
                                      .ConfigureAwait(false)
                                      .GetAwaiter()
                                      .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateCells: {ex.Message}");
                    }
                    cellsCount = cells.Count;
                }
                _cells.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] S2Cells Count: {cellsCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdateSpawnpoints()
        {
            lock (_spawnpointsLock)
            {
                if (_spawnpoints.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var spawnpointsCount = 0;
                var spawnpoints = _spawnpoints;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {spawnpoints.Count:N0} Spawnpoints");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var spawnpointRepository = new SpawnpointRepository(ctx);
                        spawnpointRepository.InsertOrUpdate(spawnpoints)
                                            .ConfigureAwait(false)
                                            .GetAwaiter()
                                            .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateSpawnpoints: {ex.Message}");
                    }
                    spawnpointsCount = spawnpoints.Count;
                }
                _spawnpoints.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Spawnpoints Count: {spawnpointsCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdateWeather()
        {
            lock (_weatherLock)
            {
                if (_weather.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var weatherCount = 0;
                var weather = _weather;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {weather.Count:N0} Weather");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var weatherRepository = new WeatherRepository(ctx);
                        weatherRepository.InsertOrUpdate(weather)
                                         .ConfigureAwait(false)
                                         .GetAwaiter()
                                         .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateWeather: {ex.Message}");
                    }
                    weatherCount = weather.Count;
                }
                _weather.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Weather Count: {weatherCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdateGyms()
        {
            lock (_gymsLock)
            {
                if (_gyms.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var gymCount = 0;
                var gyms = _gyms;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {gyms.Count:N0} Gyms");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var gymRepository = new GymRepository(ctx);
                        gymRepository.InsertOrUpdate(gyms)
                                     .ConfigureAwait(false)
                                     .GetAwaiter()
                                     .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateGyms: {ex.Message}");
                    }
                    gymCount = gyms.Count;
                }
                _gyms.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Gyms Count: {gymCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdateGymDefenders()
        {
            lock (_gymDefendersLock)
            {
                if (_gymDefenders.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var gymDefendersCount = 0;
                var gymDefenders = _gymDefenders;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {gymDefenders.Count:N0} Gym Defenders");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var defenderRepository = new GymDefenderRepository(ctx);
                        defenderRepository.InsertOrUpdate(gymDefenders)
                                          .ConfigureAwait(false)
                                          .GetAwaiter()
                                          .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateGymDefenders: {ex.Message}");
                    }
                    gymDefendersCount = gymDefenders.Count;
                }
                _gymDefenders.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Gym Defenders Count: {gymDefendersCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdateGymTrainers()
        {
            lock (_gymTrainersLock)
            {
                if (_gymTrainers.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var gymTrainersCount = 0;
                var gymTrainers = _gymTrainers;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {gymTrainers.Count:N0} Gym Trainers");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var trainerRepository = new GymTrainerRepository(ctx);
                        trainerRepository.InsertOrUpdate(gymTrainers)
                                         .ConfigureAwait(false)
                                         .GetAwaiter()
                                         .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdateGymTrainers: {ex.Message}");
                    }
                    gymTrainersCount = gymTrainers.Count;
                }
                _gymTrainers.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Gym Trainers Count: {gymTrainersCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdatePokestops()
        {
            lock (_pokestopsLock)
            {
                if (_pokestops.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var pokestopCount = 0;
                var pokestops = _pokestops;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {pokestops.Count:N0} Pokestops");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var pokestopRepository = new PokestopRepository(ctx);
                        pokestopRepository.InsertOrUpdate(pokestops)
                                          .ConfigureAwait(false)
                                          .GetAwaiter()
                                          .GetResult();
                    }
                    catch (MySqlConnector.MySqlException ex)
                    {
                        ConsoleExt.WriteError($"[DataConsumer] UpdatePokestops: {ex.Message}");
                    }
                    pokestopCount = pokestops.Count;
                }
                _pokestops.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Pokestops Count: {pokestopCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdatePokemon()
        {
            lock (_pokemonLock)
            {
                if (_pokemon.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var pokemonCount = 0;
                var pokemon = _pokemon;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {pokemon.Count:N0} Pokemon");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
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
                    pokemonCount = pokemon.Count;
                }
                _pokemon.Clear();
                stopwatch.Stop();
                ConsoleExt.WriteInfo($"[DataConsumer] Pokemon Count: {pokemonCount:N0} parsed in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private void UpdateQuests()
        {
            lock (_questsLock)
            {
                if (_quests.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var questCount = 0;
                var quests = _quests;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {quests.Count:N0} Quests");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var pokestopRepository = new PokestopRepository(ctx);
                        pokestopRepository.InsertOrUpdate(quests)
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
            }
        }

        private void UpdateAccounts()
        {
            lock (_accountsLock)
            {
                if (_playerData.Count == 0)
                    return;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var accountCount = 0;
                var accounts = _playerData;
                ConsoleExt.WriteInfo($"[DataConsumer] Inserting {accounts.Count:N0} Account Data");
                using (var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]))
                {
                    try
                    {
                        var accountRepository = new AccountRepository(ctx);
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
            }
        }

        #endregion

        #region Cache Helpers

        private Task<RedisValue> GetData(string key)
        {
            try
            {
                if (_redis.IsConnected)
                {
                    return _redisDatabase.ListLeftPopAsync(key);
                }
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex);
            }
            return default;
        }

        private async Task<T> GetEntity<T>(ulong id, string prefix) where T : BaseEntity
        {
            var oldEntity = await GetCacheData<T>(prefix + "_" + id).ConfigureAwait(false);
            if (oldEntity == null)
            {
                // Entity does not exist in redis database cache, get from sql database
                using var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]);
                oldEntity = await ctx.Set<T>().FindAsync(new object[] { id }).ConfigureAwait(false);
            }
            return oldEntity;
        }

        private async Task<T> GetEntity<T>(string id, string prefix) where T : BaseEntity
        {
            var oldEntity = await GetCacheData<T>(prefix + "_" + id).ConfigureAwait(false);
            if (oldEntity == null)
            {
                // Entity does not exist in redis database cache, get from sql database
                using var ctx = DbContextFactory.CreateDeviceControllerContext(_config.Root["DbConnectionString"]);
                oldEntity = await ctx.Set<T>().FindAsync(new object[] { id }).ConfigureAwait(false);
            }
            return oldEntity;
        }

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

        #region Redis Event Publishing

        private async Task PublishGym(GymResult result, Gym gym)
        {
            if (result.SendGym)
            {
                await PublishData(RedisChannels.WebhookGym, gym.GetWebhookValues("gym"));
            }
            if (result.SendGymInfo)
            {
                // TODO: await PublishData(RedisChannels.WebhookGymInfo, gym.GetWebhookValues("gym-info"));
            }
            if (result.SendRaid)
            {
                await PublishData(RedisChannels.WebhookRaid, gym.GetWebhookValues("raid"));
            }
            if (result.SendEgg)
            {
                await PublishData(RedisChannels.WebhookEgg, gym.GetWebhookValues("egg"));
            }
        }

        private async Task PublishPokestop(PokestopResult result, Pokestop pokestop)
        {
            if (result.SendPokestop)
            {
                await PublishData(RedisChannels.WebhookPokestop, pokestop.GetWebhookValues("pokestop"));
            }
            if (result.SendLure)
            {
                await PublishData(RedisChannels.WebhookLure, pokestop.GetWebhookValues("lure"));
            }
            if (result.SendQuest)
            {
                await PublishData(RedisChannels.WebhookQuest, pokestop.GetWebhookValues("quest"));
            }
            if (result.SendInvasion)
            {
                await PublishData(RedisChannels.WebhookInvasion, pokestop.GetWebhookValues("invasion"));
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

        #endregion

        private void SetPvpRanks(Pokemon pokemon)
        {
            if (pokemon.AttackIV == null)
            {
                return;
            }
            var ranks = _pvpCalculator.QueryPvpRank
            (
                pokemon.PokemonId,
                pokemon.Form ?? 0,
                pokemon.Costume,
                pokemon.AttackIV ?? 0,
                pokemon.DefenseIV ?? 0,
                pokemon.StaminaIV ?? 0,
                pokemon.Level ?? 0,
                (PokemonGender)pokemon.Gender
            );
            if (ranks.Count > 0)
            {
                if (ranks.ContainsKey("great"))
                {
                    pokemon.PvpRankingsGreatLeague = ranks["great"];
                }
                if (ranks.ContainsKey("ultra"))
                {
                    pokemon.PvpRankingsUltraLeague = ranks["ultra"];
                }
            }
        }

        private static S2CellId S2CellIdFromLatLng(double latitude, double longitude)
        {
            return S2CellId.FromLatLng(S2LatLng.FromDegrees(latitude, longitude));
        }
    }
}