namespace ChuckDeviceController.Services
{
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using MySqlConnector;
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
    
    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.HostedServices;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Pvp;
    using ChuckDeviceController.Pvp.Models;
    using ChuckDeviceController.Services.Rpc;

    // TODO: Add webhook sender service - add webhook items to queue
    // TODO: Split up/refactor class

    public class DataProcessorService : TimedHostedService, IDataProcessorService
    {
        #region Constants

        private const uint ProcessIntervalS = 5;
        private const ushort DefaultDecimals = 4;
        private const ushort CellScanIntervalS = 900; // REVIEW: Change to every 5 minutes vs 15 minutes
        private const ushort WeatherCellScanIntervalS = CellScanIntervalS * 2; // Update every 30 minutes

        #endregion

        #region Variables

        private static readonly SemaphoreSlim _parsingSem = new(5, 5); //new(1, 1);
        private static readonly object _cellLock = new();

        private readonly ILogger<IDataProcessorService> _logger;
        private readonly IAsyncQueue<DataQueueItem> _taskQueue;
        private readonly IMemoryCache _diskCache;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IClearFortsHostedService _clearFortsService;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly IWebHostEnvironment _env;
        private readonly IDataConsumerService _dataConsumerService;
        private readonly TimeSpan _diskCacheExpiry = TimeSpan.FromMinutes(30);
        //private readonly ThreadManager _threadManager = new(maxThreadCount: 25);

        #endregion

        #region Properties

        public DataProcessorOptionsConfig Options { get; }

        public bool ShowBenchmarkTimes => _env?.IsDevelopment() ?? false;

        #endregion

        #region Constructor

        public DataProcessorService(
            ILogger<IDataProcessorService> logger,
            IOptions<DataProcessorOptionsConfig> options,
            IAsyncQueue<DataQueueItem> taskQueue,
            IMemoryCache diskCache,
            IGrpcClientService grpcClientService,
            IClearFortsHostedService clearFortsService,
            IMemoryCacheHostedService memCache,
            IWebHostEnvironment env,
            IDataConsumerService dataConsumerService)
            : base(logger, options?.Value?.IntervalS ?? DataProcessorOptionsConfig.DefaultIntervalS)
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _dataConsumerService = dataConsumerService;
            _diskCache = diskCache;
            _grpcClientService = grpcClientService;
            _clearFortsService = clearFortsService;
            _memCache = memCache;
            _env = env;

            //connection = EntityRepository.CreateConnectionAsync().Result;

            Options = options?.Value ?? new();
        }

        #endregion

        #region Background Service

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

                var workItems = await _taskQueue.DequeueBulkAsync(Options.Queue.MaximumBatchSize, stoppingToken);
                if (!(workItems?.Any() ?? false))
                {
                    Thread.Sleep(1);
                    return;
                }

                var items = DataItems.ParseWorkItems(workItems);
                //await ProcessWorkItemAsync(items, stoppingToken);
                new Thread(async () =>
                {
                    using var connection = await EntityRepository.CreateConnectionAsync(stoppingToken: stoppingToken);
                    _ = new ConnectionLeakWatcher(connection, connectionTimeoutS: 60);
                    await ProcessWorkItemAsync(connection, items, stoppingToken);
                })
                { IsBackground = true }.Start();
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }

            Thread.Sleep(1);
        }

        private async Task ProcessWorkItemAsync(MySqlConnection connection, DataItems workItem, CancellationToken stoppingToken = default)
        {
            await _parsingSem.WaitAsync(stoppingToken);

            if (workItem == null)
                return;

            CheckQueueLength();

            //MySqlConnection connection = null;
            //using var connection = await EntityRepository.CreateConnectionAsync(stoppingToken: stoppingToken);
            //if (connection == null)
            //{
            //    _logger.LogError($"Failed to connect to database");
            //    return;
            //}

            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            ProtoDataStatistics.Instance.TotalPlayerDataReceived += (uint)workItem.PlayerData.Count();
            ProtoDataStatistics.Instance.TotalS2CellsReceived += (uint)workItem.Cells.Count();
            ProtoDataStatistics.Instance.TotalClientWeatherCellsReceived += (uint)workItem.Weather.Count();
            ProtoDataStatistics.Instance.TotalFortsReceived += (uint)workItem.Forts.Count();
            ProtoDataStatistics.Instance.TotalFortDetailsReceived += (uint)workItem.FortDetails.Count();
            ProtoDataStatistics.Instance.TotalGymInfoReceived += (uint)workItem.GymInfo.Count();
            ProtoDataStatistics.Instance.TotalWildPokemonReceived += (uint)workItem.WildPokemon.Count();
            ProtoDataStatistics.Instance.TotalNearbyPokemonReceived += (uint)workItem.NearbyPokemon.Count();
            ProtoDataStatistics.Instance.TotalMapPokemonReceived += (uint)workItem.MapPokemon.Count();
            ProtoDataStatistics.Instance.TotalQuestsReceived += (uint)workItem.Quests.Count();
            ProtoDataStatistics.Instance.TotalPokemonEncountersReceived += (uint)workItem.Encounters.Count();
            ProtoDataStatistics.Instance.TotalPokemonDiskEncountersReceived += (uint)workItem.DiskEncounters.Count();

            try
            {
                var guid = Guid.NewGuid().ToString()[..8];

                // Parse player account data
                if (Options.ProcessPlayerData && workItem.PlayerData.Any())
                {
                    await UpdatePlayerDataAsync(guid, connection, workItem.PlayerData);
                    //new Thread(async () => await UpdatePlayerDataAsync(guid, connection, workItem.PlayerData))
                    //{ IsBackground = true }.Start();
                }

                // Parse S2 cells
                if (Options.ProcessCells && workItem.Cells.Any())
                {
                    await UpdateCellsAsync(guid, workItem.Cells);
                    //new Thread(async () => await UpdateCellsAsync(guid, workItem.Cells))
                    //{ IsBackground = true }.Start();
                }

                // Parse weather cells
                if (Options.ProcessWeather && workItem.Weather.Any())
                {
                    await UpdateClientWeatherAsync(guid, connection, workItem.Weather);
                    //new Thread(async () => await UpdateClientWeatherAsync(guid, connection, workItem.Weather))
                    //{ IsBackground = true }.Start();
                }

                // Parse Pokestop and Gym forts
                if (Options.ProcessForts && workItem.Forts.Any())
                {
                    // TODO: Get username
                    await UpdateFortsAsync(guid, connection, workItem.Forts, "");
                    //new Thread(async () => await UpdateFortsAsync(guid, connection, workItem.Forts, ""))
                    //{ IsBackground = true }.Start();
                }

                // Parse Fort Details
                if (Options.ProcessFortDetails && workItem.FortDetails.Any())
                {
                    await UpdateFortDetailsAsync(guid, connection, workItem.FortDetails);
                    //new Thread(async () => await UpdateFortDetailsAsync(guid, connection, workItem.FortDetails))
                    //{ IsBackground = true }.Start();
                }

                // Parse gym info
                if (Options.ProcessGymInfo && workItem.GymInfo.Any())
                {
                    await UpdateGymInfoAsync(guid, connection, workItem.GymInfo);
                    //new Thread(async () => await UpdateGymInfoAsync(guid, connection, workItem.GymInfo))
                    //{ IsBackground = true }.Start();
                }

                // Parse wild pokemon
                if (Options.ProcessWildPokemon && workItem.WildPokemon.Any())
                {
                    await UpdateWildPokemonAsync(guid, connection, workItem.WildPokemon);
                    //new Thread(async () => await UpdateWildPokemonAsync(guid, connection, workItem.WildPokemon))
                    //{ IsBackground = true }.Start();
                }

                // Parse nearby pokemon
                if (Options.ProcessNearbyPokemon && workItem.NearbyPokemon.Any())
                {
                    await UpdateNearbyPokemonAsync(guid, connection, workItem.NearbyPokemon);
                    //new Thread(async () => await UpdateNearbyPokemonAsync(guid, connection, workItem.NearbyPokemon))
                    //{ IsBackground = true }.Start();
                }

                // Parse map pokemon
                if (Options.ProcessMapPokemon && workItem.MapPokemon.Any())
                {
                    await UpdateMapPokemonAsync(guid, connection, workItem.MapPokemon);
                    //new Thread(async () => await UpdateMapPokemonAsync(guid, connection, workItem.MapPokemon))
                    //{ IsBackground = true }.Start();
                }

                // Parse pokemon encounters
                if (Options.ProcessEncounters && workItem.Encounters.Any())
                {
                    await UpdateEncountersAsync(guid, connection, workItem.Encounters);
                    //new Thread(async () => await UpdateEncountersAsync(guid, connection, workItem.Encounters))
                    //{ IsBackground = true }.Start();
                }

                // Parse pokemon disk encounters
                if (Options.ProcessDiskEncounters && workItem.DiskEncounters.Any())
                {
                    await UpdateDiskEncountersAsync(guid, connection, workItem.DiskEncounters);
                    //new Thread(async () => await UpdateDiskEncountersAsync(guid, connection, workItem.DiskEncounters))
                    //{ IsBackground = true }.Start();
                }

                // Parse pokestop quests
                if (Options.ProcessQuests && workItem.Quests.Any())
                {
                    await UpdateQuestsAsync(guid, connection, workItem.Quests);
                    //new Thread(async () => await UpdateQuestsAsync(guid, connection, workItem.Quests))
                    //{ IsBackground = true }.Start();
                }

                //Parallel.ForEach(tasks, async (task, token) => await task.ConfigureAwait(false));
                ProtoDataStatistics.Instance.TotalEntitiesProcessed += (uint)workItem.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }

            if (ShowBenchmarkTimes)
            {
                sw.Stop();
                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, DefaultDecimals);
                _logger.LogInformation($"Finished parsing {workItem.Count:N0} data entities in {totalSeconds}s");
                //PrintBenchmarkTimes(DataLogLevel.Summary, workItem.Data, "total entities", sw);
            }

            _parsingSem.Release();
            await Task.CompletedTask;
        }

        #endregion

        #region Data Handling Methods

        private async Task UpdatePlayerDataAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> playerData)
        {
            var webhooks = new List<Account>();
            var count = playerData.Count();
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} player accounts");

            foreach (var player in playerData)
            {
                try
                {
                    var username = player.username;
                    var data = (GetPlayerOutProto)player.gpr;
                    Account account = await EntityRepository.GetEntityAsync<string, Account>(connection, username, _memCache);
                    if (account == null)
                    {
                        _logger.LogWarning($"Failed to retrieve account with username '{username}' from cache and database");
                        continue;
                    }

                    await account.UpdateAsync(connection, data, _memCache);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.AccountOnMergeUpdate, account);
                    ProtoDataStatistics.Instance.TotalPlayerDataProcessed++;

                    if (account.SendWebhook)
                    {
                        webhooks.Add(account);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdatePlayerDataAsync: {ex}");
                }
            }

            _logger.LogInformation($"[{requestId}] {count:N0} player accounts parsed");

            foreach (var account in webhooks)
            {
                await SendWebhookPayloadAsync(WebhookPayloadType.Account, account);
            }
        }

        private async Task UpdateCellsAsync(string requestId, IEnumerable<ulong> cells)
        {
            var sw = new Stopwatch();
            sw.Start();

            var s2cells = new List<Cell>();
            var count = cells.Count();
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} s2 cells");

            //var parsed = cells.Select(async cellId => await UpdateCellAsync(cellId));
            foreach (var cellId in cells)
            {
                try
                {
                    var cached = _memCache.Get<ulong, Cell>(cellId);
                    if (cached != null)
                    {
                        // Filter cells not already cached, cached cells expire every 60 minutes.
                        // Once expired they will be updated when found again.
                        var now = DateTime.UtcNow.ToTotalSeconds();
                        var needsUpdate = cached.Updated < now - CellScanIntervalS;
                        if (!needsUpdate)
                            continue;
                    }
                    else
                    {
                        cached = new Cell(cellId);
                        _memCache.Set(cellId, cached);
                    }

                    s2cells.Add(cached);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateCellsAsync: {ex}");
                }
            }

            await _dataConsumerService.AddEntitiesAsync(SqlQueryType.CellOnMergeUpdate, s2cells);
            ProtoDataStatistics.Instance.TotalS2CellsProcessed += (uint)s2cells.Count;

            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            _logger.LogInformation($"[{requestId}] {count:N0} s2 cells parsed in {totalSeconds}s with {(count - s2cells.Count):N0} ignored");

            //for (var i = 0; i < s2cells.Count; i++)
            //{
            //    try
            //    {
            //        var cell = s2cells[i];

            //        // Cache all S2 cell entities in memory cache
            //        _memCache.Set(cell.Id, cell);

            //        // Add S2 cells to ClearFortsHostedService
            //        _clearFortsService.AddCell(cell.Id);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError($"Error: {ex}");
            //    }
            //}
        }

        private async Task UpdateCellAsync(ulong cellId)
        {
            try
            {
                var cached = _memCache.Get<ulong, Cell>(cellId);
                if (cached != null)
                {
                    // Filter cells not already cached, cached cells expire every 60 minutes.
                    // Once expired they will be updated when found again.
                    var now = DateTime.UtcNow.ToTotalSeconds();
                    var needsUpdate = cached.Updated < now - CellScanIntervalS;
                    if (!needsUpdate)
                        return;
                }
                else
                {
                    cached = new Cell(cellId);
                    _memCache.Set(cellId, cached);
                }

                await _dataConsumerService.AddEntityAsync(SqlQueryType.CellOnMergeUpdate, cached);
                ProtoDataStatistics.Instance.TotalS2CellsProcessed++;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateCellAsync: {ex}");
            }

            //try
            //{
            //    // Cache all S2 cell entities in memory cache
            //    _memCache.Set(cell.Id, cell);

            //    // Add S2 cells to ClearFortsHostedService
            //    _clearFortsService.AddCell(cell.Id);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"Error: {ex}");
            //}
        }

        private async Task UpdateClientWeatherAsync(string requestId, MySqlConnection connection, IEnumerable<ClientWeatherProto> clientWeather)
        {
            var webhooks = new List<Weather>();
            var count = clientWeather.Count();
            var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} client weather cells");

            // Convert weather protos to Weather models
            var weatherCells = clientWeather
                .Where(wcell =>
                {
                    // Filter cells not already cached or updated recently
                    var cached = _memCache.Get<long, Weather>(wcell.S2CellId);
                    if (cached == null)
                        return true;

                    var now = DateTime.UtcNow.ToTotalSeconds();
                    var needsUpdate = cached.Updated < now - WeatherCellScanIntervalS;
                    return needsUpdate;
                })
                .Select(wcell =>
                {
                    var cached = _memCache.Get<long, Weather>(wcell.S2CellId);
                    if (cached == null)
                    {
                        cached = new Weather(wcell);
                        _memCache.Set(wcell.S2CellId, cached);
                    }
                    return cached;
                }).ToList();

            // Check if any new/need to be updated weather cells, otherwise skip
            if (!weatherCells.Any())
                return;

            foreach (var wcell in weatherCells)
            {
                _logger.LogInformation($"[{requestId}] Parsing weather cell {index:N0}/{count:N0}");
                try
                {
                    //var oldWeather = await EntityRepository.GetEntityAsync<long, Weather>(connection, wcell.Id, _memCache);
                    Weather? oldWeather = null;
                    await wcell.UpdateAsync(oldWeather, _memCache);

                    if (wcell.HasChanges)
                    {
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.WeatherOnMergeUpdate, wcell);
                        ProtoDataStatistics.Instance.TotalClientWeatherCellsProcessed++;
                    }

                    // Check if 'SendWebhook' flag was triggered, if so relay webhook payload to communicator
                    if (wcell.SendWebhook)
                    {
                        webhooks.Add(wcell);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateClientWeatherAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                index++;
            }

            _logger.LogInformation($"[{requestId}] {count:N0} weather cells parsed");

            if (webhooks.Any())
            {
                foreach (var weather in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Weather, weather);
                }
            }
        }

        private async Task UpdateWildPokemonAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> wildPokemon)
        {
            var webhooks = new List<Pokemon>();
            var count = wildPokemon.Count();
            //var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} wild pokemon");

            foreach (var wild in wildPokemon)
            {
                //_logger.LogInformation($"[{requestId}] Parsing wild pokemon {index:N0}/{count:N0}");
                try
                {
                    var cellId = wild.cell;
                    var data = (WildPokemonProto)wild.data;
                    var timestampMs = wild.timestampMs;
                    var username = wild.username;
                    var isEvent = wild.isEvent;
                    //var pokemon = new Pokemon(data, cellId, username, isEvent);
                    Pokemon pokemon = Pokemon.ParsePokemonFromWild(data, cellId, username, isEvent);
                    Spawnpoint spawnpoint = await pokemon.ParseSpawnpointAsync(connection, _memCache, data.TimeTillHiddenMs, timestampMs);
                    if (spawnpoint != null)
                    {
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
                        ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
                    }

                    //var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache, skipCache: true, setCache: false);
                    Pokemon? oldPokemon = null;
                    //await pokemon.UpdateAsync(connection, _memCache, updateIv: false, skipLookup: false);
                    await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: false);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonIgnoreOnMerge, pokemon);
                    ProtoDataStatistics.Instance.TotalWildPokemonProcessed++;

                    if (pokemon.SendWebhook)
                    {
                        webhooks.Add(pokemon);
                    }

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
                //index++;
            }

            _logger.LogInformation($"[{requestId}] {count:N0} wild pokemon parsed");

            if (webhooks.Any())
            {
                _logger.LogDebug($"Sending {webhooks.Count:N0} pokemon webhooks");
                foreach (var pokemon in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                }

                // Send pokemon to Configurator endpoint for IV stats
                await SendPokemonAsync(webhooks);
            }
        }

        private async Task UpdateNearbyPokemonAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> nearbyPokemon)
        {
            var webhooks = new List<Pokemon>();
            var count = nearbyPokemon.Count();
            //var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} nearby pokemon");

            foreach (var nearby in nearbyPokemon)
            {
                //_logger.LogInformation($"[{requestId}] Parsing nearby pokemon {index:N0}/{count:N0}");
                try
                {
                    var cellId = nearby.cell;
                    var data = (NearbyPokemonProto)nearby.data;
                    var username = nearby.username;
                    var isEvent = nearby.isEvent;
                    //var pokemon = new Pokemon(connection, _memCache, data, cellId, username, isEvent);
                    Pokemon pokemon = await Pokemon.ParsePokemonFromNearby(connection, _memCache, data, cellId, username, isEvent);
                    if (pokemon == null)
                    {
                        // Failed to get pokestop
                        _logger.LogWarning($"Failed to find pokestop with id '{data.FortId}' for nearby pokemon: {data.EncounterId}");
                        continue;
                    }

                    //var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache, skipCache: true, setCache: false);
                    Pokemon? oldPokemon = null;
                    //await pokemon.UpdateAsync(connection, _memCache, updateIv: false, skipLookup: false);
                    await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: false);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonIgnoreOnMerge, pokemon);
                    ProtoDataStatistics.Instance.TotalNearbyPokemonProcessed++;

                    //_logger.LogInformation($"[{requestId}] Updated nearby pokemon {index:N0}/{count:N0}");

                    if (pokemon.SendWebhook)
                    {
                        webhooks.Add(pokemon);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateNearbyPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                //index++;
            }
            _logger.LogInformation($"[{requestId}] {count:N0} nearby pokemon parsed");

            if (webhooks.Any())
            {
                _logger.LogDebug($"Sending {webhooks.Count:N0} pokemon webhooks");
                foreach (var pokemon in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                }

                // Send pokemon to Configurator endpoint for IV stats
                await SendPokemonAsync(webhooks);
            }
        }

        private async Task UpdateMapPokemonAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> mapPokemon)
        {
            var webhooks = new List<Pokemon>();
            var count = mapPokemon.Count();
            var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} map pokemon");

            foreach (var map in mapPokemon)
            {
                _logger.LogInformation($"[{requestId}] Parsing map pokemon {index:N0}/{count:N0}");
                try
                {
                    var cellId = map.cell;
                    var data = (MapPokemonProto)map.data;
                    var username = map.username;
                    var isEvent = map.isEvent;

                    // Check if we have a pending disk encounter cache
                    var displayId = data.PokemonDisplay.DisplayId;
                    var cachedDiskEncounter = _diskCache.Get<DiskEncounterOutProto>(displayId);
                    if (cachedDiskEncounter == null)
                    {
                        // Failed to get DiskEncounter from cache
                        _logger.LogWarning($"Unable to fetch cached Pokemon disk encounter with id '{displayId}' from cache");
                        continue;
                    }

                    // Thanks Fabio <3
                    _logger.LogDebug($"Found Pokemon disk encounter with id '{displayId}' in cache");

                    //var pokemon = new Pokemon(connection, _memCache, data, cellId, username, isEvent);
                    // TODO: Lookup old pokemon first then if not null update properties from map proto
                    Pokemon pokemon = await Pokemon.ParsePokemonFromMap(connection, _memCache, data, cellId, username, isEvent);
                    pokemon.AddDiskEncounter(cachedDiskEncounter, username);

                    var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache, skipCache: true, setCache: false);
                    await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: true);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonOnMergeUpdate, pokemon);
                    ProtoDataStatistics.Instance.TotalMapPokemonProcessed++;

                    if (pokemon.SendWebhook)
                    {
                        webhooks.Add(pokemon);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateMapPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                index++;
            }
            _logger.LogInformation($"[{requestId}] {count:N0} map pokemon parsed");

            if (webhooks.Any())
            {
                _logger.LogDebug($"Sending {webhooks.Count:N0} pokemon webhooks");
                foreach (var pokemon in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                }

                // Send pokemon to Configurator endpoint for IV stats
                await SendPokemonAsync(webhooks);
            }
        }

        private async Task UpdateFortsAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> forts, string? username)
        {
            var webhooks = new Dictionary<WebhookType, List<BaseEntity>>();
            var count = forts.Count();
            //var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} forts");

            foreach (var fort in forts)
            {
                //_logger.LogInformation($"[{requestId}] Parsing fort {index:N0}/{count:N0}");
                try
                {
                    var cellId = (ulong)fort.cell;
                    var data = (PokemonFortProto)fort.data;
                    //var username = (string)fort.username;

                    switch (data.FortType)
                    {
                        case FortType.Checkpoint:
                            // Init Pokestop model from fort proto data
                            var pokestop = new Pokestop(data, cellId);
                            //var oldPokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, pokestop.Id, _memCache);
                            Pokestop? oldPokestop = null;
                            //var pokestopWebhooks = await pokestop.UpdateAsync(connection, _memCache, updateQuest: false, skipLookup: false);
                            var pokestopWebhooks = await pokestop.UpdateAsync(oldPokestop, _memCache, updateQuest: false);

                            await _dataConsumerService.AddEntityAsync(SqlQueryType.PokestopIgnoreOnMerge, pokestop);
                            ProtoDataStatistics.Instance.TotalFortsProcessed++;
                            _clearFortsService.AddPokestop(cellId, data.FortId);

                            if (pokestopWebhooks.Any())
                            {
                                foreach (var (key, value) in pokestopWebhooks)
                                {
                                    if (webhooks.ContainsKey(key))
                                    {
                                        webhooks[key].Add(value);
                                    }
                                    else
                                    {
                                        webhooks.Add(key, new() { value });
                                    }
                                }
                            }

                            await UpdateIncidentsAsync(requestId, connection, pokestop);
                            break;
                        case FortType.Gym:
                            // Init Gym model from fort proto data
                            var gym = new Gym(data, cellId);
                            //var oldGym = await EntityRepository.GetEntityAsync<string, Gym>(connection, gym.Id, _memCache);
                            Gym? oldGym = null;
                            //var gymWebhooks = await gym.UpdateAsync(connection, _memCache, skipLookup: false);
                            var gymWebhooks = await gym.UpdateAsync(oldGym, _memCache);

                            await _dataConsumerService.AddEntityAsync(SqlQueryType.GymOnMergeUpdate, gym);
                            ProtoDataStatistics.Instance.TotalFortsProcessed++;
                            _clearFortsService.AddGym(cellId, data.FortId);

                            if (gymWebhooks.Any())
                            {
                                foreach (var (key, value) in gymWebhooks)
                                {
                                    if (webhooks.ContainsKey(key))
                                    {
                                        webhooks[key].Add(value);
                                    }
                                    else
                                    {
                                        webhooks.Add(key, new() { value });
                                    }
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateFortsAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                //index++;
            }
            _logger.LogInformation($"[{requestId}] {count:N0} forts parsed");

            if (webhooks.Any())
            {
                foreach (var (key, pokestops) in webhooks)
                {
                    var type = ConvertWebhookType(key);
                    foreach (var pokestop in pokestops)
                    {
                        await SendWebhookPayloadAsync(type, pokestop);
                    }
                }
            }

            if (string.IsNullOrEmpty(username))
                return;

            // Send found/nearby forts with gRPC service for leveling instance
            var lvlForts = forts
                .Select(fort => fort.data)
                .Select(fort => (PokemonFortProto)fort)
                .Where(fort => fort.FortType == FortType.Checkpoint)
                .ToList();
            // Ensure that the account username is set, otherwise ignore relaying
            // fort data for leveling instance
            if (!lvlForts.Any())
                return;

            await SendPokestopsAsync(lvlForts, username);
        }

        private async Task UpdateFortDetailsAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> fortDetails)
        {
            var fortDetailsPokestops = fortDetails
                .Where(fort => fort.data.FortType == FortType.Checkpoint);
            if (fortDetailsPokestops.Any())
            {
                var count = fortDetailsPokestops.Count();
                //var index = 1;
                _logger.LogInformation($"[{requestId}] Parsing {count:N0} pokestop fort details");

                // Convert fort details protos to Pokestop models
                foreach (var fort in fortDetailsPokestops)
                {
                    //_logger.LogInformation($"[{requestId}] Parsing pokestop fort detail {index:N0}/{count:N0}");
                    try
                    {
                        var data = (FortDetailsOutProto)fort.data;
                        var pokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, data.Id, _memCache);
                        if (pokestop == null)
                            continue;

                        pokestop.AddDetails(data);
                        //var pokestopWebhooks = await pokestop.UpdateAsync(connection, _memCache, skipLookup: true);
                        var pokestopWebhooks = await pokestop.UpdateAsync(null, _memCache);

                        if (pokestop.HasChanges)
                        {
                            await _dataConsumerService.AddEntityAsync(SqlQueryType.PokestopDetailsOnMergeUpdate, pokestop);
                            ProtoDataStatistics.Instance.TotalFortDetailsProcessed++;
                        }

                        foreach (var webhook in pokestopWebhooks)
                        {
                            var type = ConvertWebhookType(webhook.Key);
                            await SendWebhookPayloadAsync(type, pokestop);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"UpdateFortDetailsAsync[Pokestop]: {ex.InnerException?.Message ?? ex.Message}");
                    }
                    //index++;
                }

                _logger.LogInformation($"[{requestId}] {count:N0} pokestop fort details parsed");
            }

            var fortDetailsGyms = fortDetails
                .Where(fort => fort.data.FortType == FortType.Gym);
            if (fortDetailsGyms.Any())
            {
                var count = fortDetailsGyms.Count();
                //var index = 1;
                _logger.LogInformation($"[{requestId}] Parsing {count:N0} gym fort details");

                // Convert fort details protos to Gym models
                foreach (var fort in fortDetailsGyms)
                {
                    //_logger.LogInformation($"[{requestId}] Parsing fort detail {index:N0}/{count:N0}");

                    try
                    {
                        var data = (FortDetailsOutProto)fort.data;
                        var gym = await EntityRepository.GetEntityAsync<string, Gym>(connection, data.Id, _memCache);
                        if (gym == null)
                            continue;

                        gym.AddDetails(data);
                        //var gymWebhooks = await gym.UpdateAsync(connection, _memCache, skipLookup: true);
                        var gymWebhooks = await gym.UpdateAsync(null, _memCache);

                        if (gym.HasChanges)
                        {
                            await _dataConsumerService.AddEntityAsync(SqlQueryType.GymDetailsOnMergeUpdate, gym);
                        }

                        foreach (var webhook in gymWebhooks)
                        {
                            var type = ConvertWebhookType(webhook.Key);
                            await SendWebhookPayloadAsync(type, gym);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"UpdateFortDetailsAsync[Gym]: {ex.InnerException?.Message ?? ex.Message}");
                    }
                    //index++;
                }

                _logger.LogInformation($"[{requestId}] {count:N0} gym fort details parsed");
            }
        }

        private async Task UpdateGymInfoAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> gymInfos)
        {
            var webhooks = new Dictionary<WebhookType, List<BaseEntity>>();
            var count = gymInfos.Count();
            var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} gym infos");

            // Convert gym info protos to Gym models
            foreach (var gymInfo in gymInfos)
            {
                _logger.LogInformation($"[{requestId}] Parsing gym info {index:N0}/{count:N0}");
                try
                {
                    var data = (GymGetInfoOutProto)gymInfo.data;
                    var fortId = data.GymStatusAndDefenders.PokemonFortProto.FortId;
                    var gym = await EntityRepository.GetEntityAsync<string, Gym>(connection, fortId, _memCache);
                    if (gym == null)
                        continue;

                    gym.AddDetails(data);
                    //var gymWebhooks = await gym.UpdateAsync(connection, _memCache, skipLookup: true);
                    var gymWebhooks = await gym.UpdateAsync(null, _memCache);

                    if (gym.HasChanges)
                    {
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.GymDetailsOnMergeUpdate, gym);
                        ProtoDataStatistics.Instance.TotalFortDetailsProcessed++;
                    }

                    foreach (var (key, value) in gymWebhooks)
                    {
                        if (webhooks.ContainsKey(key))
                        {
                            webhooks[key].Add(value);
                        }
                        else
                        {
                            webhooks.Add(key, new() { value });
                        }
                    }

                    if (Options.ProcessGymDefenders || Options.ProcessGymTrainers)
                    {
                        var gymDefenders = data.GymStatusAndDefenders.GymDefender;
                        if (gymDefenders == null)
                            continue;

                        foreach (var gymDefenderData in gymDefenders)
                        {
                            if (Options.ProcessGymTrainers && gymDefenderData.TrainerPublicProfile != null)
                            {
                                var gymTrainer = new GymTrainer(gymDefenderData.TrainerPublicProfile);
                                await _dataConsumerService.AddEntityAsync(SqlQueryType.GymTrainerOnMergeUpdate, gymTrainer);
                                ProtoDataStatistics.Instance.TotalGymTrainersProcessed++;

                                if (webhooks.ContainsKey(WebhookType.GymTrainers))
                                {
                                    webhooks[WebhookType.GymTrainers].Add(gymTrainer);
                                }
                                else
                                {
                                    webhooks.Add(WebhookType.GymTrainers, new() { gymTrainer });
                                }
                            }
                            if (Options.ProcessGymDefenders && gymDefenderData.MotivatedPokemon != null)
                            {
                                var gymDefender = new GymDefender(gymDefenderData, fortId);
                                await _dataConsumerService.AddEntityAsync(SqlQueryType.GymDefenderOnMergeUpdate, gymDefender);
                                ProtoDataStatistics.Instance.TotalGymDefendersProcessed++;

                                if (webhooks.ContainsKey(WebhookType.GymDefenders))
                                {
                                    webhooks[WebhookType.GymDefenders].Add(gymDefender);
                                }
                                else
                                {
                                    webhooks.Add(WebhookType.GymDefenders, new() { gymDefender });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
                }

                index++;
            }

            _logger.LogInformation($"[{requestId}] {count:N0} gym infos parsed");

            if (webhooks.Any())
            {
                foreach (var (key, value) in webhooks)
                {
                    var type = ConvertWebhookType(key);
                    await SendWebhookPayloadAsync(type, value);
                }
            }
        }

        private async Task UpdateQuestsAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> quests)
        {
            // Convert quest protos to Pokestop models
            var webhooks = new Dictionary<WebhookType, List<Pokestop>>();
            var count = quests.Count();
            //var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} quests");

            foreach (var quest in quests)
            {
                //_logger.LogInformation($"[{requestId}] Parsing quest {index:N0}/{count:N0}");
                try
                {
                    var data = (QuestProto)quest.quest;
                    var title = quest.title;
                    var hasAr = quest.hasAr;
                    var fortId = data.FortId;
                    var pokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, fortId, _memCache);
                    if (pokestop == null)
                        continue;

                    pokestop.AddQuest(title, data, hasAr);
                    //var questWebhooks = await pokestop.UpdateAsync(connection, _memCache, updateQuest: true, skipLookup: true);
                    var questWebhooks = await pokestop.UpdateAsync(null, _memCache, updateQuest: true);

                    if (pokestop.HasChanges && (pokestop.HasQuestChanges || pokestop.HasAlternativeQuestChanges))
                    {
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.PokestopOnMergeUpdate, pokestop);
                        ProtoDataStatistics.Instance.TotalQuestsProcessed++;
                    }

                    if (questWebhooks.Any())
                    {
                        foreach (var (key, value) in questWebhooks)
                        {
                            if (webhooks.ContainsKey(key))
                            {
                                webhooks[key].Add(value);
                            }
                            else
                            {
                                webhooks.Add(key, new() { value });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateQuestsAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                //index++;
            }
            _logger.LogInformation($"[{requestId}] {count:N0} quests parsed");

            if (webhooks.Any())
            {
                foreach (var (key, pokestopQuests) in webhooks)
                {
                    var type = ConvertWebhookType(key);
                    foreach (var quest in pokestopQuests)
                    {
                        await SendWebhookPayloadAsync(type, quest);
                    }
                }
            }
        }

        private async Task UpdateEncountersAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> encounters)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var timestampMs = now * 1000;
            var webhooks = new List<Pokemon>();

            var count = encounters.Count();
            //var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} pokemon encounters");

            foreach (var encounter in encounters)
            {
                //_logger.LogInformation($"[{requestId}] Parsing pokemon encounter {index:N0}/{count:N0}");
                try
                {
                    var data = (EncounterOutProto)encounter.data;
                    var username = encounter.username;
                    var isEvent = encounter.isEvent;
                    var encounterId = data.Pokemon.EncounterId.ToString();
                    var pokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, encounterId, _memCache, skipCache: true, setCache: false);
                    //var isNew = false;
                    if (pokemon == null)
                    {
                        // New Pokemon
                        var cellId = S2CellExtensions.S2CellIdFromLatLng(data.Pokemon.Latitude, data.Pokemon.Longitude);
                        await UpdateCellAsync(cellId.Id);

                        //pokemon = new Pokemon(data.Pokemon, cellId.Id, username, isEvent);
                        pokemon = Pokemon.ParsePokemonFromWild(data.Pokemon, cellId.Id, username, isEvent);
                        //isNew = true;
                    }
                    await pokemon.AddEncounterAsync(data, username);

                    //if (pokemon.HasIvChanges)
                    //{
                    SetPvpRankings(pokemon);
                    //}

                    Spawnpoint? spawnpoint = await pokemon.ParseSpawnpointAsync(connection, _memCache, data.Pokemon.TimeTillHiddenMs, timestampMs);
                    if (spawnpoint != null)
                    {
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
                        ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
                    }

                    //var oldPokemon = isNew ? null : await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache);
                    //await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipLookup: isNew);
                    //await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: true);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonOnMergeUpdate, pokemon);
                    ProtoDataStatistics.Instance.TotalPokemonEncountersProcessed++;

                    if (pokemon.SendWebhook)
                    {
                        webhooks.Add(pokemon);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateEncountersAsync: {ex}");
                }
                //index++;
            }
            _logger.LogInformation($"[{requestId}] {count:N0} pokemon encounters parsed");

            if (webhooks.Any())
            {
                _logger.LogDebug($"Sending {webhooks.Count:N0} pokemon webhooks");
                foreach (var pokemon in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                }

                // Send pokemon to Configurator endpoint for IV stats
                await SendPokemonAsync(webhooks);
            }
        }

        private async Task UpdateDiskEncountersAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> diskEncounters)
        {
            var webhooks = new List<Pokemon>();
            var count = diskEncounters.Count();
            var index = 1;
            _logger.LogInformation($"[{requestId}] Parsing {count:N0} pokemon disk encounters");

            foreach (var diskEncounter in diskEncounters)
            {
                _logger.LogInformation($"[{requestId}] Parsing pokemon disk encounter {index:N0}/{count:N0}");
                try
                {
                    var data = (DiskEncounterOutProto)diskEncounter.data;
                    var username = diskEncounter.username;
                    var isEvent = diskEncounter.isEvent;
                    var displayId = Convert.ToString(data.Pokemon.PokemonDisplay.DisplayId);
                    var pokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, displayId, _memCache, skipCache: true, setCache: false);
                    if (pokemon == null)
                    {
                        _diskCache.Set(displayId, data, _diskCacheExpiry);
                        _logger.LogInformation($"Disk encounter with id '{displayId}' added to cache");
                        continue;
                    }

                    pokemon.AddDiskEncounter(data, username);
                    if (pokemon.HasIvChanges)
                    {
                        SetPvpRankings(pokemon);
                    }

                    //var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache);
                    //await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: true);
                    //await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipLookup: true);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonOnMergeUpdate, pokemon);
                    ProtoDataStatistics.Instance.TotalPokemonDiskEncountersProcessed++;

                    if (pokemon.SendWebhook)
                    {
                        webhooks.Add(pokemon);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                index++;
            }
            _logger.LogInformation($"[{requestId}] {count:N0} pokemon disk encounters parsed");

            if (webhooks.Any())
            {
                _logger.LogDebug($"Sending {webhooks.Count:N0} pokemon webhooks");
                foreach (var pokemon in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                }

                // Send pokemon to Configurator endpoint for IV stats
                await SendPokemonAsync(webhooks);
            }
        }

        private async Task UpdateIncidentsAsync(string requestId, MySqlConnection connection, Pokestop pokestop)
        {
            if (!(pokestop.Incidents?.Any() ?? false))
                return;

            var incidents = pokestop.Incidents;
            var webhooks = new List<PokestopWithIncident>();
            //var count = incidents.Count;
            //var index = 1;
            //_logger.LogInformation($"[{requestId}] Parsing {count:N0} pokestop incidents");

            // Loop incidents
            foreach (var incident in incidents)
            {
                //_logger.LogInformation($"[{requestId}] Parsing pokestop incident {index:N0}/{count:N0}");
                try
                {
                    var oldIncident = await EntityRepository.GetEntityAsync<string, Incident>(connection, incident.Id, _memCache);
                    //await incident.UpdateAsync(connection, _memCache, skipOldLookup: false);
                    await incident.UpdateAsync(oldIncident, _memCache);

                    if (incident.HasChanges)
                    {
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.IncidentOnMergeUpdate, incident);
                        ProtoDataStatistics.Instance.TotalIncidentsProcessed++;
                    }

                    if (incident.SendWebhook)
                    {
                        webhooks.Add(new PokestopWithIncident(pokestop, incident));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ParseIncidentsAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
                //index++;
            }
            //_logger.LogInformation($"[{requestId}] {count:N0} pokemon incidents parsed");

            if (webhooks.Any())
            {
                //_logger.LogDebug($"Sending {webhooks.Count:N0} pokestop incident webhooks");
                foreach (var incident in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Invasion, incident);
                }
            }
        }

        #endregion

        #region Private Methods

        private void CheckQueueLength()
        {
            var usage = $"{_taskQueue.Count:N0}/{Options.Queue.MaximumCapacity:N0}";
            if (_taskQueue.Count >= Options.Queue.MaximumCapacity)
            {
                _logger.LogError($"Data processing queue is at maximum capacity! {usage}");
            }
            else if (_taskQueue.Count >= Options.Queue.MaximumSizeWarning)
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

        //private static List<TEntity> FilterEntityData<TEntity>(string property, List<dynamic> data, ProtoDataType type)
        //{
        //    var entities = data
        //        .Where(x => x.type == type)
        //        .Select(x => (TEntity)x.GetType().GetProperty(property).GetValue(x, null))
        //        .Distinct()
        //        .ToList();
        //    return entities;
        //}

        private void PrintBenchmarkTimes(DataLogLevel logLevel, IReadOnlyList<object> entities, string text = "total entities", Stopwatch? sw = null)
        {
            // TODO: Implement log filtering again
            //if (!Options.Data.IsEnabled(logLevel))
            //    return;

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
            //if (entity == null)
            //{
            //    _logger.LogWarning($"Unable to relay entity {typeof(T).Name} to webhook service, entity is null...");
            //    return;
            //}

            //var json = entity.ToJson();
            //if (string.IsNullOrEmpty(json))
            //{
            //    _logger.LogWarning($"Failed to serialize entity {typeof(T).Name} to relay to webhook service, skipping...");
            //    return;
            //}

            //// Fire off gRPC request on a separate thread
            //await Task.Run(() =>
            //{
            //    new Thread(async () =>
            //    {
            //        await _grpcClientService.SendWebhookPayloadAsync(webhookType, json);
            //    })
            //    { IsBackground = true }.Start();
            //});

            await Task.CompletedTask;
        }

        private async Task SendPokemonAsync(List<Pokemon> pokemon)
        {
            //var newPokemon = pokemon
            //    .Where(pkmn => pkmn.IsNewPokemon)
            //    .ToList();
            //var newPokemonWithIV = pokemon
            //    .Where(pkmn => pkmn.IsNewPokemonWithIV)
            //    .ToList();

            //if (newPokemon.Any())
            //{
            //    // Fire off gRPC request on a separate thread
            //    await Task.Run(() =>
            //    {
            //        new Thread(async () =>
            //        {
            //            // Send got Pokemon proto message
            //            await _grpcClientService.SendRpcPayloadAsync(
            //                    newPokemon,
            //                    PayloadType.PokemonList,
            //                    hasIV: false
            //                );
            //        })
            //        { IsBackground = true }.Start();
            //    });
            //}

            //if (newPokemonWithIV.Any())
            //{
            //    // Fire off gRPC request on a separate thread
            //    await Task.Run(() =>
            //    {
            //        new Thread(async () =>
            //        {
            //            // Send got Pokemon IV proto message
            //            await _grpcClientService.SendRpcPayloadAsync(
            //                newPokemonWithIV,
            //                PayloadType.PokemonList,
            //                hasIV: true
            //            );
            //        })
            //        { IsBackground = true }.Start();
            //    });
            //}

            await Task.CompletedTask;
        }

        private async Task SendPokestopsAsync(List<PokemonFortProto> forts, string username)
        {
            // Fire off gRPC request on a separate thread
            //await Task.Run(() =>
            //{
            //    new Thread(async () =>
            //    {
            //        await _grpcClientService.SendRpcPayloadAsync(forts, PayloadType.FortList, username);
            //    })
            //    { IsBackground = true }.Start();
            //});

            await Task.CompletedTask;
        }

        private async Task SendPlayerDataAsync(string username, ushort level, uint xp)
        {
            // Fire off gRPC request on a separate thread
            //await Task.Run(() =>
            //{
            //    new Thread(async () =>
            //    {
            //        var payload = new { username, level, xp };
            //        await _grpcClientService.SendRpcPayloadAsync(payload, PayloadType.PlayerInfo, username);
            //    })
            //    { IsBackground = true }.Start();
            //});

            await Task.CompletedTask;
        }

        #endregion
    }

    public class DataItems
    {
        public IEnumerable<dynamic> PlayerData { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<ulong> Cells { get; internal set; } = Array.Empty<ulong>();

        public IEnumerable<ClientWeatherProto> Weather { get; internal set; } = Array.Empty<ClientWeatherProto>();
        //public IReadOnlyDictionary<string, IEnumerable<dynamic>> Forts { get; internal set; } = new Dictionary<string, IEnumerable<dynamic>>();

        public IEnumerable<dynamic> Forts { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> FortDetails { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> GymInfo { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> WildPokemon { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> NearbyPokemon { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> MapPokemon { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> Quests { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> Encounters { get; internal set; } = Array.Empty<dynamic>();

        public IEnumerable<dynamic> DiskEncounters { get; internal set; } = Array.Empty<dynamic>();

        public int Count =>
            PlayerData.Count() +
            Cells.Count() +
            Weather.Count() +
            Forts.Count() +
            FortDetails.Count() +
            GymInfo.Count() +
            PokemonCount +
            Quests.Count();
            //Encounters.Count() +
            //DiskEncounters.Count();

        public int PokemonCount =>
            WildPokemon.Count() +
            NearbyPokemon.Count() +
            MapPokemon.Count() +
            Encounters.Count() +
            DiskEncounters.Count();

        public DataItems(
            IEnumerable<dynamic> playerData,
            IEnumerable<ulong> cells,
            IEnumerable<ClientWeatherProto> weather,
            //Dictionary<string, IEnumerable<dynamic>> forts,
            IEnumerable<dynamic> forts,
            IEnumerable<dynamic> fortDetails,
            IEnumerable<dynamic> gymInfo,
            IEnumerable<dynamic> wildPokemon,
            IEnumerable<dynamic> nearbyPokemon,
            IEnumerable<dynamic> mapPokemon,
            IEnumerable<dynamic> quests,
            IEnumerable<dynamic> encounters,
            IEnumerable<dynamic> diskEncounters)
        {
            PlayerData = playerData;
            Cells = cells;
            Weather = weather;
            Forts = forts;
            FortDetails = fortDetails;
            GymInfo = gymInfo;
            WildPokemon = wildPokemon;
            NearbyPokemon = nearbyPokemon;
            MapPokemon = mapPokemon;
            Quests = quests;
            Encounters = encounters;
            DiskEncounters = diskEncounters;
        }

        public static DataItems ParseWorkItems(IEnumerable<DataQueueItem> workItems)
        {
            var items = workItems.SelectMany(x => x.Data!);
            var playerData = items
                .Where(x => x.type == ProtoDataType.PlayerData)
                .ToList();
            var cells = items
                .Where(x => x.type == ProtoDataType.Cell)
                .Select(x => (ulong)x.cell)
                .Distinct()
                .ToList();
            var clientWeather = items
                .Where(x => x.type == ProtoDataType.ClientWeather)
                .Select(x => (ClientWeatherProto)x.data)
                .Distinct()
                .ToList();
            var forts = items
                .Where(x => x.type == ProtoDataType.Fort)
                .ToList();
            //var forts2 = workItems.ToDictionary(x => x.Username!, y => y.Data!.Where(x => x.type == ProtoDataType.Fort)) ?? new();
            var fortDetails = items
                .Where(x => x.type == ProtoDataType.FortDetails)
                .ToList();
            var gymInfos = items
                .Where(x => x.type == ProtoDataType.GymInfo)
                .ToList();
            var wildPokemon = items
                .Where(x => x.type == ProtoDataType.WildPokemon)
                .ToList();
            var nearbyPokemon = items
                .Where(x => x.type == ProtoDataType.NearbyPokemon)
                .ToList();
            var mapPokemon = items
                .Where(x => x.type == ProtoDataType.MapPokemon)
                .ToList();
            var quests = items
                .Where(x => x.type == ProtoDataType.Quest)
                .ToList();
            var encounters = items
                .Where(x => x.type == ProtoDataType.Encounter)
                .ToList();
            var diskEncounters = items
                .Where(x => x.type == ProtoDataType.DiskEncounter)
                .ToList();

            return new DataItems(
                playerData, cells, clientWeather,
                forts, fortDetails, gymInfos,
                wildPokemon, nearbyPokemon, mapPokemon,
                quests, encounters, diskEncounters);
        }
    }
}