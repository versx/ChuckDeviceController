namespace ChuckDeviceController.Services.DataProcessor;

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

using ChuckDeviceController.Caching.Memory;
using ChuckDeviceController.Collections;
using ChuckDeviceController.Common;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Geometry.Extensions;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Protos;
using ChuckDeviceController.Pvp;
using ChuckDeviceController.Pvp.Models;
using ChuckDeviceController.Services.DataConsumer;

public class DataProcessorService : TimedHostedService, IDataProcessorService
{
    #region Variables

    private static readonly TimeSpan _diskCacheExpiry = TimeSpan.FromMinutes(30);
    private readonly SemaphoreSlim _semParser;// = new(1, 1);

    private readonly ILogger<IDataProcessorService> _logger;
    private readonly SafeCollection<DataQueueItem> _taskQueue;
    private readonly IMemoryCache _diskCache;
    private readonly IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse> _grpcProtoClient;
    private readonly IGrpcClient<WebhookPayload.WebhookPayloadClient, WebhookPayloadRequest, WebhookPayloadResponse> _grpcWebhookClient;
    private readonly ClearGymsCache _gymIdsPerCell;
    private readonly ClearPokestopsCache _stopIdsPerCell;

    private readonly IMemoryCacheService _memCache;
    private readonly IWebHostEnvironment _env;
    private readonly DataConsumerQueue _dataConsumerQueue;
    //private readonly ThreadManager _threadManager = new(maxThreadCount: 25);

    #endregion

    #region Properties

    public DataProcessorOptionsConfig Options { get; }

    public bool EnableWebhooks { get; }

    #endregion

    #region Constructor

    public DataProcessorService(
        ILogger<IDataProcessorService> logger,
        IOptions<DataProcessorOptionsConfig> options,
        SafeCollection<DataQueueItem> taskQueue,
        IMemoryCache diskCache,
        IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse> grpcProtoClient,
        IGrpcClient<WebhookPayload.WebhookPayloadClient, WebhookPayloadRequest, WebhookPayloadResponse> grpcWebhookClient,
        ClearGymsCache gymIdsPerCell,
        ClearPokestopsCache stopIdsPerCell,
        IMemoryCacheService memCache,
        IWebHostEnvironment env,
        IConfiguration configuration,
        DataConsumerQueue dataConsumerQueue)
        : base(logger, options?.Value?.IntervalS ?? DataProcessorOptionsConfig.DefaultIntervalS)
    {
        Options = options?.Value ?? new();
        EnableWebhooks = configuration.GetValue<bool>("Webhooks:Enabled");

        _logger = logger;
        _taskQueue = taskQueue;
        _dataConsumerQueue = dataConsumerQueue;
        _diskCache = diskCache;
        _grpcProtoClient = grpcProtoClient;
        _grpcWebhookClient = grpcWebhookClient;
        _gymIdsPerCell = gymIdsPerCell;
        _stopIdsPerCell = stopIdsPerCell;
        
        _memCache = memCache;
        _env = env;
        _semParser = new SemaphoreSlim(Options.ParsingConcurrencyLevel, Options.ParsingConcurrencyLevel);
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
        await _semParser.WaitAsync(stoppingToken);

        try
        {
            if (_taskQueue.Count == 0)
            {
                _semParser.Release();
                return;
            }

            var protos = _taskQueue.Take((int)Options.Queue.MaximumBatchSize, stoppingToken);
            if (!(protos?.Any() ?? false))
            {
                _semParser.Release();
                return;
            }

            var manifest = ProtoDataManifest.ParseProtoManifest(protos);
            //var result = ProcessprotosAsync(connection, items, stoppingToken);
            //while (!result.IsCompleted)
            //{
            //    //Console.WriteLine($"Waiting for DataProcessor to finish...");
            //    //Thread.Sleep(1);
            //    await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken);
            //}
            //Console.WriteLine($"Finished data processing...");
            new Thread(async () =>
            {
                using var connection = await EntityRepository.CreateConnectionAsync(
                    $"{nameof(DataProcessorService)}::RunJobAsync",
                    openConnection: true,
                    runLeakWatcher: true,
                    waitTimeS: 30,
                    connectionLeakTimeoutS: 300,
                    stoppingToken
                );
                if (connection == null)
                {
                    _logger.LogError($"Failed to connect to MySQL database server!");
                    return;
                }
                await ProcessprotosAsync(connection, manifest, stoppingToken);
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

        _semParser.Release();
        Thread.Sleep(1);
    }

    private async Task ProcessprotosAsync(MySqlConnection connection, ProtoDataManifest protos, CancellationToken stoppingToken = default)
    {
        if (protos == null)
            return;

        CheckQueueLength();

        var sw = new Stopwatch();
        if (Options.ShowProcessingTimes)
        {
            sw.Start();
        }

        ProtoDataStatistics.Instance.TotalPlayerDataReceived += (uint)protos.PlayerData.Count();
        ProtoDataStatistics.Instance.TotalS2CellsReceived += (uint)protos.Cells.Count();
        ProtoDataStatistics.Instance.TotalClientWeatherCellsReceived += (uint)protos.Weather.Count();
        ProtoDataStatistics.Instance.TotalFortsReceived += (uint)protos.Forts.Count();
        ProtoDataStatistics.Instance.TotalFortDetailsReceived += (uint)protos.FortDetails.Count();
        ProtoDataStatistics.Instance.TotalGymInfoReceived += (uint)protos.GymInfo.Count();
        ProtoDataStatistics.Instance.TotalWildPokemonReceived += (uint)protos.WildPokemon.Count();
        ProtoDataStatistics.Instance.TotalNearbyPokemonReceived += (uint)protos.NearbyPokemon.Count();
        ProtoDataStatistics.Instance.TotalMapPokemonReceived += (uint)protos.MapPokemon.Count();
        ProtoDataStatistics.Instance.TotalQuestsReceived += (uint)protos.Quests.Count();
        ProtoDataStatistics.Instance.TotalPokemonEncountersReceived += (uint)protos.Encounters.Count();
        ProtoDataStatistics.Instance.TotalPokemonDiskEncountersReceived += (uint)protos.DiskEncounters.Count();

        var requestId = Guid.NewGuid().ToString()[..8];
        try
        {
            // Parse player account data
            if (Options.ProcessPlayerData && protos.PlayerData.Any())
            {
                await UpdatePlayerDataAsync(requestId, connection, protos.PlayerData);
            }

            // Parse S2 cells
            if (Options.ProcessCells && protos.Cells.Any())
            {
                await UpdateCellsAsync(requestId, protos.Cells);
            }

            // Parse weather cells
            if (Options.ProcessWeather && protos.Weather.Any())
            {
                await UpdateClientWeatherAsync(requestId, connection, protos.Weather);
            }

            // Parse Pokestop and Gym forts
            if (Options.ProcessForts && protos.Forts.Any())
            {
                await UpdateFortsAsync(requestId, connection, protos.Forts);
            }

            // Parse Fort Details
            if (Options.ProcessFortDetails && protos.FortDetails.Any())
            {
                await UpdateFortDetailsAsync(requestId, connection, protos.FortDetails);
            }

            // Parse gym info
            if (Options.ProcessGymInfo && protos.GymInfo.Any())
            {
                await UpdateGymInfoAsync(requestId, connection, protos.GymInfo);
            }

            // Parse wild pokemon
            if (Options.ProcessWildPokemon && protos.WildPokemon.Any())
            {
                await UpdateWildPokemonAsync(requestId, connection, protos.WildPokemon);
            }

            // Parse nearby pokemon
            if (Options.ProcessNearbyPokemon && protos.NearbyPokemon.Any())
            {
                await UpdateNearbyPokemonAsync(requestId, connection, protos.NearbyPokemon);
            }

            // Parse map pokemon
            if (Options.ProcessMapPokemon && protos.MapPokemon.Any())
            {
                await UpdateMapPokemonAsync(requestId, connection, protos.MapPokemon);
            }

            // Parse pokemon encounters
            if (Options.ProcessEncounters && protos.Encounters.Any())
            {
                await UpdateEncountersAsync(requestId, connection, protos.Encounters);
            }

            // Parse pokemon disk encounters
            if (Options.ProcessDiskEncounters && protos.DiskEncounters.Any())
            {
                await UpdateDiskEncountersAsync(requestId, connection, protos.DiskEncounters);
            }

            // Parse pokestop quests
            if (Options.ProcessQuests && protos.Quests.Any())
            {
                await UpdateQuestsAsync(requestId, connection, protos.Quests);
            }

            //Parallel.ForEach(tasks, async (task, token) => await task.ConfigureAwait(false));
            ProtoDataStatistics.Instance.TotalEntitiesProcessed += (uint)protos.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            var time = Options.ShowProcessingTimes ? $" in {totalSeconds}s" : "";
            _logger.LogInformation("[{RequestId}] Finished parsing {Count:N0} data entities{Time}", requestId, protos.Count, time);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Data Handling Methods

    private async Task UpdatePlayerDataAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> playerData)
    {
        var webhooks = new List<Account>();
        var count = playerData.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} player accounts", requestId, count);
        }

        foreach (var player in playerData)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing player account {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                var username = (string)player.username;
                var data = (GetPlayerOutProto)player.gpr;
                var account = await EntityRepository.GetEntityAsync<string, Account>(connection, username, _memCache);
                if (account == null)
                {
                    _logger.LogWarning("[{RequestId}] Failed to retrieve account with username '{Username}' from cache and database to update account status", requestId, username);
                    continue;
                }

                await account.UpdateAsync(data, _memCache);
                AddEntity(SqlQueryType.AccountOnMergeUpdate, account);
                ProtoDataStatistics.Instance.TotalPlayerDataProcessed++;

                if (account.SendWebhook)
                {
                    webhooks.Add(account);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdatePlayerDataAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} player accounts parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            await SendWebhooksAsync(WebhookType.Accounts, webhooks);
        }
    }

    private async Task UpdateCellsAsync(string requestId, IEnumerable<ulong> cells)
    {
        var s2cells = new List<Cell>();
        var count = cells.Count();
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} s2 cells", requestId, count);
        }

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
                    var needsUpdate = cached.Updated < now - Options.CellScanIntervalS;
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
                _logger.LogError("UpdateCellsAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
        }

        AddEntities(SqlQueryType.CellOnMergeUpdate, s2cells);
        ProtoDataStatistics.Instance.TotalS2CellsProcessed += (uint)s2cells.Count;

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} s2 cells parsed with {S2CellsIgnored:N0} ignored in {TotalSeconds}s", requestId, count, count - s2cells.Count, totalSeconds);
        }

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

        await Task.CompletedTask;
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
                var needsUpdate = cached.Updated < now - Options.CellScanIntervalS;
                if (!needsUpdate)
                    return;
            }
            else
            {
                cached = new Cell(cellId);
                _memCache.Set(cellId, cached);
            }

            AddEntity(SqlQueryType.CellOnMergeUpdate, cached);
            ProtoDataStatistics.Instance.TotalS2CellsProcessed++;
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateCellAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
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

        await Task.CompletedTask;
    }

    private async Task UpdateClientWeatherAsync(string requestId, MySqlConnection connection, IEnumerable<ClientWeatherProto> clientWeather)
    {
        var webhooks = new List<Weather>();
        var count = clientWeather.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} client weather cells", requestId, count);
        }

        // Convert weather protos to Weather models
        var weatherCells = clientWeather
            .Where(wcell =>
            {
                // Filter cells not already cached or updated recently
                var cached = _memCache.Get<long, Weather>(wcell.S2CellId);
                if (cached == null)
                    return true;

                var now = DateTime.UtcNow.ToTotalSeconds();
                var needsUpdate = cached.Updated < now - Options.WeatherCellScanIntervalS;
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
            })
            .ToList();

        // Check if any new/need to be updated weather cells, otherwise skip
        if (!weatherCells.Any())
            return;

        foreach (var wcell in weatherCells)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing weather cell {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                Weather? oldWeather = null;
                //var oldWeather = await EntityRepository.GetEntityAsync<long, Weather>(connection, wcell.Id, _memCache);
                await wcell.UpdateAsync(oldWeather, _memCache);

                if (wcell.HasChanges)
                {
                    AddEntity(SqlQueryType.WeatherOnMergeUpdate, wcell);
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
                _logger.LogError("UpdateClientWeatherAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} weather cells parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            await SendWebhooksAsync(WebhookType.Weather, webhooks);
        }
    }

    private async Task UpdateWildPokemonAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> wildPokemon)
    {
        var webhooks = new List<Pokemon>();
        var count = wildPokemon.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} wild pokemon", requestId, count);
        }

        foreach (var wild in wildPokemon)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing wild pokemon {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                var cellId = (ulong)wild.cell;
                var data = (WildPokemonProto)wild.data;
                var timestampMs = (ulong)wild.timestampMs;
                var username = (string)wild.username;
                var isEvent = (bool)wild.isEvent;
                var pokemon = Pokemon.ParsePokemonFromWild(data, cellId, username, isEvent);
                var spawnpoint = await pokemon.ParseSpawnpointAsync(connection, _memCache, data.TimeTillHiddenMs, timestampMs);
                if (spawnpoint != null)
                {
                    AddEntity(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
                    ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
                }

                Pokemon? oldPokemon = null;
                //var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache, skipCache: true, setCache: false);
                await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: false, setPvpRankings: new Action<Pokemon>(SetPvpRankings));
                AddEntity(SqlQueryType.PokemonIgnoreOnMerge, pokemon);
                ProtoDataStatistics.Instance.TotalWildPokemonProcessed++;

                if (pokemon.SendWebhook)
                {
                    webhooks.Add(pokemon);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateWildPokemonAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} wild pokemon parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            _logger.LogDebug("[{RequestId}] Sending {Count:N0} pokemon webhooks", requestId, webhooks.Count);
            await SendWebhooksAsync(WebhookType.Pokemon, webhooks);

            // Send pokemon to Configurator endpoint for IV stats
            await SendPokemonAsync(webhooks);
        }
    }

    private async Task UpdateNearbyPokemonAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> nearbyPokemon)
    {
        var webhooks = new List<Pokemon>();
        var count = nearbyPokemon.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} nearby pokemon", requestId, count);
        }

        foreach (var nearby in nearbyPokemon)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing nearby pokemon {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                var cellId = (ulong)nearby.cell;
                var data = (NearbyPokemonProto)nearby.data;
                var username = (string)nearby.username;
                var isEvent = (bool)nearby.isEvent;
                var pokemon = await Pokemon.ParsePokemonFromNearby(connection, _memCache, data, cellId, username, isEvent);
                if (pokemon == null)
                {
                    // Failed to get pokestop
                    _logger.LogWarning("Failed to find pokestop with id '{FortId}' for nearby pokemon: {EncounterId}", data.FortId, data.EncounterId);
                    continue;
                }

                Pokemon? oldPokemon = null;
                //var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache, skipCache: true, setCache: false);
                await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: false, setPvpRankings: new Action<Pokemon>(SetPvpRankings));
                AddEntity(SqlQueryType.PokemonIgnoreOnMerge, pokemon);
                ProtoDataStatistics.Instance.TotalNearbyPokemonProcessed++;

                //_logger.LogInformation($"[{requestId}] Updated nearby pokemon {index:N0}/{count:N0}");

                if (pokemon.SendWebhook)
                {
                    webhooks.Add(pokemon);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateNearbyPokemonAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} nearby pokemon parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            _logger.LogDebug("Sending {Count:N0} pokemon webhooks", webhooks.Count);
            await SendWebhooksAsync(WebhookType.Pokemon, webhooks);

            // Send pokemon to Configurator endpoint for IV stats
            await SendPokemonAsync(webhooks);
        }
    }

    private async Task UpdateMapPokemonAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> mapPokemon)
    {
        var webhooks = new List<Pokemon>();
        var count = mapPokemon.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} map pokemon", requestId, count);
        }

        foreach (var map in mapPokemon)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing map pokemon {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                var cellId = (ulong)map.cell;
                var data = (MapPokemonProto)map.data;
                var username = (string)map.username;
                var isEvent = (bool)map.isEvent;

                // Check if we have a pending disk encounter cache
                var displayId = data.PokemonDisplay.DisplayId;
                var cachedDiskEncounter = _diskCache.Get<DiskEncounterOutProto>(displayId);
                if (cachedDiskEncounter == null)
                {
                    // Failed to get DiskEncounter from cache
                    _logger.LogWarning("Unable to fetch cached Pokemon disk encounter with id '{DisplayId}' from cache", displayId);
                    continue;
                }

                // Thanks Fabio <3
                _logger.LogDebug("Found Pokemon disk encounter with id '{DisplayId}' in cache", displayId);

                // TODO: Lookup old pokemon first, if not null update properties from map proto
                var pokemon = await Pokemon.ParsePokemonFromMap(connection, _memCache, data, cellId, username, isEvent);
                pokemon.AddDiskEncounter(cachedDiskEncounter, username);

                var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache, skipCache: true, setCache: false);
                await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: true, setPvpRankings: new Action<Pokemon>(SetPvpRankings));
                AddEntity(SqlQueryType.PokemonOnMergeUpdate, pokemon);
                ProtoDataStatistics.Instance.TotalMapPokemonProcessed++;

                if (pokemon.SendWebhook)
                {
                    webhooks.Add(pokemon);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateMapPokemonAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} map pokemon parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            _logger.LogDebug("Sending {Count:N0} pokemon webhooks", webhooks.Count);
            await SendWebhooksAsync(WebhookType.Pokemon, webhooks);

            // Send pokemon to Configurator endpoint for IV stats
            await SendPokemonAsync(webhooks);
        }
    }

    private async Task UpdateFortsAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> forts)
    {
        var webhooks = new Dictionary<WebhookType, List<BaseEntity>>();
        var incidents = new Dictionary<Pokestop, List<Incident>>();
        var count = forts.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} forts", requestId, count);
        }

        foreach (var fort in forts)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing fort {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                var cellId = (ulong)fort.cell;
                var data = (PokemonFortProto)fort.data;
                var username = (string)fort.username;

                switch (data.FortType)
                {
                    case FortType.Checkpoint:
                        // Init Pokestop model from fort proto data
                        var pokestop = new Pokestop(data, cellId);
                        Pokestop? oldPokestop = null;
                        //var oldPokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, pokestop.Id, _memCache);
                        var pokestopWebhooks = await pokestop.UpdateAsync(oldPokestop, _memCache, updateQuest: false);

                        AddEntity(SqlQueryType.PokestopIgnoreOnMerge, pokestop);
                        _stopIdsPerCell.AddPokestop(cellId, data.FortId);

                        if (pokestopWebhooks.Any())
                        {
                            GroupWebhooks(pokestopWebhooks, webhooks);
                        }

                        if (Options.ProcessIncidents && (pokestop.Incidents?.Any() ?? false))
                        {
                            if (incidents.TryGetValue(pokestop, out var value))
                            {
                                value.AddRange(pokestop.Incidents);
                            }
                            else
                            {
                                incidents.Add(pokestop, pokestop.Incidents?.ToList() ?? new());
                            }
                        }
                        break;
                    case FortType.Gym:
                        // Init Gym model from fort proto data
                        var gym = new Gym(data, cellId);
                        Gym? oldGym = null;
                        //var oldGym = await EntityRepository.GetEntityAsync<string, Gym>(connection, gym.Id, _memCache);
                        var gymWebhooks = await gym.UpdateAsync(oldGym, _memCache);

                        AddEntity(SqlQueryType.GymOnMergeUpdate, gym);
                        _gymIdsPerCell.AddGym(cellId, data.FortId);

                        if (gymWebhooks.Any())
                        {
                            GroupWebhooks(gymWebhooks, webhooks);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateFortsAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
            ProtoDataStatistics.Instance.TotalFortsProcessed++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} forts parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            await SendWebhooksAsync(webhooks);
        }

        if (Options.ProcessIncidents)
        {
            await UpdateIncidentsAsync(requestId, connection, incidents);
        }

        // Send found/nearby forts with gRPC service for leveling instance
        var lvlForts = forts
            .Where(fort => ((PokemonFortProto)fort.data).FortType == FortType.Checkpoint)
            .ToList();
        if (!lvlForts.Any())
            return;

        await SendPokestopsAsync(lvlForts);
    }

    private async Task UpdateFortDetailsAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> fortDetails)
    {
        var webhooks = new Dictionary<WebhookType, List<object>>();
        var sw = new Stopwatch();

        if (Options.ShowProcessingCount)
        {
            sw.Start();
        }

        var fortDetailsPokestops = fortDetails
            .Where(fort => fort.data.FortType == FortType.Checkpoint);
        if (fortDetailsPokestops.Any())
        {
            var count = fortDetailsPokestops.Count();
            var index = 1;

            if (Options.ShowProcessingTimes)
            {
                _logger.LogInformation("[{RequestId}] Parsing {Count:N0} pokestop fort details", requestId, count);
            }

            // Convert fort details protos to Pokestop models
            foreach (var fort in fortDetailsPokestops)
            {
                if (Options.ShowProcessingCount)
                {
                    _logger.LogInformation("[{RequestId}] Parsing pokestop fort detail {Index:N0}/{Count:N0}", requestId, index, count);
                }

                try
                {
                    var data = (FortDetailsOutProto)fort.data;
                    var pokestop = await EntityRepository.GetEntityAsync<string, Pokestop>(connection, data.Id, _memCache);
                    if (pokestop == null)
                        continue;

                    pokestop.AddDetails(data);
                    var pokestopWebhooks = await pokestop.UpdateAsync(null, _memCache);

                    if (pokestop.HasChanges)
                    {
                        AddEntity(SqlQueryType.PokestopDetailsOnMergeUpdate, pokestop);
                        ProtoDataStatistics.Instance.TotalFortDetailsProcessed++;
                    }

                    if (pokestopWebhooks.Any())
                    {
                        GroupWebhooks(pokestopWebhooks, webhooks);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("UpdateFortDetailsAsync[Pokestop]: {Message}", ex.InnerException?.Message ?? ex.Message);
                }
                index++;
            }

            if (Options.ShowProcessingTimes)
            {
                sw.Stop();
                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
                _logger.LogInformation("[{RequestId}] {Count:N0} pokestop fort details parsed in {TotalSeconds}s", requestId, count, totalSeconds);
            }
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Reset();
            sw.Start();
        }

        var fortDetailsGyms = fortDetails
            .Where(fort => fort.data.FortType == FortType.Gym);
        if (fortDetailsGyms.Any())
        {
            var count = fortDetailsGyms.Count();
            var index = 1;

            if (Options.ShowProcessingTimes)
            {
                _logger.LogInformation("[{RequestId}] Parsing {Count:N0} gym fort details", requestId, count);
            }

            // Convert fort details protos to Gym models
            foreach (var fort in fortDetailsGyms)
            {
                if (Options.ShowProcessingCount)
                {
                    _logger.LogInformation("[{RequestId}] Parsing fort detail {Index:N0}/{Count:N0}", requestId, index, count);
                }

                try
                {
                    var data = (FortDetailsOutProto)fort.data;
                    var gym = await EntityRepository.GetEntityAsync<string, Gym>(connection, data.Id, _memCache);
                    if (gym == null)
                        continue;

                    gym.AddDetails(data);
                    var gymWebhooks = await gym.UpdateAsync(null, _memCache);

                    if (gym.HasChanges)
                    {
                        AddEntity(SqlQueryType.GymDetailsOnMergeUpdate, gym);
                        ProtoDataStatistics.Instance.TotalFortDetailsProcessed++;
                    }

                    if (gymWebhooks.Any())
                    {
                        GroupWebhooks(gymWebhooks, webhooks);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("UpdateFortDetailsAsync[Gym]: {Message}", ex.InnerException?.Message ?? ex.Message);
                }
                index++;
            }

            if (Options.ShowProcessingTimes)
            {
                sw.Stop();
                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
                _logger.LogInformation("[{RequestId}] {Count:N0} gym fort details parsed in {TotalSeconds}s", requestId, count, totalSeconds);
            }
        }

        if (webhooks.Any())
        {
            await SendWebhooksAsync(webhooks);
        }
    }

    private async Task UpdateGymInfoAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> gymInfos)
    {
        var sw = new Stopwatch();
        var webhooks = new Dictionary<WebhookType, List<BaseEntity>>();
        var count = gymInfos.Count();
        var index = 1;

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} gym infos", requestId, count);
        }

        // Convert gym info protos to Gym models
        foreach (var gymInfo in gymInfos)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing gym info {Index:N0}/{Count:N0}", requestId, index, count);
            }

            try
            {
                var data = (GymGetInfoOutProto)gymInfo.data;
                var fortId = data.GymStatusAndDefenders?.PokemonFortProto.FortId;
                if (string.IsNullOrEmpty(fortId))
                    continue;

                var gym = await EntityRepository.GetEntityAsync<string, Gym>(connection, fortId, _memCache);
                if (gym == null)
                    continue;

                gym.AddDetails(data);
                var gymWebhooks = await gym.UpdateAsync(null, _memCache);

                if (gym.HasChanges)
                {
                    AddEntity(SqlQueryType.GymDetailsOnMergeUpdate, gym);
                    ProtoDataStatistics.Instance.TotalFortDetailsProcessed++;
                }

                if (gymWebhooks.Any())
                {
                    GroupWebhooks(gymWebhooks, webhooks);
                }

                if (Options.ProcessGymDefenders || Options.ProcessGymTrainers)
                {
                    var gymDefenders = data.GymStatusAndDefenders?.GymDefender;
                    if (gymDefenders == null)
                        continue;

                    foreach (var gymDefenderData in gymDefenders)
                    {
                        if (Options.ProcessGymTrainers && gymDefenderData.TrainerPublicProfile != null)
                        {
                            var gymTrainer = new GymTrainer(gymDefenderData.TrainerPublicProfile);
                            AddEntity(SqlQueryType.GymTrainerOnMergeUpdate, gymTrainer);
                            ProtoDataStatistics.Instance.TotalGymTrainersProcessed++;

                            if (webhooks.TryGetValue(WebhookType.GymTrainers, out var value))
                            {
                                value.Add(gymTrainer);
                            }
                            else
                            {
                                webhooks.Add(WebhookType.GymTrainers, new() { gymTrainer });
                            }
                        }
                        if (Options.ProcessGymDefenders && gymDefenderData.MotivatedPokemon != null)
                        {
                            var gymDefender = new GymDefender(gymDefenderData, fortId);
                            AddEntity(SqlQueryType.GymDefenderOnMergeUpdate, gymDefender);
                            ProtoDataStatistics.Instance.TotalGymDefendersProcessed++;

                            if (webhooks.TryGetValue(WebhookType.GymDefenders, out var value))
                            {
                                value.Add(gymDefender);
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
                _logger.LogError("UpdateGymInfoAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} gym info parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            await SendWebhooksAsync(webhooks);
        }
    }

    private async Task UpdateQuestsAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> quests)
    {
        var sw = new Stopwatch();
        var webhooks = new Dictionary<WebhookType, List<Pokestop>>();
        var count = quests.Count();
        var index = 1;

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} quests", requestId, count);
        }

        foreach (var quest in quests)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing quest {Index:N0}/{Count:N0}", requestId, index, count);
            }

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
                var questWebhooks = await pokestop.UpdateAsync(null, _memCache, updateQuest: true);

                if (pokestop.HasChanges && (pokestop.HasQuestChanges || pokestop.HasAlternativeQuestChanges))
                {
                    AddEntity(SqlQueryType.PokestopOnMergeUpdate, pokestop);
                    ProtoDataStatistics.Instance.TotalQuestsProcessed++;
                }

                if (questWebhooks.Any())
                {
                    GroupWebhooks(questWebhooks, webhooks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateQuestsAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} quests parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            await SendWebhooksAsync(webhooks);
        }
    }

    private async Task UpdateEncountersAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> encounters)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var timestampMs = now * 1000;
        var webhooks = new List<Pokemon>();
        var count = encounters.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} pokemon encounters", requestId, count);
        }

        foreach (var encounter in encounters)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing pokemon encounter {Index:N0}/{Count:N0}", requestId, index, count);
            }

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

                    pokemon = Pokemon.ParsePokemonFromWild(data.Pokemon, cellId.Id, username, isEvent);
                    //isNew = true;
                }
                pokemon.AddEncounter(data, username, isEvent: false, setPvpRankings: new Action<Pokemon>(SetPvpRankings));

                var spawnpoint = await pokemon.ParseSpawnpointAsync(connection, _memCache, data.Pokemon.TimeTillHiddenMs, timestampMs);
                if (spawnpoint != null)
                {
                    AddEntity(SqlQueryType.SpawnpointOnMergeUpdate, spawnpoint);
                    ProtoDataStatistics.Instance.TotalSpawnpointsProcessed++;
                }

                //var oldPokemon = isNew ? null : await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache);
                //await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: true);
                AddEntity(SqlQueryType.PokemonOnMergeUpdate, pokemon);
                ProtoDataStatistics.Instance.TotalPokemonEncountersProcessed++;

                if (pokemon.SendWebhook)
                {
                    webhooks.Add(pokemon);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateEncountersAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} pokemon encounters parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            _logger.LogDebug("Sending {Count:N0} pokemon webhooks", webhooks.Count);
            await SendWebhooksAsync(WebhookType.Pokemon, webhooks);

            // Send pokemon to Configurator endpoint for IV stats
            await SendPokemonAsync(webhooks);
        }
    }

    private async Task UpdateDiskEncountersAsync(string requestId, MySqlConnection connection, IEnumerable<dynamic> diskEncounters)
    {
        var webhooks = new List<Pokemon>();
        var count = diskEncounters.Count();
        var index = 1;
        var sw = new Stopwatch();

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} pokemon disk encounters", requestId, count);
        }

        foreach (var diskEncounter in diskEncounters)
        {
            if (Options.ShowProcessingCount)
            {
                _logger.LogInformation("[{RequestId}] Parsing pokemon disk encounter {Index:N0}/{Count:N0}", requestId, index, count);
            }

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
                    _logger.LogInformation("Disk encounter with id '{DisplayId}' added to cache", displayId);
                    continue;
                }

                pokemon.AddDiskEncounter(data, username, setPvpRankings: new Action<Pokemon>(SetPvpRankings));

                //var oldPokemon = await EntityRepository.GetEntityAsync<string, Pokemon>(connection, pokemon.Id, _memCache);
                //await pokemon.UpdateAsync(oldPokemon, _memCache, updateIv: true);
                AddEntity(SqlQueryType.PokemonOnMergeUpdate, pokemon);
                ProtoDataStatistics.Instance.TotalPokemonDiskEncountersProcessed++;

                if (pokemon.SendWebhook)
                {
                    webhooks.Add(pokemon);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateDiskEncountersAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            index++;
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} pokemon disk encounters parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            _logger.LogDebug("Sending {Count:N0} pokemon webhooks", webhooks.Count);
            await SendWebhooksAsync(WebhookType.Pokemon, webhooks);

            // Send pokemon to Configurator endpoint for IV stats
            await SendPokemonAsync(webhooks);
        }
    }

    private async Task UpdateIncidentsAsync(string requestId, MySqlConnection connection, Dictionary<Pokestop, List<Incident>> pokestopIncidents)
    {
        var sw = new Stopwatch();
        var webhooks = new List<PokestopWithIncident>();
        var count = pokestopIncidents.Values.Count;
        var index = 1;

        if (Options.ShowProcessingTimes)
        {
            sw.Start();
            _logger.LogInformation("[{RequestId}] Parsing {Count:N0} pokestop incidents", requestId, count);
        }

        // Loop incidents
        foreach (var (pokestop, incidents) in pokestopIncidents)
        {
            foreach (var incident in incidents)
            {
                if (Options.ShowProcessingCount)
                {
                    _logger.LogInformation("[{RequestId}] Parsing pokestop incident {Index:N0}/{Count:N0}", requestId, index, count);
                }

                try
                {
                    //Incident? oldIncident = null;
                    var oldIncident = await EntityRepository.GetEntityAsync<string, Incident>(connection, incident.Id, _memCache);
                    await incident.UpdateAsync(oldIncident, _memCache);

                    if (incident.HasChanges)
                    {
                        AddEntity(SqlQueryType.IncidentOnMergeUpdate, incident);
                        ProtoDataStatistics.Instance.TotalIncidentsProcessed++;
                    }

                    if (incident.SendWebhook)
                    {
                        webhooks.Add(new PokestopWithIncident(pokestop, incident));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("UpdateIncidentsAsync: {Message}", ex.InnerException?.Message ?? ex.Message);
                }
                index++;
            }
        }

        if (Options.ShowProcessingTimes)
        {
            sw.Stop();
            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            _logger.LogInformation("[{RequestId}] {Count:N0} pokemon incidents parsed in {TotalSeconds}s", requestId, count, totalSeconds);
        }

        if (webhooks.Any())
        {
            //_logger.LogDebug($"Sending {webhooks.Count:N0} pokestop incident webhooks");
            await SendWebhooksAsync(WebhookType.Invasions, webhooks);
        }
    }

    #endregion

    #region Private Methods

    private void CheckQueueLength()
    {
        var usage = $"{_taskQueue.Count:N0}/{Options.Queue.MaximumCapacity:N0}";
        if (_taskQueue.Count >= Options.Queue.MaximumCapacity)
        {
            _logger.LogError("Data processing queue is at maximum capacity! {Usage}", usage);
        }
        else if (_taskQueue.Count >= Options.Queue.MaximumSizeWarning)
        {
            _logger.LogWarning("Data processing queue is over normal capacity with {Usage} items total, consider increasing 'MaximumQueueBatchSize'", usage);
        }
    }

    private static void SetPvpRankings(Pokemon pokemon)
    {
        if (!EntityConfiguration.Instance.EnablePvp)
            return;

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
        pokemon.PvpRankings = pvpRanks != null && pvpRanks.Any()
            ? (Dictionary<string, dynamic>)pvpRanks
            : null;
    }

    private static void GroupWebhooks<T1, T2>(Dictionary<WebhookType, T1> input, Dictionary<WebhookType, List<T2>> output)
        where T1 : class, T2
    {
        foreach (var (key, value) in input)
        {
            if (output.TryGetValue(key, out var result))
            {
                result.Add(value);
            }
            else
            {
                output.Add(key, new List<T2> { value });
            }
        }
    }

    private void AddEntity(SqlQueryType type, BaseEntity entity)
    {
        // Lock for thread safety
        var partial = _dataConsumerQueue.GetOrAdd(type, _ => new());
        lock (partial)
        {
            partial.Add(entity);
        }
    }

    private void AddEntities(SqlQueryType type, IEnumerable<BaseEntity> entities)
    {
        // Lock for thread safety
        var partial = _dataConsumerQueue.GetOrAdd(type, _ => new());
        lock (partial)
        {
            entities
                .ToList()
                .ForEach(partial.Add);
        }
    }

    #endregion

    #region Grpc Senders

    private async Task SendWebhooksAsync<T>(WebhookType type, IEnumerable<T> webhooks)
    {
        // Fire off gRPC request on a separate thread
        await Task.Run(() =>
        {
            new Thread(async () =>
            {
                var webhookType = type.ConvertWebhookType();
                foreach (var webhook in webhooks)
                {
                    await SendWebhookAsync(webhookType, webhook);
                }
            })
            { IsBackground = true }.Start();
        });
    }

    private async Task SendWebhooksAsync<T>(Dictionary<WebhookType, List<T>> webhooks)
    {
        // Fire off gRPC request on a separate thread
        await Task.Run(() =>
        {
            new Thread(async () =>
            {
                foreach (var (type, entities) in webhooks)
                {
                    var webhookType = type.ConvertWebhookType();
                    foreach (var entity in entities)
                    {
                        await SendWebhookAsync(webhookType, entity);
                    }
                }
            })
            { IsBackground = true }.Start();
        });
    }

    private async Task SendWebhookAsync<T>(WebhookPayloadType webhookType, T entity)
    {
        if (entity == null)
        {
            _logger.LogWarning("Unable to relay entity {Name} to webhook service, entity is null...", typeof(T).Name);
            return;
        }

        var json = entity.ToJson();
        if (string.IsNullOrEmpty(json))
        {
            _logger.LogWarning("Failed to serialize entity {Name} to relay to webhook service, skipping...", typeof(T).Name);
            return;
        }

        // Fire off gRPC request on a separate thread
        await _grpcWebhookClient.SendAsync(new WebhookPayloadRequest
        {
            PayloadType = webhookType,
            Payload = json,
        });
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
            await Task.Run(() =>
            {
                new Thread(async () =>
                {
                    // Send got Pokemon proto message
                    await _grpcProtoClient.SendAsync(new PayloadRequest
                    {
                        PayloadType = PayloadType.PokemonList,
                        Payload = newPokemon.ToJson(),
                        HasIV = false,
                    });
                })
                { IsBackground = true }.Start();
            });
        }

        if (newPokemonWithIV.Any())
        {
            // Fire off gRPC request on a separate thread
            await Task.Run(() =>
            {
                new Thread(async () =>
                {
                    // Send got Pokemon IV proto message
                    await _grpcProtoClient.SendAsync(new PayloadRequest
                    {
                        PayloadType = PayloadType.PokemonList,
                        Payload = newPokemonWithIV.ToJson(),
                        HasIV = true,
                    });
                })
                { IsBackground = true }.Start();
            });
        }

        await Task.CompletedTask;
    }

    private async Task SendPokestopsAsync(List<dynamic> forts)
    {
        // Fire off gRPC request on a separate thread
        await Task.Run(() =>
        {
            new Thread(async () =>
            {
                var data = forts
                    .Select(fort => new { fort.data, fort.username })
                    .ToList();
                await _grpcProtoClient.SendAsync(new PayloadRequest
                {
                    PayloadType = PayloadType.FortList,
                    Payload = data.ToJson(),
                });
            })
            { IsBackground = true }.Start();
        });

        await Task.CompletedTask;
    }

    private async Task SendPlayerDataAsync(string username, ushort level, uint xp)
    {
        // Fire off gRPC request on a separate thread
        await Task.Run(() =>
        {
            new Thread(async () =>
            {
                var payload = new { username, level, xp };
                await _grpcProtoClient.SendAsync(new PayloadRequest
                {
                    PayloadType = PayloadType.PlayerInfo,
                    Payload = payload.ToJson(),
                    Username = username,
                });
            })
            { IsBackground = true }.Start();
        });

        await Task.CompletedTask;
    }

    #endregion
}

public class ProtoDataManifest
{
    public IEnumerable<dynamic> PlayerData { get; internal set; } = Array.Empty<dynamic>();

    public IEnumerable<ulong> Cells { get; internal set; } = Array.Empty<ulong>();

    public IEnumerable<ClientWeatherProto> Weather { get; internal set; } = Array.Empty<ClientWeatherProto>();

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

    public int PokemonCount =>
        WildPokemon.Count() +
        NearbyPokemon.Count() +
        MapPokemon.Count() +
        Encounters.Count() +
        DiskEncounters.Count();

    public ProtoDataManifest(
        IEnumerable<dynamic> playerData,
        IEnumerable<ulong> cells,
        IEnumerable<ClientWeatherProto> weather,
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

    public static ProtoDataManifest ParseProtoManifest(IEnumerable<DataQueueItem> protoss)
    {
        var items = protoss.SelectMany(x => x.Data!);
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

        return new ProtoDataManifest
        (
            playerData, cells, clientWeather,
            forts, fortDetails, gymInfos,
            wildPokemon, nearbyPokemon, mapPokemon,
            quests, encounters, diskEncounters
        );
    }
}