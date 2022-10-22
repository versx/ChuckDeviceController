namespace ChuckDeviceController.Services
{
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
    
    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.HostedServices;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Pvp;
    using ChuckDeviceController.Pvp.Models;
    using ChuckDeviceController.Services.Rpc;

    // TODO: Create stateless DataConsumer
    // TODO: Possibly create scoped DataProcessorService for each device uuid
    // TODO: Use/benchmark Dapper Micro ORM
    // TODO: Split up/refactor class
    // TODO: Add insert/upsertable entities into cache list to be upserted at x amount in list or if x amount of time has passed (if amount is not reached)?
    // TODO: Make data processor timed hosted service interval configurable

    // TODO: var leakMonitor = new ConnectionLeakWatcher(context);

    public class DataProcessorService : TimedHostedService, IDataProcessorService
    {
        #region Constants

        // TODO: Make DataProcessorService constants configurable
        private const uint ProcessIntervalMs = 5 * 1000;
        private const uint SemaphoreLockWaitTimeMs = 3 * 1000;
        private const int EntitySemMax = 3;
        private const ushort DefaultDecimals = 4;
        private const ushort CellScanIntervalS = 900;

        #endregion

        #region Variables

        private static readonly SemaphoreSlim _parsingSem = new(0, EntitySemMax);
        private static readonly object _cellLock = new();
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromMilliseconds(SemaphoreLockWaitTimeMs);

        private readonly ILogger<IDataProcessorService> _logger;
        private readonly IAsyncQueue<DataQueueItem> _taskQueue;
        private readonly IMemoryCache _diskCache;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IClearFortsHostedService _clearFortsService;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly IWebHostEnvironment _env;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDataConsumerService _dataConsumerService;

        #endregion

        #region Properties

        public ProcessingOptionsConfig Options { get; }

        public bool ShowBenchmarkTimes => _env?.IsDevelopment() ?? false;

        public override uint TimerIntervalMs => ProcessIntervalMs;

        #endregion

        #region Constructor

        public DataProcessorService(
            ILogger<IDataProcessorService> logger,
            IOptions<ProcessingOptionsConfig> options,
            IAsyncQueue<DataQueueItem> taskQueue,
            IMemoryCache diskCache,
            IGrpcClientService grpcClientService,
            IClearFortsHostedService clearFortsService,
            IMemoryCacheHostedService memCache,
            IWebHostEnvironment env,
            IServiceScopeFactory serviceScopeFactory,
            IDataConsumerService dataConsumerService)
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _dataConsumerService = dataConsumerService;
            _diskCache = diskCache;
            _grpcClientService = grpcClientService;
            _clearFortsService = clearFortsService;
            _memCache = memCache;
            _env = env;
            _serviceScopeFactory = serviceScopeFactory;

            Options = options.Value;
        }

        #endregion

        #region Background Service

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            var mapCell = new ClientMapCellProto();
            var gmo = new GetMapObjectsOutProto();
            gmo.MapCell.Add(mapCell);

            _logger.LogInformation(
                $"{nameof(IDataProcessorService)} is stopping.");

            await base.StopAsync(stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"{nameof(IDataProcessorService)} is now running in the background.");

            await Task.CompletedTask;
        }

        protected override async Task RunJobAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (_taskQueue.Count == 0)
                {
                    Thread.Sleep(1);
                    return;
                }

                var workItems = await _taskQueue.DequeueBulkAsync(Options.Queue.Data.MaximumBatchSize, stoppingToken);
                if (!workItems.Any())
                {
                    Thread.Sleep(1);
                    return;
                }

                // TODO: Filter data entities here and push to separate methods
                //Parallel.ForEach(workItems, async payload => await ProcessWorkItemAsync(payload, stoppingToken).ConfigureAwait(false));

                ProtoDataStatistics.Instance.TotalEntitiesProcessed += (uint)workItems.Sum(x => x.Data?.Count ?? 0);

                await Task.Run(() =>
                {
                    new Thread(async () =>
                    {
                        foreach (var workItem in workItems)
                        {
                            await Task.Factory.StartNew(async () => await ProcessWorkItemAsync(workItem, stoppingToken));
                        }
                    })
                    { IsBackground = true }.Start();
                }, stoppingToken);

                //await Task.Run(async () =>
                //{
                //    foreach (var workItem in workItems)
                //    {
                //        await Task.Factory.StartNew(async () => await ProcessWorkItemAsync(workItem, stoppingToken));
                //    }
                //}, stoppingToken);
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

        private async Task ProcessWorkItemAsync(DataQueueItem workItem, CancellationToken stoppingToken)
        {
            if (!(workItem.Data?.Any() ?? false))
            {
                return;
            }

            CheckQueueLength();

            ProtoDataStatistics.Instance.TotalEntitiesUpserted += (uint)workItem.Data.Count;

            await _parsingSem.WaitAsync(_semWaitTime, stoppingToken);

            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            using var scope = _serviceScopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<MapDbContext>();

            var playerData = workItem.Data
                .Where(x => x.type == ProtoDataType.PlayerData)
                .Select(x => x.data)
                .ToList();
            if (playerData.Any())
            {
                // Insert player account data
                await UpdatePlayerDataAsync(playerData);
            }

            var cells = workItem.Data
                .Where(x => x.type == ProtoDataType.Cell)
                .Select(x => (ulong)x.cell)
                .Distinct()
                .ToList();
            //var cells = FilterEntityData<ulong>("cell", workItem.Data, ProtoDataType.Cell);
            if (cells.Any())
            {
                // Insert S2 cells
                await UpdateCellsAsync(context, cells);
            }

            var clientWeather = workItem.Data
                .Where(x => x.type == ProtoDataType.ClientWeather)
                .Select(x => (ClientWeatherProto)x.data)
                .Distinct()
                .ToList();
            //var clientWeather = FilterEntityData<ClientWeatherProto>("data", workItem.Data, ProtoDataType.ClientWeather);
            if (clientWeather.Any())
            {
                // Insert weather cells
                await UpdateClientWeatherAsync(context, clientWeather);
            }

            var forts = workItem.Data
                .Where(x => x.type == ProtoDataType.Fort)
                .ToList();
            if (forts.Any())
            {
                // Insert Forts
                await UpdateFortsAsync(context, workItem.Username, forts);
            }

            var fortDetails = workItem.Data
                .Where(x => x.type == ProtoDataType.FortDetails)
                .ToList();
            if (fortDetails.Any())
            {
                // Insert Fort Details
                await UpdateFortDetailsAsync(context, fortDetails);
            }

            var gymInfos = workItem.Data
                .Where(x => x.type == ProtoDataType.GymInfo)
                .ToList();
            if (gymInfos.Any())
            {
                // Insert gym info
                await UpdateGymInfoAsync(context, gymInfos);
            }

            var wildPokemon = workItem.Data
                .Where(x => x.type == ProtoDataType.WildPokemon)
                .ToList();
            if (wildPokemon.Any())
            {
                // Insert wild pokemon
                await UpdateWildPokemonAsync(context, wildPokemon);
            }

            var nearbyPokemon = workItem.Data
                .Where(x => x.type == ProtoDataType.NearbyPokemon)
                .ToList();
            if (nearbyPokemon.Any())
            {
                // Insert nearby pokemon
                await UpdateNearbyPokemonAsync(context, nearbyPokemon);
             }

            var mapPokemon = workItem.Data
                .Where(x => x.type == ProtoDataType.MapPokemon)
                .ToList();
            if (mapPokemon.Any())
            {
                // Insert map pokemon
                await UpdateMapPokemonAsync(context, mapPokemon);
            }

            //if (wildPokemon.Any() || nearbyPokemon.Any() || mapPokemon.Any())
            //{
            //    await UpdatePokemonAsync(wildPokemon, nearbyPokemon, mapPokemon);
            //}

            var quests = workItem.Data
                .Where(x => x.type == ProtoDataType.Quest)
                .ToList();
            if (quests.Any())
            {
                // Insert quests
                await UpdateQuestsAsync(context, quests);
            }

            var encounters = workItem.Data
                .Where(x => x.type == ProtoDataType.Encounter)
                .ToList();
            if (encounters.Any())
            {
                // Insert Pokemon encounters
                await UpdateEncountersAsync(context, encounters);
            }

            var diskEncounters = workItem.Data
                .Where(x => x.type == ProtoDataType.DiskEncounter)
                .ToList();
            if (diskEncounters.Any())
            {
                // Insert lured/disk Pokemon encounters
                await UpdateDiskEncountersAsync(context, diskEncounters);
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, DefaultDecimals);
                //_logger.LogInformation($"Processed {workItem.Data.Count:N0} data entities in {totalSeconds}s to upsert...");
                //PrintBenchmarkTimes(DataLogLevel.Summary, workItem.Data, "total entities", sw);
            }

            _parsingSem.Release();
        }

        #endregion

        #region Data Handling Methods

        private async Task UpdatePlayerDataAsync(IEnumerable<dynamic> playerData)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                using var scope = _serviceScopeFactory.CreateAsyncScope();
                using var context = scope.ServiceProvider.GetRequiredService<ControllerDbContext>();

                foreach (var player in playerData)
                {
                    var username = player.username;
                    var data = (GetPlayerOutProto)player.gpr;
                    var account = await GetAccountEntity(context, username);
                    if (account == null)
                    {
                        // Failed to retrieve account by username from cache and database
                        continue;
                    }

                    await account.UpdateAsync(context, player, _memCache);
                    await _dataConsumerService.AddAccountAsync(BulkOptions.AccountOnMergeUpdate, account);

                    if (account.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Account, account);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdatePlayerDataAsync: {ex}");
            }
        }

        private async Task UpdateCellsAsync(MapDbContext context, IEnumerable<ulong> cells)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                lock (_cellLock)
                {
                    var now = DateTime.UtcNow.ToTotalSeconds();
                    // Convert cell ids to Cell models
                    var s2cells = new List<Cell>();
                    foreach (var cellId in cells)
                    {
                        var isCached = _memCache.IsSet<ulong, Cell>(cellId);
                        var cached = GetEntity<ulong, Cell>(context, cellId).Result;
                        if (!isCached || cached == null)
                        {
                            cached = new Cell(cellId);
                            _memCache.Set(cellId, cached);
                        }
                        var needsUpdate = cached.Updated > now - CellScanIntervalS;
                        if (!needsUpdate)
                            continue;

                        s2cells.Add(cached);
                    }

                    //var s2cells = cells
                    //    .Select(cellId => GetEntity<ulong, Cell>(context, cellId).Result ?? new Cell(cellId))
                    //    // Filter cells not already cached, cached cells expire every 60 minutes.
                    //    // Once expired they will be updated when found again.
                    //    .Where(cell => !_memCache.IsSet<ulong, Cell>(cell.Id) || cell.Updated > now - CellScanIntervalS)
                    //    .ToList();

                    // Check if any new/need to be updated cells, otherwise skip
                    if (!s2cells.Any())
                        return;

                    /*
                    var cellsSql = s2cells.Select(cell => $"({cell.Id}, {cell.Level}, {cell.Latitude}, {cell.Longitude}, {ts})");
                    var args = string.Join(",", cellsSql);
                    var sql = string.Format(SqlQueries.S2Cells, args);
                    var result = await context.Database.ExecuteSqlRawAsync(sql);
                    */
                    // TODO: Check why result value returns double of entities amount it updated

                    _dataConsumerService.AddCellsAsync(BulkOptions.CellOnMergeUpdate, s2cells)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

                    //PrintBenchmarkTimes(DataLogLevel.S2Cells, s2cells, "S2 Cells", sw);

                    //foreach (var cell in s2cells)
                    for (var i = 0; i < s2cells.Count; i++)
                    {
                        try
                        {
                            var cell = s2cells[i];
                            //if (cell == null)
                            //    continue; // TODO: Remove temp

                            // Cache all S2 cell entities in memory cache
                            _memCache.Set(cell.Id, cell);

                            // Add S2 cells to ClearFortsHostedService
                            _clearFortsService.AddCell(cell.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateCellsAsync: {ex}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }

            await Task.CompletedTask;
        }

        private async Task UpdateClientWeatherAsync(MapDbContext context, IEnumerable<ClientWeatherProto> clientWeather)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                // Convert weather protos to Weather models
                var weather = clientWeather
                    // Filter cells not already cached
                    .Where(wcell => !_memCache.IsSet<long, Weather>(wcell.S2CellId))
                    // Instantiate new Weather entity models for new weather cells not cached
                    .Select(wcell => new Weather(wcell))
                    .ToList();

                // Check if any new/need to be updated weather cells, otherwise skip
                if (!weather.Any())
                    return;

                foreach (var wcell in weather)
                {
                    await wcell.UpdateAsync(context, _memCache);
                    await _dataConsumerService.AddWeatherAsync(BulkOptions.WeatherOnMergeUpdate, wcell);

                    // Check if 'SendWebhook' flag was triggered, if so relay webhook payload to communicator
                    if (wcell.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Weather, wcell);
                    }
                }

                /*
                var weatherSql = weather.Select(wcell =>
                {
                    var list = new List<object>
                    {
                        wcell.Id,
                        wcell.Level,
                        wcell.Latitude,
                        wcell.Longitude,
                        Convert.ToInt32(wcell.GameplayCondition),
                        wcell.CloudLevel,
                        wcell.RainLevel,
                        wcell.SnowLevel,
                        wcell.FogLevel,
                        wcell.WindLevel,
                        wcell.WindDirection,
                        wcell.WarnWeather ?? false,
                        wcell.SpecialEffectLevel,
                        wcell.Severity ?? 0,
                        wcell.Updated,
                    };
                    return "(" + string.Join(",", list) + ")";
                });
                var args = string.Join(",", weatherSql);
                var sql = string.Format(SqlQueries.WeatherCells, args);

                var result = await context.Database.ExecuteSqlRawAsync(sql);
                */
                //PrintBenchmarkTimes(DataLogLevel.Weather, weather, "Weather Cells", sw);
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateClientWeatherAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateWildPokemonAsync(MapDbContext context, IEnumerable<dynamic> wildPokemon)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                foreach (var wild in wildPokemon)
                {
                    var cellId = wild.cell;
                    var data = (WildPokemonProto)wild.data;
                    var timestampMs = wild.timestampMs;
                    var username = wild.username;
                    var isEvent = wild.isEvent;
                    var pokemon = new Pokemon(data, cellId, username, isEvent); // TODO: Get entity from cache
                    var spawnpoint = await ParseSpawnpointAsync(context, pokemon, data.TimeTillHiddenMs, timestampMs);
                    if (spawnpoint != null)
                    {
                        await _dataConsumerService.AddSpawnpointAsync(BulkOptions.SpawnpointOnMergeUpdate, spawnpoint);
                    }

                    await pokemon.UpdateAsync(context, _memCache, updateIv: false);
                    await _dataConsumerService.AddPokemonAsync(BulkOptions.PokemonIgnoreOnMerge, pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }

                //PrintBenchmarkTimes(DataLogLevel.WildPokemon, pokemonToUpsert, "Wild Pokemon", sw);
                // NOTE: Used for testing Pokemon database event triggers
                //foreach (var pokemon in pokemonToUpsert)
                //{
                //    if (context.Pokemon.Any(pkmn => pkmn.Id == pokemon.Id))
                //    {
                //        context.Update(pokemon);
                //    }
                //    else
                //    {
                //        await context.AddAsync(pokemon);
                //    }
                //}
                //await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateWildPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateNearbyPokemonAsync(MapDbContext context, IEnumerable<dynamic> nearbyPokemon)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                foreach (var nearby in nearbyPokemon)
                {
                    var cellId = nearby.cell;
                    var data = (NearbyPokemonProto)nearby.data;
                    var username = nearby.username;
                    var isEvent = nearby.isEvent;
                    var pokemon = new Pokemon(context, data, cellId, username, isEvent); // TODO: Get entity from cache

                    await pokemon.UpdateAsync(context, _memCache, updateIv: false);
                    await _dataConsumerService.AddPokemonAsync(BulkOptions.PokemonIgnoreOnMerge, pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateNearbyPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateMapPokemonAsync(MapDbContext context, IEnumerable<dynamic> mapPokemon)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                foreach (var map in mapPokemon)
                {
                    var cellId = map.cell;
                    var data = (MapPokemonProto)map.data;
                    var username = map.username;
                    var isEvent = map.isEvent;
                    var pokemon = new Pokemon(context, data, cellId, username, isEvent); // TODO: Get entity from cache
                    await pokemon.UpdateAsync(context, _memCache, updateIv: false);

                    // Check if we have a pending disk encounter cache
                    var displayId = data.PokemonDisplay.DisplayId;
                    var cachedDiskEncounter = _diskCache.Get<DiskEncounterOutProto>(displayId);
                    if (cachedDiskEncounter != null)
                    {
                        // Thanks Fabio <3
                        _logger.LogDebug($"Found Pokemon disk encounter with id '{displayId}' in cache");

                        pokemon.AddDiskEncounter(cachedDiskEncounter, username);
                        await pokemon.UpdateAsync(context, _memCache, updateIv: true);
                    }
                    else
                    {
                        // Failed to get DiskEncounter from cache
                        _logger.LogWarning($"Unable to fetch cached Pokemon disk encounter with id '{displayId}' from cache");
                    }

                    await _dataConsumerService.AddPokemonAsync(BulkOptions.PokemonOptions, pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateMapPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        // TODO: Separate Pokestop & Gym fort parsing methods
        private async Task UpdateFortsAsync(MapDbContext context, string? username, IEnumerable<dynamic> forts)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            // Send found/nearby forts with gRPC service for leveling instance
            var lvlForts = forts
                .Where(fort => fort.data.FortType != FortType.Gym)
                .Select(fort => fort.data)
                .Select(fort => (PokemonFortProto)fort)
                .ToList();
            // Ensure that the account username is set, otherwise ignore relaying
            // fort data for leveling instance
            if (!string.IsNullOrEmpty(username) && lvlForts.Any())
            {
                await SendGymsAsync(lvlForts, username);
            }

            try
            {
                var fortsPokestops = forts
                    .Where(fort => fort.data.FortType == FortType.Checkpoint)
                    .ToList();
                // Convert fort protos to Pokestop models
                foreach (var fort in fortsPokestops)
                {
                    var cellId = (ulong)fort.cell;
                    var data = (PokemonFortProto)fort.data;
                    //var username = (string)fort.username;

                    // Init Pokestop model from fort proto data
                    var pokestop = new Pokestop(data, cellId);
                    var pokestopWebhooks = await pokestop.UpdateAsync(context, _memCache, updateQuest: false);
                    if (pokestopWebhooks.Any())
                    {
                        foreach (var webhook in pokestopWebhooks)
                        {
                            var type = ConvertWebhookType(webhook.Key);
                            await SendWebhookPayloadAsync(type, webhook.Value);
                        }
                    }

                    await _dataConsumerService.AddPokestopAsync(BulkOptions.PokestopIgnoreOnMerge, pokestop);

                    try
                    {
                        // Loop incidents
                        if ((pokestop.Incidents?.Count ?? 0) > 0)
                        {
                            foreach (var incident in pokestop!.Incidents!)
                            {
                                await incident.UpdateAsync(context, _memCache);
                                if (incident.SendWebhook)
                                {
                                    await SendWebhookPayloadAsync(WebhookPayloadType.Invasion, new PokestopWithIncident(pokestop, incident));
                                }
                            }

                            await _dataConsumerService.AddIncidentsAsync(BulkOptions.IncidentOptions, pokestop.Incidents);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"UpdateFortsAsync[Incident]: {ex.InnerException?.Message ?? ex.Message}");
                    }

                    _clearFortsService.AddPokestop(cellId, data.FortId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFortsAsync[Pokestop]: {ex.InnerException?.Message ?? ex.Message}");
            }

            try
            {
                var fortsGyms = forts
                    .Where(fort => fort.data.FortType == FortType.Gym)
                    .ToList();
                // Convert fort protos to Gym models
                foreach (var fort in fortsGyms)
                {
                    var cellId = (ulong)fort.cell;
                    var data = (PokemonFortProto)fort.data;
                    //var username = (string)fort.username;

                    // Init Gym model from fort proto data
                    var gym = new Gym(data, cellId);
                    var gymWebhooks = await gym.UpdateAsync(context, _memCache);
                    if (gymWebhooks.Any())
                    {
                        foreach (var webhook in gymWebhooks)
                        {
                            var type = ConvertWebhookType(webhook.Key);
                            await SendWebhookPayloadAsync(type, webhook.Value);
                        }
                    }

                    await _dataConsumerService.AddGymAsync(BulkOptions.GymOptions, gym);

                    _clearFortsService.AddGym(cellId, data.FortId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFortsAsync[Gym]: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateFortDetailsAsync(MapDbContext context, IEnumerable<dynamic> fortDetails)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                //await _pokestopsSem.WaitAsync(_semWaitTime);

                var fortDetailsPokestops = fortDetails
                    .Where(fort => fort.data.FortType == FortType.Checkpoint)
                    .ToList();
                // Convert fort details protos to Pokestop models
                foreach (var fort in fortDetailsPokestops)
                {
                    var data = (FortDetailsOutProto)fort.data;
                    var pokestop = await GetEntity<string, Pokestop>(context, data.Id);
                    if (pokestop == null)
                        continue;

                    pokestop.AddDetails(data);
                    var pokestopWebhooks = await pokestop.UpdateAsync(context, _memCache);
                    foreach (var webhook in pokestopWebhooks)
                    {
                        var type = ConvertWebhookType(webhook.Key);
                        await SendWebhookPayloadAsync(type, pokestop);
                    }
                    if (pokestop.HasChanges)
                    {
                        await _dataConsumerService.AddPokestopAsync(BulkOptions.PokestopDetailsOnMergeUpdate, pokestop);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFortDetailsAsync[Pokestop]: {ex.InnerException?.Message ?? ex.Message}");
            }

            try
            {
                var fortDetailsGyms = fortDetails
                    .Where(fort => fort.data.FortType == FortType.Gym)
                    .ToList();
                // Convert fort details protos to Gym models
                foreach (var fort in fortDetailsGyms)
                {
                    var data = (FortDetailsOutProto)fort.data;
                    var gym = await GetEntity<string, Gym>(context, data.Id);
                    if (gym == null)
                        continue;

                    gym.AddDetails(data);
                    var gymWebhooks = await gym.UpdateAsync(context, _memCache);
                    foreach (var webhook in gymWebhooks)
                    {
                        var type = ConvertWebhookType(webhook.Key);
                        await SendWebhookPayloadAsync(type, gym);
                    }
                    if (gym.HasChanges)
                    {
                        await _dataConsumerService.AddGymAsync(BulkOptions.GymDetailsOnMergeUpdate, gym);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFortDetailsAsync[Gym]: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateGymInfoAsync(MapDbContext context, IEnumerable<dynamic> gymInfos)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                // Convert gym info protos to Gym models
                foreach (var gymInfo in gymInfos)
                {
                    var data = (GymGetInfoOutProto)gymInfo.data;
                    var fortId = data.GymStatusAndDefenders.PokemonFortProto.FortId;
                    var gym = await GetEntity<string, Gym>(context, fortId);
                    if (gym == null)
                        continue;

                    gym.AddDetails(data);
                    var webhooks = await gym.UpdateAsync(context, _memCache);
                    foreach (var webhook in webhooks)
                    {
                        var type = ConvertWebhookType(webhook.Key);
                        await SendWebhookPayloadAsync(type, gym);
                    }
                    if (gym.HasChanges)
                    {
                        await _dataConsumerService.AddGymAsync(BulkOptions.GymDetailsOnMergeUpdate, gym);
                    }

                    var gymDefenders = data.GymStatusAndDefenders.GymDefender;
                    if (gymDefenders == null)
                        continue;

                    foreach (var gymDefenderData in gymDefenders)
                    {
                        if (gymDefenderData.TrainerPublicProfile != null)
                        {
                            var gymTrainer = new GymTrainer(gymDefenderData.TrainerPublicProfile);
                            await _dataConsumerService.AddGymTrainerAsync(BulkOptions.GymTrainerOptions, gymTrainer);

                            // Send webhook
                            await SendWebhookPayloadAsync(WebhookPayloadType.GymTrainer, new GymWithTrainer(gym, gymTrainer));
                        }
                        if (gymDefenderData.MotivatedPokemon != null)
                        {
                            var gymDefender = new GymDefender(gymDefenderData, fortId);
                            await _dataConsumerService.AddGymDefenderAsync(BulkOptions.GymDefenderOptions, gymDefender);

                            // Send webhook
                            await SendWebhookPayloadAsync(WebhookPayloadType.GymDefender, new GymWithDefender(gym, gymDefender));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateQuestsAsync(MapDbContext context, IEnumerable<dynamic> quests)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                // Convert quest protos to Pokestop models
                foreach (var quest in quests)
                {
                    var data = (QuestProto)quest.quest;
                    var title = quest.title;
                    var hasAr = quest.hasAr;
                    var fortId = data.FortId;
                    var pokestop = await GetEntity<string, Pokestop>(context, fortId);
                    if (pokestop == null)
                        continue;

                    pokestop.AddQuest(title, data, hasAr);
                    var webhooks = await pokestop.UpdateAsync(context, _memCache, updateQuest: true);
                    foreach (var webhook in webhooks)
                    {
                        var type = ConvertWebhookType(webhook.Key);
                        await SendWebhookPayloadAsync(type, webhook.Value);
                    }

                    if (pokestop.HasChanges && (pokestop.HasQuestChanges || pokestop.HasAlternativeQuestChanges))
                    {
                        await _dataConsumerService.AddPokestopAsync(BulkOptions.PokestopIgnoreOnMerge, pokestop);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateQuestsAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateEncountersAsync(MapDbContext context, IEnumerable<dynamic> encounters)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var timestampMs = now * 1000;
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                foreach (var encounter in encounters)
                {
                    try
                    {
                        var data = (EncounterOutProto)encounter.data;
                        var username = encounter.username;
                        var isEvent = encounter.isEvent;
                        var encounterId = data.Pokemon.EncounterId.ToString();
                        var pokemon = await GetEntity<string, Pokemon>(context, encounterId);
                        if (pokemon == null)
                        {
                            // New Pokemon
                            var cellId = S2CellExtensions.S2CellIdFromLatLng(data.Pokemon.Latitude, data.Pokemon.Longitude);
                            await UpdateCellsAsync(context, new[] { cellId.Id });

                            pokemon = new Pokemon(data.Pokemon, cellId.Id, username, isEvent);
                        }
                        await pokemon.AddEncounterAsync(data, username);

                        var spawnpoint = await ParseSpawnpointAsync(context, pokemon, data.Pokemon.TimeTillHiddenMs, timestampMs);
                        if (spawnpoint != null)
                        {
                            await _dataConsumerService.AddSpawnpointAsync(BulkOptions.SpawnpointOnMergeUpdate, spawnpoint);
                        }

                        if (pokemon.HasIvChanges)
                        {
                            SetPvpRankings(pokemon);
                        }

                        await pokemon.UpdateAsync(context, _memCache, updateIv: true);
                        await _dataConsumerService.AddPokemonAsync(BulkOptions.PokemonOnMergeUpdate, pokemon);

                        if (pokemon.SendWebhook)
                        {
                            await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        private async Task UpdateDiskEncountersAsync(MapDbContext context, IEnumerable<dynamic> diskEncounters)
        {
            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            try
            {
                foreach (var diskEncounter in diskEncounters)
                {
                    var data = (DiskEncounterOutProto)diskEncounter.data;
                    var username = diskEncounter.username;
                    var isEvent = diskEncounter.isEvent;
                    var displayId = Convert.ToString(data.Pokemon.PokemonDisplay.DisplayId);
                    var pokemon = await GetEntity<string, Pokemon>(context, displayId);
                    if (pokemon == null)
                    {
                        _diskCache.Set(displayId, data, TimeSpan.FromMinutes(30));
                        _logger.LogInformation($"Disk encounter with id '{displayId}' added to cache");
                        continue;
                    }

                    pokemon.AddDiskEncounter(data, username);
                    if (pokemon.HasIvChanges)
                    {
                        SetPvpRankings(pokemon);
                    }

                    await pokemon.UpdateAsync(context, _memCache, updateIv: true);
                    await _dataConsumerService.AddPokemonAsync(BulkOptions.PokemonOnMergeUpdate, pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
            }
        }

        #endregion

        #region Private Methods

        private void CheckQueueLength()
        {
            var usage = $"{_taskQueue.Count:N0}/{Options.Queue.Data.MaximumCapacity:N0}";
            if (_taskQueue.Count >= Options.Queue.Data.MaximumCapacity)
            {
                _logger.LogError($"Data processing queue is at maximum capacity! {usage}");
            }
            else if (_taskQueue.Count >= Options.Queue.Data.MaximumSizeWarning)
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

        private async Task<Spawnpoint?> ParseSpawnpointAsync(MapDbContext context, Pokemon pokemon, int timeTillHiddenMs, ulong timestampMs)
        {
            var spawnId = pokemon.SpawnId ?? 0;
            if (spawnId == 0)
            {
                return null;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            if (timeTillHiddenMs <= 90000 && timeTillHiddenMs > 0)
            {
                pokemon.ExpireTimestamp = Convert.ToUInt64((timestampMs + Convert.ToUInt64(timeTillHiddenMs)) / 1000);
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
                await spawnpoint.UpdateAsync(context, _memCache, update: true);
                return spawnpoint;
            }
            else
            {
                pokemon.IsExpireTimestampVerified = false;
            }

            if (!pokemon.IsExpireTimestampVerified && spawnId > 0)
            {
                //var spawnpoint = await context.Spawnpoints.FindAsync(pokemon.SpawnId);
                //var spawnpoint = await (
                //    from spawn in context.Spawnpoints
                //    where spawn.Id == spawnId
                //    select spawn
                //).FirstOrDefaultAsync();
                var spawnpoint = await GetEntity<ulong, Spawnpoint>(context, pokemon.SpawnId ?? 0);
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
                    await newSpawnpoint.UpdateAsync(context, _memCache, update: true);
                    return newSpawnpoint;
                }
            }

            return null;
        }

        private async Task<TEntity?> GetEntity<TKey, TEntity>(MapDbContext context, TKey key)
            where TEntity : BaseEntity
        {
            TEntity? GetFromCache(TKey key)
            {
                TEntity? entity = default;
                try
                {
                    entity = _memCache.Get<TKey, TEntity>(key);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GetEntity: {ex.InnerException?.Message ?? ex.Message}");
                }
                return entity;
            }
            async Task<TEntity?> GetFromDatabase(TKey key)
            {
                TEntity? entity = default;
                try
                {
                    entity = await context.FindAsync<TEntity>(key);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GetEntity: {ex.InnerException?.Message ?? ex.Message}");
                }
                return entity;
            }

            var entity = GetFromCache(key) ?? await GetFromDatabase(key);
            return entity;
        }

        private async Task<Account?> GetAccountEntity(ControllerDbContext context, string key)
        {
            var entity = _memCache.Get<string, Account>(key);
            if (entity == null)
            {
                entity = await context.Accounts.FindAsync(key);
            }
            return entity;
        }

        private static List<TEntity> FilterEntityData<TEntity>(string property, List<dynamic> data, ProtoDataType type)
        {
            var entities = data
                .Where(x => x.type == type)
                .Select(x => (TEntity)x.GetType().GetProperty(property).GetValue(x, null))
                .Distinct()
                .ToList();
            return entities;
        }

        private void PrintBenchmarkTimes(DataLogLevel logLevel, IReadOnlyList<object> entities, string text = "total entities", Stopwatch? sw = null)
        {
            if (!Options.IsEnabled(logLevel))
                return;

            var time = string.Empty;
            if (ShowBenchmarkTimes)
            {
                sw?.Stop();
                var totalSeconds = Math.Round(sw?.Elapsed.TotalSeconds ?? 0, 5).ToString("F5");
                time = sw != null
                    ? $" in {totalSeconds}s"
                    : string.Empty;
            }
            _logger.LogInformation($"{nameof(DataProcessorService)} upserted {entities.Count:N0} {text}{time}");
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

            // Fire off gRPC request on a separate thread
            await Task.Run(async () =>
            {
                await _grpcClientService.SendWebhookPayloadAsync(webhookType, json);
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        private async Task SendPokemonAsync(List<Pokemon> pokemon)
        {
            var newPokemon = pokemon
                .Where(pkmn => pkmn.IsNewPokemon)
                .ToList();
            var newPokemonWithIV = pokemon
                .Where(pkmn => pkmn.IsNewPokemonWithIV)
                .ToList();

            if (newPokemon.Any())
            {
                // Fire off gRPC request on a separate thread
                await Task.Run(async () =>
                {
                    // Send got Pokemon proto message
                    await _grpcClientService.SendRpcPayloadAsync(
                        newPokemon,
                        PayloadType.PokemonList,
                        hasIV: false
                    );
                    await Task.CompletedTask;
                });
            }

            if (newPokemonWithIV.Any())
            {
                // Fire off gRPC request on a separate thread
                await Task.Run(async () =>
                {
                    // Send got Pokemon IV proto message
                    await _grpcClientService.SendRpcPayloadAsync(
                        newPokemonWithIV,
                        PayloadType.PokemonList,
                        hasIV: true
                    );
                    await Task.CompletedTask;
                });
            }

            await Task.CompletedTask;
        }

        private async Task SendGymsAsync(List<PokemonFortProto> forts, string username)
        {
            // Fire off gRPC request on a separate thread
            await Task.Run(async () =>
            {
                await _grpcClientService.SendRpcPayloadAsync(forts, PayloadType.FortList, username);
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        private async Task SendPlayerDataAsync(string username, ushort level, uint xp)
        {
            // Fire off gRPC request on a separate thread
            await Task.Run(async () =>
            {
                var payload = new { username, level, xp };
                await _grpcClientService.SendRpcPayloadAsync(payload, PayloadType.PlayerInfo, username);
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        #endregion
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
        private DbConnection? _connection;

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