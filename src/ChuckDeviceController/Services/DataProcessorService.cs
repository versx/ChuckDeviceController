namespace ChuckDeviceController.Services
{
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.EntityFrameworkCore;
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

    // TODO: Split up/refactor class
    // TODO: var leakMonitor = new ConnectionLeakWatcher(context);

    public class DataProcessorService : TimedHostedService, IDataProcessorService
    {
        #region Constants

        private const uint ProcessIntervalS = 5;
        private const ushort DefaultDecimals = 4;
        private const ushort CellScanIntervalS = 900; // REVIEW: Change to every 5 minutes vs 15 minutes

        #endregion

        #region Variables

        //private static readonly SemaphoreSlim _parsingSem = new(1, 1); //new(5, 5);
        private static readonly object _cellLock = new();

        private readonly ILogger<IDataProcessorService> _logger;
        private readonly IAsyncQueue<DataQueueItem> _taskQueue;
        private readonly IMemoryCache _diskCache;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IClearFortsHostedService _clearFortsService;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly IWebHostEnvironment _env;
        private readonly IDataConsumerService _dataConsumerService;
        //private readonly ThreadManager _threadManager = new(maxThreadCount: 25);

        #endregion

        #region Properties

        public DataProcessorOptionsConfig Options { get; }

        public bool ShowBenchmarkTimes => _env?.IsDevelopment() ?? false;

        public override uint TimerIntervalS => Options?.IntervalS ?? ProcessIntervalS;

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

            Options = options.Value;
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
                await ProcessWorkItemAsync(items, stoppingToken);
                //new Thread(async () => await ProcessWorkItemAsync(items, stoppingToken))
                //{ IsBackground = true }.Start();
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

        private async Task ProcessWorkItemAsync(DataItems workItem, CancellationToken stoppingToken = default)
        {
            if (workItem == null)
                return;

            CheckQueueLength();

            //await _parsingSem.WaitAsync(stoppingToken);
            MySqlConnection connection = null;
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
                var tasks = new List<Task>();
                var guid = Guid.NewGuid().ToString()[..8];
                _logger.LogInformation($"[{guid}] Parsing {workItem.Count:N0} data entities");

                if (Options.ProcessPlayerData && workItem.PlayerData.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.PlayerData.Count():N0} player data");
                    // Insert player account data
                    //await UpdatePlayerDataAsync(connection, workItem.PlayerData);
                    new Thread(async () => await UpdatePlayerDataAsync(connection, workItem.PlayerData))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdatePlayerDataAsync(workItem.PlayerData));
                }

                if (Options.ProcessCells && workItem.Cells.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.Cells.Count():N0} s2 cells");
                    // Insert S2 cells
                    //await UpdateCellsAsync(workItem.Cells);
                    new Thread(async () => await UpdateCellsAsync(workItem.Cells))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdateCellsAsync(workItem.Cells));
                }

                if (Options.ProcessWeather && workItem.Weather.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.Weather.Count():N0} weather cells");
                    // Insert weather cells
                    //await UpdateClientWeatherAsync(connection, workItem.Weather);
                    new Thread(async () => await UpdateClientWeatherAsync(connection, workItem.Weather))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdateClientWeatherAsync(connection, workItem.Weather));
                }

                if (Options.ProcessForts && workItem.Forts.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.Forts.Count():N0} forts");
                    // Insert Pokestop and Gym forts
                    //await UpdateFortsAsync(connection, workItem.Forts, "");
                    new Thread(async () => await UpdateFortsAsync(connection, workItem.Forts, ""))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdateFortsAsync(connection, workItem.Forts, ""));
                }

                if (Options.ProcessFortDetails && workItem.FortDetails.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.FortDetails.Count():N0} fort details");
                    // Insert Fort Details
                    //await UpdateFortDetailsAsync(connection, workItem.FortDetails);
                    new Thread(async () => await UpdateFortDetailsAsync(connection, workItem.FortDetails))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdateFortDetailsAsync(connection, workItem.FortDetails));
                }

                if (Options.ProcessGymInfo && workItem.GymInfo.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.GymInfo.Count():N0} gym infos");
                    // Insert gym info
                    //await UpdateGymInfoAsync(connection, workItem.GymInfo);
                    new Thread(async () => await UpdateGymInfoAsync(connection, workItem.GymInfo))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdateGymInfoAsync(connection, workItem.GymInfo));
                }

                //if (Options.ProcessPokemon && workItem.PokemonCount > 0)
                if ((Options.ProcessWildPokemon ||
                     Options.ProcessNearbyPokemon ||
                     Options.ProcessMapPokemon ||
                     Options.ProcessEncounters ||
                     Options.ProcessDiskEncounters) && workItem.PokemonCount > 0)
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.PokemonCount:N0} pokemon");
                    // Insert wild, nearby, and map pokemon
                    //await UpdatePokemonAsync(guid, connection,
                    //    workItem.WildPokemon,
                    //    workItem.NearbyPokemon,
                    //    workItem.MapPokemon,
                    //    workItem.Encounters,
                    //    workItem.DiskEncounters
                    //);
                    new Thread(async () => await UpdatePokemonAsync(guid, connection,
                        workItem.WildPokemon,
                        workItem.NearbyPokemon,
                        workItem.MapPokemon,
                        workItem.Encounters,
                        workItem.DiskEncounters
                    ))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdatePokemonAsync(connection, workItem.WildPokemon, workItem.NearbyPokemon, workItem.MapPokemon));
                }

                if (Options.ProcessQuests && workItem.Quests.Any())
                {
                    _logger.LogInformation($"[{guid}] Parsing {workItem.Quests.Count():N0} quests");
                    // Insert quests
                    //await UpdateQuestsAsync(connection, workItem.Quests);
                    new Thread(async () => await UpdateQuestsAsync(connection, workItem.Quests))
                    { IsBackground = true }.Start();
                    //tasks.Add(UpdateQuestsAsync(connection, workItem.Quests));
                }

                Parallel.ForEach(tasks, async (task, token) => await task.ConfigureAwait(false));
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

            //_parsingSem.Release();

            await Task.CompletedTask;
        }

        #endregion

        #region Data Handling Methods

        private async Task UpdatePlayerDataAsync(MySqlConnection connection, IEnumerable<dynamic> playerData)
        {
            var webhooks = new List<Account>();

            foreach (var player in playerData)
            {
                try
                {
                    var username = player.username;
                    var data = (GetPlayerOutProto)player.gpr;
                    var account = await EntityRepository.GetEntityAsync<string, Account>(connection, username, _memCache);
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

            foreach (var account in webhooks)
            {
                await SendWebhookPayloadAsync(WebhookPayloadType.Account, account);
            }
        }

        private async Task UpdateCellsAsync(IEnumerable<ulong> cells)
        {
            // Convert cell ids to Cell models
            var s2cells = new List<Cell>();
            foreach (var cellId in cells)
            {
                try
                {
                    var cached = _memCache.Get<ulong, Cell>(cellId);
                    if (cached == null)
                    {
                        cached = new Cell(cellId);
                        _memCache.Set(cellId, cached);
                    }

                    // Filter cells not already cached, cached cells expire every 60 minutes.
                    // Once expired they will be updated when found again.
                    var now = DateTime.UtcNow.ToTotalSeconds();
                    var needsUpdate = cached.Updated > now - CellScanIntervalS;
                    if (!needsUpdate)
                        continue;

                    s2cells.Add(cached);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateCellsAsync: {ex}");
                }
            }

            await _dataConsumerService.AddEntitiesAsync(SqlQueryType.CellOnMergeUpdate, s2cells);
            ProtoDataStatistics.Instance.TotalS2CellsProcessed += (uint)s2cells.Count;

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
                if (cached == null)
                {
                    cached = new Cell(cellId);
                    _memCache.Set(cellId, cached);
                }

                // Filter cells not already cached, cached cells expire every 60 minutes.
                // Once expired they will be updated when found again.
                var now = DateTime.UtcNow.ToTotalSeconds();
                var needsUpdate = cached.Updated > now - CellScanIntervalS;
                if (!needsUpdate)
                    return;

                await _dataConsumerService.AddEntityAsync(SqlQueryType.CellOnMergeUpdate, cached);
                ProtoDataStatistics.Instance.TotalS2CellsProcessed++;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateCellsAsync: {ex}");
            }
        }

        private async Task UpdateClientWeatherAsync(MySqlConnection connection, IEnumerable<ClientWeatherProto> clientWeather)
        {
            var webhooks = new List<Weather>();

            try
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                // Convert weather protos to Weather models
                var weather = clientWeather
                    .Where(wcell =>
                    {
                        // Filter cells not already cached or updated recently
                        var cached = _memCache.Get<long, Weather>(wcell.S2CellId);
                        return (cached?.Updated ?? now) > now - CellScanIntervalS;
                    })
                    // Instantiate new Weather entity models for new weather cells not cached
                    .Select(wcell => new Weather(wcell));

                // Check if any new/need to be updated weather cells, otherwise skip
                if (!weather.Any())
                    return;

                foreach (var wcell in weather)
                {
                    await wcell.UpdateAsync(connection, _memCache, skipOldLookup: true);
                    await _dataConsumerService.AddEntityAsync(SqlQueryType.WeatherOnMergeUpdate, wcell);
                    ProtoDataStatistics.Instance.TotalClientWeatherCellsProcessed++;

                    // Check if 'SendWebhook' flag was triggered, if so relay webhook payload to communicator
                    if (wcell.SendWebhook)
                    {
                        webhooks.Add(wcell);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateClientWeatherAsync: {ex.InnerException?.Message ?? ex.Message}");
            }

            if (webhooks.Any())
            {
                foreach (var weather in webhooks)
                {
                    await SendWebhookPayloadAsync(WebhookPayloadType.Weather, weather);
                }
            }
        }

        private async Task UpdatePokemonAsync(string requestId, MySqlConnection connection,
            IEnumerable<dynamic> wildPokemon, IEnumerable<dynamic> nearbyPokemon, IEnumerable<dynamic> mapPokemon,
            IEnumerable<dynamic> encounters, IEnumerable<dynamic> diskEncounters)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var ts = now * 1000;
            var webhooks = new List<Pokemon>();

            if (Options.ProcessWildPokemon)
            {
                _logger.LogInformation($"[{requestId}] Parsing {wildPokemon.Count()} wild pokemon");
                foreach (var wild in wildPokemon)
                {
                    try
                    {
                        var cellId = wild.cell;
                        var data = (WildPokemonProto)wild.data;
                        var timestampMs = wild.timestampMs;
                        var username = wild.username;
                        var isEvent = wild.isEvent;
                        var pokemon = new Pokemon(data, cellId, username, isEvent);
                        var spawnpoint = await pokemon.ParseSpawnpointAsync(connection, _memCache, data.TimeTillHiddenMs, timestampMs);
                        if (spawnpoint != null)
                        {
                            await _dataConsumerService.AddEntityAsync(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
                            ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
                        }

                        await pokemon.UpdateAsync(connection, _memCache, updateIv: false, skipOldLookup: false);
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
                }
            }

            if (Options.ProcessNearbyPokemon)
            {
                _logger.LogInformation($"[{requestId}] Parsing {nearbyPokemon.Count()} nearby pokemon");
                foreach (var nearby in nearbyPokemon)
                {
                    try
                    {
                        var cellId = nearby.cell;
                        var data = (NearbyPokemonProto)nearby.data;
                        var username = nearby.username;
                        var isEvent = nearby.isEvent;
                        var pokemon = new Pokemon(connection, _memCache, data, cellId, username, isEvent);

                        await pokemon.UpdateAsync(connection, _memCache, updateIv: false, skipOldLookup: false);
                        await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonIgnoreOnMerge, pokemon);
                        ProtoDataStatistics.Instance.TotalNearbyPokemonProcessed++;

                        if (pokemon.SendWebhook)
                        {
                            webhooks.Add(pokemon);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"UpdateNearbyPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }

            if (Options.ProcessMapPokemon)
            {
                _logger.LogInformation($"[{requestId}] Parsing {mapPokemon.Count()} map pokemon");
                foreach (var map in mapPokemon)
                {
                    try
                    {
                        var cellId = map.cell;
                        var data = (MapPokemonProto)map.data;
                        var username = map.username;
                        var isEvent = map.isEvent;
                        var pokemon = new Pokemon(connection, _memCache, data, cellId, username, isEvent);
                        await pokemon.UpdateAsync(connection, _memCache, updateIv: false, skipOldLookup: false);

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

                        pokemon.AddDiskEncounter(cachedDiskEncounter, username);
                        await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipOldLookup: true);
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
                }
            }

            if (Options.ProcessEncounters)
            {
                _logger.LogInformation($"[{requestId}] Parsing {encounters.Count()} pokemon encounters");
                foreach (var encounter in encounters)
                {
                    try
                    {
                        var data = (EncounterOutProto)encounter.data;
                        var username = encounter.username;
                        var isEvent = encounter.isEvent;
                        var encounterId = data.Pokemon.EncounterId.ToString();
                        var pokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, encounterId, _memCache);
                        var isNew = false;
                        if (pokemon == null)
                        {
                            // New Pokemon
                            var cellId = S2CellExtensions.S2CellIdFromLatLng(data.Pokemon.Latitude, data.Pokemon.Longitude);
                            await UpdateCellAsync(cellId.Id);

                            pokemon = new Pokemon(data.Pokemon, cellId.Id, username, isEvent);
                            isNew = true;
                        }
                        await pokemon.AddEncounterAsync(data, username);

                        var spawnpoint = await pokemon.ParseSpawnpointAsync(connection, _memCache, data.Pokemon.TimeTillHiddenMs, ts);
                        if (spawnpoint != null)
                        {
                            await _dataConsumerService.AddEntityAsync(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
                            ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
                        }

                        if (pokemon.HasIvChanges)
                        {
                            SetPvpRankings(pokemon);
                        }

                        await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipOldLookup: isNew);
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
                }
            }

            if (Options.ProcessDiskEncounters)
            {
                _logger.LogInformation($"[{requestId}] Parsing {diskEncounters.Count()} pokemon disk encounters");
                foreach (var diskEncounter in diskEncounters)
                {
                    try
                    {
                        var data = (DiskEncounterOutProto)diskEncounter.data;
                        var username = diskEncounter.username;
                        var isEvent = diskEncounter.isEvent;
                        var displayId = Convert.ToString(data.Pokemon.PokemonDisplay.DisplayId);
                        var pokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, displayId, _memCache);
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

                        await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipOldLookup: true);
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
                }
            }


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

        private async Task UpdateFortsAsync(MySqlConnection connection, IEnumerable<dynamic> forts, string? username)
        {
            var webhooks = new Dictionary<WebhookType, List<BaseEntity>>();

            foreach (var fort in forts)
            {
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
                            var pokestopWebhooks = await pokestop.UpdateAsync(connection, _memCache, updateQuest: false, skipOldLookup: false);

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

                            await ParseIncidentsAsync(connection, pokestop);
                            break;
                        case FortType.Gym:
                            // Init Gym model from fort proto data
                            var gym = new Gym(data, cellId);
                            var gymWebhooks = await gym.UpdateAsync(connection, _memCache, skipOldLookup: false);

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
            }


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


            // Send found/nearby forts with gRPC service for leveling instance
            var lvlForts = forts
                .Select(fort => fort.data)
                .Select(fort => (PokemonFortProto)fort)
                .Where(fort => fort.FortType == FortType.Checkpoint)
                .ToList();
            // Ensure that the account username is set, otherwise ignore relaying
            // fort data for leveling instance
            if (!string.IsNullOrEmpty(username) && lvlForts.Any())
            {
                await SendGymsAsync(lvlForts, username);
            }
        }

        private async Task UpdateFortDetailsAsync(MySqlConnection connection, IEnumerable<dynamic> fortDetails)
        {
            var fortDetailsPokestops = fortDetails
                .Where(fort => fort.data.FortType == FortType.Checkpoint);
            // Convert fort details protos to Pokestop models
            foreach (var fort in fortDetailsPokestops)
            {
                try
                {
                    var data = (FortDetailsOutProto)fort.data;
                    var pokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, data.Id, _memCache);
                    if (pokestop == null)
                        continue;

                    pokestop.AddDetails(data);
                    var pokestopWebhooks = await pokestop.UpdateAsync(connection, _memCache, skipOldLookup: true);

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
            }

            var fortDetailsGyms = fortDetails
                .Where(fort => fort.data.FortType == FortType.Gym);
            // Convert fort details protos to Gym models
            foreach (var fort in fortDetailsGyms)
            {
                try
                {
                    var data = (FortDetailsOutProto)fort.data;
                    var gym = await EntityRepository.GetEntityAsync<string, Gym>(connection, data.Id, _memCache);
                    if (gym == null)
                        continue;

                    gym.AddDetails(data);
                    var gymWebhooks = await gym.UpdateAsync(connection, _memCache, skipOldLookup: true);

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
            }
        }

        private async Task UpdateGymInfoAsync(MySqlConnection connection, IEnumerable<dynamic> gymInfos)
        {
            var webhooks = new Dictionary<WebhookType, List<BaseEntity>>();

            // Convert gym info protos to Gym models
            foreach (var gymInfo in gymInfos)
            {
                try
                {
                    var data = (GymGetInfoOutProto)gymInfo.data;
                    var fortId = data.GymStatusAndDefenders.PokemonFortProto.FortId;
                    var gym = await EntityRepository.GetEntityAsync<string, Gym>(connection, fortId, _memCache);
                    if (gym == null)
                        continue;

                    gym.AddDetails(data);
                    var gymWebhooks = await gym.UpdateAsync(connection, _memCache, skipOldLookup: true);

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
            }

            if (webhooks.Any())
            {
                foreach (var (key, value) in webhooks)
                {
                    var type = ConvertWebhookType(key);
                    await SendWebhookPayloadAsync(type, value);
                }
            }
        }

        private async Task UpdateQuestsAsync(MySqlConnection connection, IEnumerable<dynamic> quests)
        {
            // Convert quest protos to Pokestop models
            var webhooks = new Dictionary<WebhookType, List<Pokestop>>();

            foreach (var quest in quests)
            {
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
                    var questWebhooks = await pokestop.UpdateAsync(connection, _memCache, updateQuest: true, skipOldLookup: true);

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
            }

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

        //private async Task UpdateEncountersAsync(MySqlConnection connection, IEnumerable<dynamic> encounters)
        //{
        //    var now = DateTime.UtcNow.ToTotalSeconds();
        //    var timestampMs = now * 1000;
        //    var webhooks = new List<Pokemon>();

        //    foreach (var encounter in encounters)
        //    {
        //        try
        //        {
        //            var data = (EncounterOutProto)encounter.data;
        //            var username = encounter.username;
        //            var isEvent = encounter.isEvent;
        //            var encounterId = data.Pokemon.EncounterId.ToString();
        //            var pokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, encounterId, _memCache);
        //            var isNew = false;
        //            if (pokemon == null)
        //            {
        //                // New Pokemon
        //                var cellId = S2CellExtensions.S2CellIdFromLatLng(data.Pokemon.Latitude, data.Pokemon.Longitude);
        //                await UpdateCellAsync(cellId.Id);

        //                pokemon = new Pokemon(data.Pokemon, cellId.Id, username, isEvent);
        //                isNew = true;
        //            }
        //            await pokemon.AddEncounterAsync(data, username);

        //            var spawnpoint = await ParseSpawnpointAsync(connection, pokemon, data.Pokemon.TimeTillHiddenMs, timestampMs);
        //            if (spawnpoint != null)
        //            {
        //                await _dataConsumerService.AddEntityAsync(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
        //                ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
        //            }

        //            if (pokemon.HasIvChanges)
        //            {
        //                SetPvpRankings(pokemon);
        //            }

        //            // TODO: await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipOldLookup: isNew);
        //            await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonOnMergeUpdate, pokemon);
        //            ProtoDataStatistics.Instance.TotalPokemonEncountersProcessed++;

        //            if (pokemon.SendWebhook)
        //            {
        //                //await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
        //                webhooks.Add(pokemon);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError($"UpdateEncountersAsync: {ex}");
        //        }
        //    }

        //    if (webhooks.Any())
        //    {
        //        foreach (var pokemon in webhooks)
        //        {
        //            await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
        //        }
        //    }
        //}

        //private async Task UpdateDiskEncountersAsync(MySqlConnection connection, IEnumerable<dynamic> diskEncounters)
        //{
        //    var webhooks = new List<Pokemon>();

        //    foreach (var diskEncounter in diskEncounters)
        //    {
        //        try
        //        {
        //            var data = (DiskEncounterOutProto)diskEncounter.data;
        //            var username = diskEncounter.username;
        //            var isEvent = diskEncounter.isEvent;
        //            var displayId = Convert.ToString(data.Pokemon.PokemonDisplay.DisplayId);
        //            var pokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, displayId, _memCache);
        //            if (pokemon == null)
        //            {
        //                _diskCache.Set(displayId, data, TimeSpan.FromMinutes(30));
        //                _logger.LogInformation($"Disk encounter with id '{displayId}' added to cache");
        //                continue;
        //            }

        //            pokemon.AddDiskEncounter(data, username);
        //            if (pokemon.HasIvChanges)
        //            {
        //                SetPvpRankings(pokemon);
        //            }

        //            await pokemon.UpdateAsync(connection, _memCache, updateIv: true, skipOldLookup: true);
        //            await _dataConsumerService.AddEntityAsync(SqlQueryType.PokemonOnMergeUpdate, pokemon);
        //            ProtoDataStatistics.Instance.TotalPokemonDiskEncountersProcessed++;

        //            if (pokemon.SendWebhook)
        //            {
        //                webhooks.Add(pokemon);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
        //        }
        //    }

        //    if (webhooks.Any())
        //    {
        //        foreach (var pokemon in webhooks)
        //        {
        //            await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
        //        }
        //    }
        //}

        #endregion

        #region Parsing Methods

        private async Task ParseIncidentsAsync(MySqlConnection connection, Pokestop pokestop)
        {
            if (!(pokestop.Incidents?.Any() ?? false))
                return;

            var webhooks = new List<PokestopWithIncident>();

            // Loop incidents
            foreach (var incident in pokestop.Incidents)
            {
                try
                {
                    // TODO: await incident.UpdateAsync(connection, _memCache, skipOldLookup: false);
                    // TODO: if (incident.HasChanges)
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
            }

            if (webhooks.Any())
            {
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

        private async Task SendGymsAsync(List<PokemonFortProto> forts, string username)
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

    public class ThreadManager
    {
        private readonly int _maxThreadCount;
        private int _threadsUsed;

        public int ThreadsUsed => _threadsUsed;

        public bool IsThreadAvailable => _threadsUsed < _maxThreadCount;

        public ThreadManager(int maxThreadCount = 100)
        {
            _maxThreadCount = maxThreadCount;
        }

        public void TakeThread()
        {
            if (_threadsUsed < _maxThreadCount)
            {
                Interlocked.Increment(ref _threadsUsed);
            }
        }

        public void GiveThread()
        {
            if (_threadsUsed <= _maxThreadCount)
            {
                Interlocked.Decrement(ref _threadsUsed);
            }
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