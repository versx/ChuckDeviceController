namespace ChuckDeviceController.Services
{
    using System.Data;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Threading;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using Nito.AsyncEx;
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
    using Z.BulkOperations;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.HostedServices;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Pvp;
    using ChuckDeviceController.Pvp.Models;
    using ChuckDeviceController.Services.Rpc;

    // TODO: Use one MySQL factory DbContext for all received entities
    // TODO: Use/benchmark Dapper Micro ORM
    // TODO: Implement memory cache for all map data entities
    // TODO: Split up/refactor class

    public class DataProcessorService : BackgroundService, IDataProcessorService
    {
        #region Variables

        private readonly ILogger<IDataProcessorService> _logger;
        private readonly IAsyncQueue<List<dynamic>> _taskQueue;
        private readonly IDbContextFactory<MapDbContext> _dbFactory;
        private readonly IMemoryCache _diskCache;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IClearFortsHostedService _clearFortsService;
        private readonly IMemoryCacheHostedService _memCache;

        private readonly AsyncLock _cellsLock = new();
        private readonly AsyncLock _weatherLock = new();
        private readonly AsyncLock _wildPokemonLock = new();
        private readonly AsyncLock _nearbyPokemonLock = new();
        private readonly AsyncLock _mapPokemonLock = new();
        private readonly AsyncLock _fortsLock = new();
        private readonly AsyncLock _fortDetailsLock = new();
        private readonly AsyncLock _gymInfoLock = new();
        private readonly AsyncLock _questsLock = new();
        private readonly AsyncLock _encountersLock = new();
        private readonly AsyncLock _diskEncountersLock = new();
        private readonly AsyncLock _dbLock = new();

        #endregion

        #region Properties

        public ProcessorOptionsConfig Options { get; }

        #endregion

        #region Constructor

        public DataProcessorService(
            ILogger<IDataProcessorService> logger,
            IOptions<ProcessorOptionsConfig> options,
            IAsyncQueue<List<dynamic>> taskQueue,
            IDbContextFactory<MapDbContext> factory,
            IMemoryCache diskCache,
            IGrpcClientService grpcClientService,
            IClearFortsHostedService clearFortsService,
            //IMemoryCache memCache)
            IMemoryCacheHostedService memCache)
            //: base(new Logger<TimedHostedService>(LoggerFactory.Create(x => x.AddConsole())))
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _dbFactory = factory;
            _diskCache = diskCache;
            _grpcClientService = grpcClientService;
            _clearFortsService = clearFortsService;
            _memCache = memCache;

            Options = options.Value;
        }

        #endregion

        #region Background Service

        public async Task ConsumeDataAsync(string username, List<dynamic> data)
        {
            ProtoDataStatistics.Instance.TotalEntitiesReceived += (uint)data.Count;

            //await _taskQueue.EnqueueAsync(async token => await ProcessWorkItemAsync(username, data, token));
            _taskQueue.Enqueue(data);
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IDataProcessorService)} is stopping.");

            await base.StopAsync(stoppingToken);
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IDataProcessorService)} is now running in the background.");

            await Task.Run(async () =>
                await BackgroundProcessing(stoppingToken)
            , stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_taskQueue.Count == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    var workItems = new[] { await _taskQueue.DequeueAsync(stoppingToken) };
                    //var workItems = await _taskQueue.DequeueBulkAsync(5, stoppingToken);
                    //var workItems = await _taskQueue.DequeueBulkAsync(Strings.MaximumQueueBatchSize, stoppingToken);
                    if (workItems == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    Parallel.ForEach(workItems, async payload => await ProcessWorkItemAsync("TODO: Fix", payload, stoppingToken));
                    //Parallel.ForEach(workItems, task => task(stoppingToken));

                    //var workItems = await _taskQueue.DequeueMultipleAsync(Strings.MaximumQueueBatchSize, stoppingToken);
                    //var tasks = workItems.Select(item => Task.Factory.StartNew(async () => await item(stoppingToken)));
                    //Task.WaitAll(tasks.ToArray(), stoppingToken);
                    //await Task.Run(() =>
                    //{
                    //    foreach (var workItem in workItems)
                    //    {
                    //        await Task.Factory.StartNew(async () => await workItem(stoppingToken));
                    //        //await workItem(stoppingToken);
                    //    }
                    //}, stoppingToken);

                    //foreach (var workItem in workItems)
                    //{
                    //    await workItem(stoppingToken);
                    //}
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
                //await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                Thread.Sleep(1);
            }

            _logger.LogError("Exited background processing...");
        }

        private async Task ProcessWorkItemAsync(string username, List<dynamic> data, CancellationToken stoppingToken)
        {
            if (data.Count == 0)
            {
                return;
            }

            CheckQueueLength();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ProtoDataStatistics.Instance.TotalEntitiesUpserted += (uint)data.Count;

            /*
            var playerData = data.Where(x => x.type == ProtoDataType.PlayerData)
                                 .Select(x => x.data)
                                 .ToList();
            if (playerData.Count > 0)
            {
                // Insert account player data
                await UpdatePlayerDataAsync(playerData);
            }
            */

            using (await _dbLock.LockAsync(stoppingToken))
            {
                //using var context = await _dbFactory.CreateDbContextAsync(stoppingToken);
                //using var context = _dbFactory.CreateDbContext();

                var cells = data.Where(x => x.type == ProtoDataType.Cell)
                                .Select(x => x.cell)
                                .Distinct()
                                .ToList();
                if (cells.Count > 0)
                {
                    // Insert S2 cells
                    //using (await _cellsLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateCellsAsync(cells));
                        if (Options.IsEnabled(DataLogLevel.S2Cells))
                        {
                            _logger.LogInformation($"Upserted {cells.Count:N0} S2 Sells in {benchmark}s");
                        }
                    }
                }

                var clientWeather = data.Where(x => x.type == ProtoDataType.ClientWeather)
                                        .Select(x => (ClientWeatherProto)x.data)
                                        .Distinct()
                                        .ToList();
                if (clientWeather.Count > 0)
                {
                    // Insert weather cells
                    //using (await _weatherLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateClientWeatherAsync(clientWeather));
                        if (Options.IsEnabled(DataLogLevel.Weather))
                        {
                            _logger.LogInformation($"Upserted {clientWeather.Count:N0} Client Weather Cells in {benchmark}s");
                        }
                    }
                }

                var forts = data.Where(x => x.type == ProtoDataType.Fort)
                                .ToList();
                if (forts.Count > 0)
                {
                    // Insert Forts
                    //using (await _fortsLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateFortsAsync(username, forts));
                        if (Options.IsEnabled(DataLogLevel.Forts))
                        {
                            _logger.LogInformation($"Upserted {forts.Count:N0} Forts in {benchmark}s");
                        }
                    }
                }

                var fortDetails = data.Where(x => x.type == ProtoDataType.FortDetails)
                                      .ToList();
                if (fortDetails.Count > 0)
                {
                    // Insert Fort Details
                    //using (await _fortDetailsLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateFortDetailsAsync(fortDetails));
                        if (Options.IsEnabled(DataLogLevel.FortDetails))
                        {
                            _logger.LogInformation($"Upserted {fortDetails.Count:N0} Fort Details in {benchmark}s");
                        }
                    }
                }

                var gymInfos = data.Where(x => x.type == ProtoDataType.GymInfo)
                                   .ToList();
                if (gymInfos.Count > 0)
                {
                    // Insert gym info
                    //using (await _fortDetailsLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateGymInfoAsync(gymInfos));
                        if (Options.IsEnabled(DataLogLevel.GymInfo))
                        {
                            _logger.LogInformation($"Upserted {gymInfos.Count:N0} Gym Information in {benchmark}s");
                        }
                    }
                }


                var wildPokemon = data.Where(x => x.type == ProtoDataType.WildPokemon)
                                      .ToList();
                if (wildPokemon.Count > 0)
                {
                    // Insert wild pokemon
                    //using (await _wildPokemonLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateWildPokemonAsync(wildPokemon));
                        if (Options.IsEnabled(DataLogLevel.WildPokemon))
                        {
                            _logger.LogInformation($"Upserted {wildPokemon.Count:N0} Wild Pokemon in {benchmark}s");
                        }
                    }
                }

                var nearbyPokemon = data.Where(x => x.type == ProtoDataType.NearbyPokemon)
                                        .ToList();
                if (nearbyPokemon.Count > 0)
                {
                    // Insert nearby pokemon
                    //using (await _nearbyPokemonLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateNearbyPokemonAsync(nearbyPokemon));
                        if (Options.IsEnabled(DataLogLevel.NearbyPokemon))
                        {
                            _logger.LogInformation($"Upserted {nearbyPokemon.Count:N0} Nearby Pokemon in {benchmark}s");
                        }
                    }
                }

                var mapPokemon = data.Where(x => x.type == ProtoDataType.MapPokemon)
                                     .ToList();
                if (mapPokemon.Count > 0)
                {
                    // Insert map pokemon
                    //using (await _mapPokemonLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateMapPokemonAsync(mapPokemon));
                        if (Options.IsEnabled(DataLogLevel.MapPokemon))
                        {
                            _logger.LogInformation($"Upserted {mapPokemon.Count:N0} Lure Pokemon in {benchmark}s");
                        }
                    }
                }

                //if (wildPokemon.Count > 0 || nearbyPokemon.Count > 0 || mapPokemon.Count > 0)
                //{
                //    await UpdatePokemonAsync(wildPokemon, nearbyPokemon, mapPokemon);
                //}

                var quests = data.Where(x => x.type == ProtoDataType.Quest)
                                 .ToList();
                if (quests.Count > 0)
                {
                    // Insert quests
                    //using (await _questsLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateQuestsAsync(quests));
                        if (Options.IsEnabled(DataLogLevel.Quests))
                        {
                            _logger.LogInformation($"Upserted {quests.Count:N0} Pokestop Quests in {benchmark}s");
                        }
                    }
                }

                var encounters = data.Where(x => x.type == ProtoDataType.Encounter)
                                     .ToList();
                if (encounters.Count > 0)
                {
                    // Insert Pokemon encounters
                    //using (await _encountersLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateEncountersAsync(encounters));
                        if (Options.IsEnabled(DataLogLevel.PokemonEncounters))
                        {
                            _logger.LogInformation($"Upserted {encounters.Count:N0} Pokemon Encounters in {benchmark}s");
                        }
                    }
                }

                var diskEncounters = data.Where(x => x.type == ProtoDataType.DiskEncounter)
                                         .ToList();
                if (diskEncounters.Count > 0)
                {
                    // Insert lured/disk Pokemon encounters
                    //using (await _diskEncountersLock.LockAsync(stoppingToken))
                    {
                        var benchmark = BenchmarkAction(async () => await UpdateDiskEncountersAsync(diskEncounters));
                        if (Options.IsEnabled(DataLogLevel.PokemonDiskEncounters))
                        {
                            _logger.LogInformation($"Upserted {diskEncounters.Count:N0} Disk Pokemon Encounters in {benchmark}s");
                        }
                    }
                }

                stopwatch.Stop();

                if (Options.IsEnabled(DataLogLevel.Summary))
                {
                    var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                    _logger.LogInformation($"Data processer upserted {data.Count:N0} entities in {totalSeconds}s");
                }

                // TODO: Add config check to startup services registration pipeline
                if (Options.ClearOldForts)
                {
                    // Clear any old Gyms or Pokestops that might have been removed from the game
                    //await _clearFortsService.ClearOldFortsAsync();
                }
            }
        }

        #endregion

        /*
        protected override async Task RunJobAsync(CancellationToken stoppingToken)
        {
            try
            {
                var workItems = await _taskQueue.DequeueMultipleAsync(Strings.MaximumQueueBatchSize, stoppingToken);
                if (workItems == null)
                {
                    Thread.Sleep(1);
                    return;
                }

                Parallel.ForEach(workItems, async task => await task(stoppingToken));

                //foreach (var workItem in workItems)
                //{
                //    await workItem(stoppingToken);
                //    Thread.Sleep(1);
                //}
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
            //await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
            Thread.Sleep(1);

            //_logger.LogError("Exited background processing...");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IDataProcessorService)} is now running in the background.");

            await Task.CompletedTask;
        }
        */

        #region Data Handling Methods

        private async Task UpdatePlayerDataAsync(IEnumerable<dynamic> playerData)
        {
            try
            {
                foreach (var data in playerData)
                {
                    // TODO: Update related accounts
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdatePlayerDataAsync: {ex}");
            }

            await Task.CompletedTask;
        }

        private async Task UpdateCellsAsync(IEnumerable<dynamic> cells)
        //private async Task UpdateCellsAsync(MapDbContext context, IEnumerable<dynamic> cells)
        {
            try
            {
                // Convert cell ids to Cell models
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                //var raw = context.Database.ExecuteSqlRawAsync("", cells);
                var s2cells = cells
                    // Filter cells not already cached
                    .Where(cell => !_memCache.IsSet<ulong, Cell>(cell))
                    .Select(cell =>
                    {
                        ulong id = Convert.ToUInt64(Convert.ToString(cell));
                        //var cached = _memCache.Get<ulong, Cell>(id);
                        //return cached ?? new Cell(id);
                        return new Cell(id);
                    })
                    .ToList();
                // TODO: Check if s2 cell is in cache, if so remove from upsert list
                await context.Cells.BulkMergeAsync(s2cells, options => GetBulkOptions<Cell>(p => new { p.Updated }));

                foreach (var cell in s2cells)
                {
                    _clearFortsService.AddCell(cell.Id);
                    _memCache.Set(cell.Id, cell);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateCellsAsync: {ex}");
            }
        }

        private async Task UpdateClientWeatherAsync(IEnumerable<ClientWeatherProto> clientWeather)
        //private async Task UpdateClientWeatherAsync(MapDbContext context, IEnumerable<ClientWeatherProto> clientWeather)
        {
            try
            {
                // Convert weather protos to Weather models
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var weather = clientWeather
                    // Filter cells not already cached
                    .Where(weatherCell => !_memCache.IsSet<long, Weather>(weatherCell.S2CellId))
                    .Select(weatherCell =>
                    {
                        long id = Convert.ToInt64(Convert.ToString(weatherCell.S2CellId));
                        //var cached = _memCache.Get<long, Weather>(id);
                        //return cached ?? new Weather(weatherCell);
                        return new Weather(weatherCell);
                    })
                    .ToList();
                foreach (var weatherCell in weather)
                {
                    await weatherCell.UpdateAsync(context);

                    if (weatherCell.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Weather, weatherCell);
                    }

                    _memCache.Set(weatherCell.Id, weatherCell);
                }

                await context.Weather.BulkMergeAsync(weather, options =>
                {
                    options.AllowDuplicateKeys = false;
                    options.UseTableLock = true;
                    options.OnMergeUpdateInputExpression = p => new
                    {
                        p.GameplayCondition,
                        p.CloudLevel,
                        p.FogLevel,
                        p.RainLevel,
                        p.SnowLevel,
                        p.WindLevel,
                        p.WindDirection,
                        p.SpecialEffectLevel,
                        p.Severity,
                        p.WarnWeather,
                        p.Updated,
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateClientWeatherAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateWildPokemonAsync(IEnumerable<dynamic> wildPokemon)
        //private async Task UpdateWildPokemonAsync(MapDbContext context, IEnumerable<dynamic> wildPokemon)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                var spawnpointsToUpsert = new List<Spawnpoint>();
                foreach (var wild in wildPokemon)
                {
                    var cellId = wild.cell;
                    var data = (WildPokemonProto)wild.data;
                    var timestampMs = wild.timestampMs;
                    var username = wild.username;
                    var isEvent = wild.isEvent;
                    var pokemon = new Pokemon(data, cellId, username, isEvent);
                    var spawnpoint = await UpdateSpawnpointAsync(context, pokemon, data, timestampMs);
                    if (spawnpoint != null)
                    {
                        spawnpointsToUpsert.Add(spawnpoint);
                    }

                    await pokemon.UpdateAsync(context, updateIv: false);
                    pokemonToUpsert.Add(pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }

                if (spawnpointsToUpsert.Count > 0)
                {
                    await context.Spawnpoints.BulkMergeAsync(spawnpointsToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.Id,
                            p.LastSeen,
                            p.Updated,
                            p.DespawnSecond,
                        };
                    });

                    spawnpointsToUpsert.ForEach(spawnpoint => _memCache.Set(spawnpoint.Id, spawnpoint));

                    _logger.LogInformation($"Upserted {spawnpointsToUpsert.Count:N0} Spawnpoints");
                }

                if (pokemonToUpsert.Count > 0)
                {
                    await context.Pokemon.BulkMergeAsync(pokemonToUpsert, options =>
                    {
                        // Do not update IV specific columns
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        //options.IgnoreOnMergeUpdate = true;
                        //options.ResultInfo.
                        options.ForceTriggerResolution = true;
                        options.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.Id,
                            p.AttackIV,
                            p.DefenseIV,
                            p.StaminaIV,
                            p.CP,
                            p.Level,
                            p.Size,
                            p.Weight,
                            p.Move1,
                            p.Move2,
                            p.PvpRankings,
                        };
                    });

                    pokemonToUpsert.ForEach(pkmn => _memCache.Set(pkmn.Id, pkmn));

                    /*
                    foreach (var pokemon in pokemonToUpsert)
                    {
                        if (context.Pokemon.Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                    }

                    await context.SaveChangesAsync();
                    */

                    await SendPokemonAsync(pokemonToUpsert);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateWildPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateNearbyPokemonAsync(IEnumerable<dynamic> nearbyPokemon)
        //private async Task UpdateNearbyPokemonAsync(MapDbContext context, IEnumerable<dynamic> nearbyPokemon)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                foreach (var nearby in nearbyPokemon)
                {
                    var cellId = nearby.cell;
                    var data = (NearbyPokemonProto)nearby.data;
                    var username = nearby.username;
                    var isEvent = nearby.isEvent;
                    var pokemon = new Pokemon(context, data, cellId, username, isEvent);
                    await pokemon.UpdateAsync(context, updateIv: false);
                    pokemonToUpsert.Add(pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }

                if (pokemonToUpsert.Count > 0)
                {
                    await context.Pokemon.BulkMergeAsync(pokemonToUpsert, options =>
                    {
                        // Do not update IV specific columns
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        options.ForceTriggerResolution = true;
                        options.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.Id,
                            p.Costume,
                            p.Form,
                            p.AttackIV,
                            p.DefenseIV,
                            p.StaminaIV,
                            p.CP,
                            p.Level,
                            p.Size,
                            p.Weight,
                            p.Move1,
                            p.Move2,
                            p.PvpRankings,
                        };
                    });

                    await SendPokemonAsync(pokemonToUpsert);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateNearbyPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateMapPokemonAsync(IEnumerable<dynamic> mapPokemon)
        //private async Task UpdateMapPokemonAsync(MapDbContext context, IEnumerable<dynamic> mapPokemon)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                foreach (var map in mapPokemon)
                {
                    var cellId = map.cell;
                    var data = (MapPokemonProto)map.data;
                    var username = map.username;
                    var isEvent = map.isEvent;
                    var pokemon = new Pokemon(context, data, cellId, username, isEvent);
                    await pokemon.UpdateAsync(context, updateIv: false);

                    // Check if we have a pending disk encounter cache
                    var displayId = data.PokemonDisplay.DisplayId;
                    var cachedDiskEncounter = _diskCache.Get<DiskEncounterOutProto>(displayId);
                    if (cachedDiskEncounter != null)
                    {
                        // Thanks Fabio <3
                        _logger.LogDebug($"Found Pokemon disk encounter with id '{displayId}' in cache");

                        pokemon.AddDiskEncounter(cachedDiskEncounter, username);
                        await pokemon.UpdateAsync(context, updateIv: true);
                    }
                    else
                    {
                        // Failed to get DiskEncounter from cache
                        _logger.LogWarning($"Unable to fetch cached Pokemon disk encounter with id '{displayId}' from cache");
                    }

                    pokemonToUpsert.Add(pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }

                if (pokemonToUpsert.Count > 0)
                {
                    await context.Pokemon.BulkMergeAsync(pokemonToUpsert, options =>
                    {
                        // TODO: Do not update IV :thinking: wait maybe we do need to. IIRC they are found within 70m range, need to confirm
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        options.ForceTriggerResolution = true;
                    });

                    await SendPokemonAsync(pokemonToUpsert);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateMapPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateFortsAsync(string username, IEnumerable<dynamic> forts)
        //private async Task UpdateFortsAsync(MapDbContext context, string username, IEnumerable<dynamic> forts)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                // Send found/nearby forts with gRPC service for leveling instance
                var lvlForts = forts.Where(fort => fort.data.FortType != FortType.Gym)
                                    .Select(fort => fort.data)
                                    .Select(fort => (PokemonFortProto)fort)
                                    .ToList();
                // Ensure that the account username is set, otherwise ignore relaying
                // fort data for leveling instance
                if (!string.IsNullOrEmpty(username) && lvlForts.Count > 0)
                {
                    await SendGymsAsync(lvlForts, username);
                }

                var pokestopsToUpsert = new List<Pokestop>();
                var gymsToUpsert = new List<Gym>();
                var incidentsToUpsert = new List<Incident>();

                // Convert fort protos to Pokestop/Gym models
                foreach (var fort in forts)
                {
                    var cellId = (ulong)fort.cell;
                    var data = (PokemonFortProto)fort.data;
                    //var username = (string)fort.username;

                    switch (data.FortType)
                    {
                        case FortType.Checkpoint:
                            // Init Pokestop model from fort proto data
                            var pokestop = new Pokestop(data, cellId);
                            var pokestopWebhooks = await pokestop.UpdateAsync(context, updateQuest: false);
                            if (pokestopWebhooks.Count > 0)
                            {
                                foreach (var webhook in pokestopWebhooks)
                                {
                                    var type = ConvertWebhookType(webhook.Key);
                                    await SendWebhookPayloadAsync(type, webhook.Value);
                                }
                            }

                            pokestopsToUpsert.Add(pokestop);

                            // Loop incidents
                            if ((pokestop.Incidents?.Count ?? 0) > 0)
                            {
                                foreach (var incident in pokestop!.Incidents!)
                                {
                                    await incident.UpdateAsync(context);
                                    if (incident.SendWebhook)
                                    {
                                        await SendWebhookPayloadAsync(WebhookPayloadType.Invasion, new PokestopWithIncident(pokestop, incident));
                                    }
                                }
                                incidentsToUpsert.AddRange(pokestop.Incidents);
                            }

                            _clearFortsService.AddPokestop(cellId, data.FortId);
                            break;
                        case FortType.Gym:
                            // Init Gym model from fort proto data
                            var gym = new Gym(data, cellId);
                            var gymWebhooks = await gym.UpdateAsync(context);
                            if (gymWebhooks.Count > 0)
                            {
                                foreach (var webhook in gymWebhooks)
                                {
                                    var type = ConvertWebhookType(webhook.Key);
                                    await SendWebhookPayloadAsync(type, webhook.Value);
                                }
                            }

                            gymsToUpsert.Add(gym);

                            _clearFortsService.AddGym(cellId, data.FortId);
                            break;
                    }
                }

                if (pokestopsToUpsert.Count > 0)
                {
                    await context.Pokestops.BulkMergeAsync(pokestopsToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        // Ignore the following columns when updating to prevent overwrite
                        // of existing quest columns set
                        options.IgnoreOnMergeUpdateExpression = p => new
                        {
                            p.Id,
                            p.QuestType,
                            p.QuestTitle,
                            p.QuestTimestamp,
                            p.QuestTemplate,
                            p.QuestTarget,
                            p.QuestRewardType,
                            p.QuestRewards,
                            p.QuestConditions,

                            p.AlternativeQuestType,
                            p.AlternativeQuestTitle,
                            p.AlternativeQuestTimestamp,
                            p.AlternativeQuestTemplate,
                            p.AlternativeQuestTarget,
                            p.AlternativeQuestRewardType,
                            p.AlternativeQuestRewards,
                            p.AlternativeQuestConditions,
                        };
                        //options.BatchSize = 
                        //options.ErrorMode = ErrorModeType.RetrySingleAndContinue;
                        //options.InsertIfNotExists
                        //options.InsertKeepIdentity
                        //options.MergeKeepIdentity

                        //options.OnMergeUpdateInputExpression

                        //options.Provider = ProviderType.MySql
                        //options.Resolution = ResolutionType.Smart
                        //options.ResultInfo.
                    });
                }
                if (incidentsToUpsert.Count > 0)
                {
                    await context.Incidents.BulkMergeAsync(incidentsToUpsert, options => options.UseTableLock = true);
                    _logger.LogInformation($"Upserted {incidentsToUpsert.Count:N0} Pokestop Incidents");

                    incidentsToUpsert.ForEach(incident => _memCache.Set(incident.Id, incident));
                }
                if (gymsToUpsert.Count > 0)
                {
                    await context.Gyms.BulkMergeAsync(gymsToUpsert, options => options.UseTableLock = true);

                    gymsToUpsert.ForEach(gym => _memCache.Set(gym.Id, gym));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFortsAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateFortDetailsAsync(IEnumerable<dynamic> fortDetails)
        //private async Task UpdateFortDetailsAsync(MapDbContext context, IEnumerable<dynamic> fortDetails)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var pokestopsToUpsert = new List<Pokestop>();
                var gymsToUpsert = new List<Gym>();

                // Convert fort details protos to Pokestop/Gym models
                foreach (var fortDetail in fortDetails)
                {
                    var data = (FortDetailsOutProto)fortDetail.data;
                    // TODO: Check memory cache
                    switch (data.FortType)
                    {
                        case FortType.Checkpoint:
                            //var pokestop = await context.Pokestops.FindAsync(data.Id);
                            var pokestop = (
                                from p in context.Pokestops
                                select new Pokestop(new { p.Id, p.Name, p.Url })
                            ).FirstOrDefault();
                            if (pokestop == null)
                                continue;

                            pokestop.AddDetails(data);
                            var pokestopWebhooks = await pokestop.UpdateAsync(context);
                            foreach (var webhook in pokestopWebhooks)
                            {
                                var type = ConvertWebhookType(webhook.Key);
                                await SendWebhookPayloadAsync(type, pokestop);
                            }
                            if (pokestop.HasChanges)
                            {
                                pokestopsToUpsert.Add(pokestop);
                            }
                            break;
                        case FortType.Gym:
                            //var gym = await context.Gyms.FindAsync(data.Id);
                            var gym = (
                                from g in context.Gyms
                                select new Gym(new { g.Id, g.Name, g.Url })
                            ).FirstOrDefault();
                            if (gym == null)
                                continue;

                            gym.AddDetails(data);
                            var gymWebhooks = await gym.UpdateAsync(context);
                            foreach (var webhook in gymWebhooks)
                            {
                                var type = ConvertWebhookType(webhook.Key);
                                await SendWebhookPayloadAsync(type, gym);
                            }
                            if (gym.HasChanges)
                            {
                                gymsToUpsert.Add(gym);
                            }
                            break;
                    }
                }

                if (pokestopsToUpsert.Count > 0)
                {
                    await context.Pokestops.BulkInsertAsync(pokestopsToUpsert, options =>
                    {
                        options.UseTableLock = true;
                        options.AllowDuplicateKeys = false;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            // Only update necessary columns
                            p.Id,
                            p.Name,
                            p.Url,
                        };
                    });

                    pokestopsToUpsert.ForEach(pokestop => _memCache.Set(pokestop.Id, pokestop));
                    //_logger.LogInformation($"Upserted {pokestopsToUpsert.Count:N0} Pokestop Details");
                }

                if (gymsToUpsert.Count > 0)
                {
                    await context.Gyms.BulkInsertAsync(gymsToUpsert, options =>
                    {
                        options.UseTableLock = true;
                        options.AllowDuplicateKeys = false;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            // Only update necessary columns
                            p.Id,
                            p.Name,
                            p.Url,
                        };
                    });

                    gymsToUpsert.ForEach(gym => _memCache.Set(gym.Id, gym));
                    //_logger.LogInformation($"Upserted {gymsToUpsert.Count:N0} Gym Details");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFortDetailsAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateGymInfoAsync(IEnumerable<dynamic> gymInfos)
        //private async Task UpdateGymInfoAsync(MapDbContext context, IEnumerable<dynamic> gymInfos)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var gymsToUpsert = new List<Gym>();
                var gymDefendersToUpsert = new List<GymDefender>();
                var gymTrainersToUpsert = new List<GymTrainer>();

                // Convert gym info protos to Gym models
                foreach (var gymInfo in gymInfos)
                {
                    var data = (GymGetInfoOutProto)gymInfo.data;
                    var fortId = data.GymStatusAndDefenders.PokemonFortProto.FortId;

                    var gym = await context.Gyms.FindAsync(fortId);
                    if (gym != null)
                    {
                        gym.AddDetails(data);
                        var webhooks = await gym.UpdateAsync(context);
                        foreach (var webhook in webhooks)
                        {
                            var type = ConvertWebhookType(webhook.Key);
                            await SendWebhookPayloadAsync(type, gym);
                        }
                        if (gym.HasChanges)
                        {
                            gymsToUpsert.Add(gym);
                        }
                    }

                    var gymDefenders = data.GymStatusAndDefenders.GymDefender;
                    if (gymDefenders == null)
                        continue;

                    foreach (var gymDefenderData in gymDefenders)
                    {
                        if (gymDefenderData.TrainerPublicProfile != null)
                        {
                            var gymTrainer = new GymTrainer(gymDefenderData.TrainerPublicProfile);
                            gymTrainersToUpsert.Add(gymTrainer);

                            if (gym != null)
                            {
                                // Send webhook
                                await SendWebhookPayloadAsync(WebhookPayloadType.GymTrainer, new GymWithTrainer(gym, gymTrainer));
                            }
                        }
                        if (gymDefenderData.MotivatedPokemon != null)
                        {
                            var gymDefender = new GymDefender(gymDefenderData, fortId);
                            gymDefendersToUpsert.Add(gymDefender);

                            if (gym != null)
                            {
                                // Send webhook
                                await SendWebhookPayloadAsync(WebhookPayloadType.GymDefender, new GymWithDefender(gym, gymDefender));
                            }
                        }
                    }
                }

                if (gymsToUpsert.Count > 0)
                {
                    await context.Gyms.BulkInsertAsync(gymsToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            // Only update necessary columns
                            p.Id,
                            p.Name,
                            p.Url,
                        };
                    });

                    gymsToUpsert.ForEach(gym => _memCache.Set(gym.Id, gym));
                    //_logger.LogInformation($"Upserted {gymsToUpsert.Count:N0} Gym Information");
                }

                if (gymTrainersToUpsert.Count > 0)
                {
                    await context.GymTrainers.BulkInsertAsync(gymTrainersToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                    });

                    gymTrainersToUpsert.ForEach(trainer => _memCache.Set(trainer.Name, trainer));
                    _logger.LogInformation($"Upserted {gymTrainersToUpsert.Count:N0} Gym Trainers");
                }

                if (gymDefendersToUpsert.Count > 0)
                {
                    await context.GymDefenders.BulkInsertAsync(gymDefendersToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                    });

                    gymDefendersToUpsert.ForEach(defender => _memCache.Set(defender.Id, defender));
                    _logger.LogInformation($"Upserted {gymDefendersToUpsert.Count:N0} Gym Defenders");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateQuestsAsync(IEnumerable<dynamic> quests)
        //private async Task UpdateQuestsAsync(MapDbContext context, IEnumerable<dynamic> quests)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var questsToUpsert = new List<Pokestop>();

                // Convert quest protos to Pokestop models
                foreach (var quest in quests)
                {
                    var data = (QuestProto)quest.quest;
                    var title = quest.title;
                    var hasAr = quest.hasAr;
                    var fortId = data.FortId;

                    var pokestop = await context.Pokestops.FindAsync(fortId);
                    if (pokestop != null)
                    {
                        pokestop.AddQuest(title, data, hasAr);
                        var webhooks = await pokestop.UpdateAsync(context, updateQuest: true);
                        foreach (var webhook in webhooks)
                        {
                            var type = ConvertWebhookType(webhook.Key);
                            await SendWebhookPayloadAsync(type, webhook.Value);
                        }

                        if (pokestop.HasChanges && (pokestop.HasQuestChanges || pokestop.HasAlternativeQuestChanges))
                        {
                            questsToUpsert.Add(pokestop);
                        }
                    }
                }

                if (questsToUpsert.Count > 0)
                {
                    await context.Pokestops.BulkMergeAsync(questsToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        // Only include the following columns when updating
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.Id,
                            p.QuestType,
                            p.QuestTitle,
                            p.QuestTimestamp,
                            p.QuestTemplate,
                            p.QuestTarget,
                            p.QuestRewards,
                            p.QuestConditions,

                            p.AlternativeQuestType,
                            p.AlternativeQuestTitle,
                            p.AlternativeQuestTimestamp,
                            p.AlternativeQuestTemplate,
                            p.AlternativeQuestTarget,
                            p.AlternativeQuestRewards,
                            p.AlternativeQuestConditions,
                        };
                    });

                    questsToUpsert.ForEach(quest => _memCache.Set(quest.Id, quest));
                    //_logger.LogInformation($"Upserted {quests.Count:N0} Pokestop Quests");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateEncountersAsync(IEnumerable<dynamic> encounters)
        //private async Task UpdateEncountersAsync(MapDbContext context, IEnumerable<dynamic> encounters)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var timestampMs = now * 1000;

            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                //var leakMonitor = new ConnectionLeakWatcher(context);
                var pokemonToUpsert = new List<Pokemon>();
                var spawnpointsToUpsert = new List<Spawnpoint>();
                var s2cellsToUpsert = new List<Cell>();
                foreach (var encounter in encounters)
                {
                    try
                    {
                        var data = (EncounterOutProto)encounter.data;
                        var username = encounter.username;
                        var isEvent = encounter.isEvent;
                        var encounterId = data.Pokemon.EncounterId.ToString();
                        var pokemon = await context.Pokemon.FindAsync(encounterId);
                        if (pokemon == null)
                        {
                            // New Pokemon
                            var cellId = S2CellExtensions.S2CellIdFromLatLng(data.Pokemon.Latitude, data.Pokemon.Longitude);
                            if (!context.Cells.Any(cell => cell.Id == cellId.Id))
                            {
                                s2cellsToUpsert.Add(new Cell(cellId.Id));
                            }
                            pokemon = new Pokemon(data.Pokemon, cellId.Id, username, isEvent);
                        }
                        await pokemon.AddEncounterAsync(data, username);
                        var spawnpoint = await UpdateSpawnpointAsync(context, pokemon, data.Pokemon, timestampMs);
                        if (spawnpoint != null)
                        {
                            spawnpointsToUpsert.Add(spawnpoint);
                        }

                        if (pokemon.HasIvChanges)
                        {
                            SetPvpRankings(pokemon);
                        }
                        await pokemon.UpdateAsync(context, updateIv: true);
                        pokemonToUpsert.Add(pokemon);

                        if (pokemon.SendWebhook)
                        {
                            await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }
                }

                if (spawnpointsToUpsert.Count > 0)
                {
                    await context.Spawnpoints.BulkMergeAsync(spawnpointsToUpsert, options =>
                    {
                        options.UseTableLock = true;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.Id,
                            p.LastSeen,
                            p.Updated,
                            p.DespawnSecond,
                        };
                    });

                    spawnpointsToUpsert.ForEach(spawnpoint => _memCache.Set(spawnpoint.Id, spawnpoint));

                    _logger.LogInformation($"Upserted {spawnpointsToUpsert.Count:N0} Spawnpoints");
                }

                if (s2cellsToUpsert.Count > 0)
                {
                    await context.Cells.BulkMergeAsync(s2cellsToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.Updated,
                        };
                    });

                    foreach (var cell in s2cellsToUpsert)
                    {
                        _clearFortsService.AddCell(cell.Id);
                    }

                    s2cellsToUpsert.ForEach(s2cell => _memCache.Set(s2cell.Id, s2cell));
                }

                if (pokemonToUpsert.Count > 0)
                {
                    await context.Pokemon.BulkMergeAsync(pokemonToUpsert, options =>
                    {
                        options.UseTableLock = true;
                        // Only update IV specific columns
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.Id,
                            p.PokemonId,
                            p.Form,
                            p.Costume,
                            p.Gender,
                            p.AttackIV,
                            p.DefenseIV,
                            p.StaminaIV,
                            p.CP,
                            p.Level,
                            p.Size,
                            p.Weight,
                            p.Move1,
                            p.Move2,
                            p.Weather,
                            p.PvpRankings,
                        };
                    });

                    //_logger.LogInformation($"Upserted {pokemonToUpsert.Count:N0} Pokemon Encounters");
                    pokemonToUpsert.ForEach(pokemon => _memCache.Set(pokemon.Id, pokemon));

                    await SendPokemonAsync(pokemonToUpsert);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateDiskEncountersAsync(IEnumerable<dynamic> diskEncounters)
        //private async Task UpdateDiskEncountersAsync(MapDbContext context, IEnumerable<dynamic> diskEncounters)
        {
            try
            {
                //using var context = await _dbFactory.CreateDbContextAsync();
                using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                foreach (var diskEncounter in diskEncounters)
                {
                    var data = (DiskEncounterOutProto)diskEncounter.data;
                    var username = diskEncounter.username;
                    var isEvent = diskEncounter.isEvent;
                    var displayId = data.Pokemon.PokemonDisplay.DisplayId;
                    var pokemon = await context.Pokemon.FindAsync(displayId);
                    if (pokemon != null)
                    {
                        pokemon.AddDiskEncounter(data, username);
                        if (pokemon.HasIvChanges)
                        {
                            SetPvpRankings(pokemon);
                        }
                        await pokemon.UpdateAsync(context, updateIv: true);
                        pokemonToUpsert.Add(pokemon);

                        if (pokemon.SendWebhook)
                        {
                            await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                        }
                    }
                    else
                    {
                        _diskCache.Set(displayId, data, TimeSpan.FromMinutes(30));
                        _logger.LogInformation($"Disk encounter with id '{displayId}' added to cache");
                    }
                }

                if (pokemonToUpsert.Count > 0)
                {
                    await context.Pokemon.BulkMergeAsync(pokemonToUpsert, options =>
                    {
                        options.UseTableLock = true;
                        // Only update IV specific columns
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.Id,
                            p.PokemonId,
                            p.Form,
                            p.Costume,
                            p.Gender,
                            p.AttackIV,
                            p.DefenseIV,
                            p.StaminaIV,
                            p.CP,
                            p.Level,
                            p.Size,
                            p.Weight,
                            p.Move1,
                            p.Move2,
                            p.Weather,
                            p.PvpRankings,
                        };
                    });

                    //_logger.LogInformation($"Upserted {pokemonToUpsert.Count:N0} Disk Pokemon Encounters");
                    pokemonToUpsert.ForEach(pokemon => _memCache.Set(pokemon.Id, pokemon));

                    await SendPokemonAsync(pokemonToUpsert);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static async Task<Spawnpoint?> UpdateSpawnpointAsync(MapDbContext context, Pokemon pokemon, WildPokemonProto wild, ulong timestampMs)
        {
            var spawnId = pokemon.SpawnId ?? 0;
            if (spawnId == 0)
            {
                return null;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            if (wild.TimeTillHiddenMs <= 90000 && wild.TimeTillHiddenMs > 0)
            {
                pokemon.ExpireTimestamp = Convert.ToUInt64((timestampMs + Convert.ToUInt64(wild.TimeTillHiddenMs)) / 1000);
                pokemon.IsExpireTimestampVerified = true;
                var date = timestampMs.FromMilliseconds();
                var secondOfHour = date.Second + (date.Minute * 60);

                var spawnpoint = new Spawnpoint
                {
                    Id = spawnId,
                    Latitude = pokemon.Latitude,
                    Longitude = pokemon.Longitude,
                    DespawnSecond = Convert.ToUInt16(secondOfHour),
                    LastSeen = Pokemon.SaveSpawnpointLastSeen ? now : null,
                    Updated = now,
                };
                await spawnpoint.UpdateAsync(context, update: true);
                return spawnpoint;
            }
            else
            {
                pokemon.IsExpireTimestampVerified = false;
            }

            if (!pokemon.IsExpireTimestampVerified && spawnId > 0)
            {
                var spawnpoint = await context.Spawnpoints.FindAsync(pokemon.SpawnId);
                if (spawnpoint != null && spawnpoint.DespawnSecond != null)
                {
                    var despawnSecond = spawnpoint.DespawnSecond;
                    var timestampS = timestampMs / 1000;
                    var date = timestampS.FromMilliseconds();
                    var secondOfHour = date.Second + (date.Minute * 60);
                    var despawnOffset = despawnSecond - secondOfHour;
                    if (despawnSecond < secondOfHour)
                        despawnOffset += 3600;

                    // Update spawnpoint last_seen if enabled
                    if (Pokemon.SaveSpawnpointLastSeen)
                    {
                        spawnpoint.LastSeen = now;
                    }

                    pokemon.ExpireTimestamp = timestampS + (ulong)despawnOffset;
                    pokemon.IsExpireTimestampVerified = true;
                    return spawnpoint;
                }
                else
                {
                    var newSpawnpoint = new Spawnpoint
                    {
                        Id = spawnId,
                        Latitude = pokemon.Latitude,
                        Longitude = pokemon.Longitude,
                        DespawnSecond = null,
                        LastSeen = Pokemon.SaveSpawnpointLastSeen ? now : null,
                        Updated = now,
                    };
                    await newSpawnpoint.UpdateAsync(context, update: true);
                    return newSpawnpoint;
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        private void CheckQueueLength()
        {
            var usage = $"{_taskQueue.Count:N0}/{Strings.MaximumQueueCapacity:N0}";
            if (_taskQueue.Count == Strings.MaximumQueueCapacity)
            {
                _logger.LogWarning($"Data processing queue is at maximum capacity! {usage}");
            }
            else if (_taskQueue.Count > Strings.MaximumQueueSizeWarning)
            {
                _logger.LogWarning($"Data processing queue is over normal capacity with {usage} items total, consider increasing 'MaximumQueueBatchSize'");
            }
        }

        private static void SetPvpRankings(Pokemon pokemon)
        {
            var pokemonId = (HoloPokemonId)pokemon.PokemonId;
            PokemonForm? formId = pokemon.Form != null && pokemon.Form != 0
                ? (PokemonForm)(pokemon.Form ?? 0)
                : null;
            PokemonGender? genderId = pokemon.Gender != null && pokemon.Gender != 0
                ? (PokemonGender)(pokemon.Gender ?? 0)
                : null;
            var costumeId = (PokemonCostume)(pokemon.Costume ?? 0);
            var iv = new IV(pokemon.AttackIV ?? 0, pokemon.DefenseIV ?? 0, pokemon.StaminaIV ?? 0);
            var level = pokemon.Level ?? 0;
            var pvpRanks = PvpRankGenerator.Instance.GetAllPvpLeagues(
                pokemonId,
                formId,
                genderId,
                costumeId,
                iv,
                level
            );
            pokemon.PvpRankings = pvpRanks != null && pvpRanks.Count > 0
                ? (Dictionary<string, dynamic>)pvpRanks
                : null;
        }

        private static WebhookPayloadType ConvertWebhookType(WebhookType type)
        {
            return type switch
            {
                WebhookType.Pokemon => WebhookPayloadType.Pokemon,
                WebhookType.Pokestops => WebhookPayloadType.Pokestop,
                WebhookType.Lures => WebhookPayloadType.Lure,
                WebhookType.Invasions => WebhookPayloadType.Invasion,
                WebhookType.Quests => WebhookPayloadType.Quest,
                WebhookType.AlternativeQuests => WebhookPayloadType.AlternativeQuest,
                WebhookType.Gyms => WebhookPayloadType.Gym,
                WebhookType.GymInfo => WebhookPayloadType.GymInfo,
                WebhookType.GymDefenders => WebhookPayloadType.GymDefender,
                WebhookType.GymTrainers => WebhookPayloadType.GymTrainer,
                WebhookType.Eggs => WebhookPayloadType.Egg,
                WebhookType.Raids => WebhookPayloadType.Raid,
                WebhookType.Weather => WebhookPayloadType.Weather,
                WebhookType.Accounts => WebhookPayloadType.Account,
                _ => WebhookPayloadType.Pokemon,
            };
        }

        #endregion

        #region Grpc Senders

        private async Task SendWebhookPayloadAsync<T>(WebhookPayloadType webhookType, T entity)
        {
            if (entity == null)
            {
                _logger.LogWarning($"Unable to relay entity {typeof(T).Name} to webhook service, entity is null...");
                return;
            }

            var json = entity.ToJson();
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning($"Failed to serialize entity {typeof(T).Name} to relay to webhook service, skipping...");
                return;
            }

            await _grpcClientService.SendWebhookPayloadAsync(webhookType, json);
        }

        private async Task SendPokemonAsync(List<Pokemon> pokemon)
        {
            var newPokemon = pokemon.Where(pkmn => pkmn.IsNewPokemon).ToList();
            var newPokemonWithIV = pokemon.Where(pkmn => pkmn.IsNewPokemonWithIV).ToList();

            if (newPokemon.Count > 0)
            {
                // Send got Pokemon proto message
                await _grpcClientService.SendRpcPayloadAsync(newPokemon, PayloadType.PokemonList, hasIV: false);
            }

            if (newPokemonWithIV.Count > 0)
            {
                // Send got Pokemon IV proto message
                await _grpcClientService.SendRpcPayloadAsync(newPokemonWithIV, PayloadType.PokemonList, hasIV: true);
            }
        }

        private async Task SendGymsAsync(List<PokemonFortProto> forts, string username)
        {
            await _grpcClientService.SendRpcPayloadAsync(forts, PayloadType.FortList, username);
        }

        private async Task SendPlayerDataAsync(string username, ushort level, uint xp)
        {
            var payload = new
            {
                username,
                level,
                xp,
            };
            await _grpcClientService.SendRpcPayloadAsync(payload, PayloadType.PlayerInfo, username);
        }

        #endregion

        private static double BenchmarkAction(Action action, ushort precision = 4)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Start();

            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, precision);
            //Console.WriteLine($"Benchmark took {totalSeconds}s for {action.Method.Name} (Target: {action.Target})");
            return totalSeconds;
        }

        private static BulkOperation<T> GetBulkOptions<T>(Expression<Func<T, object>> keys) where T : BaseEntity
        {
            var options = new BulkOperation<T>
            {
                AllowDuplicateKeys = false,
                UseTableLock = true,
                OnMergeUpdateInputExpression = keys,
            };
            return options;
        }
    }

    /// <summary>
    ///     This class can help identify db connection leaks (connections that are not closed after use).
    /// Usage:
    ///     connection = new SqlConnection(..);
    ///     connection.Open()
    /// #if DEBUG
    ///     new ConnectionLeakWatcher(connection);
    /// #endif
    ///     That's it. Don't store a reference to the watcher. It will make itself available for garbage collection
    ///     once it has fulfilled its purpose. Watch the visual studio debug output for details on potentially leaked connections.
    ///     Note that a connection could possibly just be taking its time and may eventually be closed properly despite being flagged by this class.
    ///     So take the output with a pinch of salt.
    /// </summary>
    /// <credits>https://stackoverflow.com/a/15002420</credits>
    public class ConnectionLeakWatcher : IDisposable
    {
        private static int _idCounter = 0;
        private readonly int _connectionId = ++_idCounter;
        private readonly Timer? _timer;
        //Store reference to connection so we can unsubscribe from state change events
        //private SqlConnection? _connection;
        private System.Data.Common.DbConnection? _connection;

        public string StackTrace { get; set; }

        //public ConnectionLeakWatcher(SqlConnection connection)
        //public ConnectionLeakWatcher(System.Data.Common.DbConnection connection)
        public ConnectionLeakWatcher(DbContext context)
        {
            //_connection = connection;
            _connection = context.Database.GetDbConnection();
            StackTrace = Environment.StackTrace;

            _connection.StateChange += ConnectionOnStateChange;
            Debug.WriteLine($"Connection opened {_connectionId}");

            _timer = new Timer(_ =>
            {
                //The timeout expired without the connection being closed. Write to debug output the stack trace of the connection creation to assist in pinpointing the problem
                Debug.WriteLine("Suspected connection leak with origin: {0}{1}{0}Connection id: {2}", Environment.NewLine, StackTrace, _connectionId);
                //That's it - we're done. Clean up by calling Dispose.
                Dispose();
            }, null, 10000, Timeout.Infinite);
        }

        private void ConnectionOnStateChange(object sender, StateChangeEventArgs stateChangeEventArgs)
        {
            //Connection state changed. Was it closed?
            if (stateChangeEventArgs.CurrentState == ConnectionState.Closed)
            {
                //The connection was closed within the timeout
                Debug.WriteLine($"Connection closed {_connectionId}");
                //That's it - we're done. Clean up by calling Dispose.
                Dispose();
            }
        }

        #region Dispose

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (_isDisposed) return;

            _timer.Dispose();

            if (_connection != null)
            {
                _connection.StateChange -= ConnectionOnStateChange;
                _connection = null;
            }

            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        ~ConnectionLeakWatcher()
        {
            Dispose();
        }

        #endregion
    }
}