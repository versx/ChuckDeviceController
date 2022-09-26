namespace ChuckDeviceController.Services
{
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
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
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.HostedServices;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Pvp;
    using ChuckDeviceController.Pvp.Models;
    using ChuckDeviceController.Services.Rpc;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;

    // TODO: Create stateless DataConsumer for each entity
    // TODO: Possibly create scoped DataProcessorService for each device uuid
    // TODO: Use/benchmark Dapper Micro ORM
    // TODO: Split up/refactor class

    public class DataProcessorService : TimedHostedService, IDataProcessorService
    {
        #region Variables

        private readonly ILogger<IDataProcessorService> _logger;
        private readonly IAsyncQueue<DataQueueItem> _taskQueue;
        private readonly IDbContextFactory<MapDbContext> _dbFactory;
        private readonly IMemoryCache _diskCache;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IClearFortsHostedService _clearFortsService;
        private readonly IMemoryCacheHostedService _memCache;
        private readonly IWebHostEnvironment _env;

        #endregion

        #region Properties

        public ProcessorOptionsConfig Options { get; }

        public bool ShowBenchmarkTimes => _env?.IsDevelopment() ?? false;

        #endregion

        #region Constructor

        public DataProcessorService(
            ILogger<IDataProcessorService> logger,
            IOptions<ProcessorOptionsConfig> options,
            IAsyncQueue<DataQueueItem> taskQueue,
            IDbContextFactory<MapDbContext> factory,
            IMemoryCache diskCache,
            IGrpcClientService grpcClientService,
            IClearFortsHostedService clearFortsService,
            IMemoryCacheHostedService memCache,
            IWebHostEnvironment env)
            : base(new Logger<TimedHostedService>(LoggerFactory.Create(x => x.AddConsole())))
        {
            _logger = logger;
            _taskQueue = taskQueue;
            _dbFactory = factory;
            _diskCache = diskCache;
            _grpcClientService = grpcClientService;
            _clearFortsService = clearFortsService;
            _memCache = memCache;
            _env = env;

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
                    return;
                }

                var workItems = await _taskQueue.DequeueBulkAsync(Strings.MaximumQueueBatchSize, stoppingToken);
                //var workItems = await _taskQueue.DequeueBulkAsync(25, stoppingToken);
                if (!workItems.Any())
                {
                    return;
                }

                // TODO: Filter data entities here and push to separate methods
                Parallel.ForEach(workItems, async payload => await ProcessWorkItemAsync(payload, stoppingToken).ConfigureAwait(false));

                ProtoDataStatistics.Instance.TotalEntitiesProcessed += (uint)workItems.Sum(x => x.Data?.Count ?? 0);

                //await Task.Run(async () =>
                //{
                //    foreach (var workItem in workItems)
                //    {
                //        //await Task.Factory.StartNew(async () => await ProcessWorkItemAsync(workItem, stoppingToken));
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

            var sw = new Stopwatch();
            if (ShowBenchmarkTimes)
            {
                sw.Start();
            }

            ProtoDataStatistics.Instance.TotalEntitiesUpserted += (uint)workItem.Data.Count;

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

            var cells = workItem.Data
                .Where(x => x.type == ProtoDataType.Cell)
                .Select(x => (ulong)x.cell)
                .Distinct()
                .ToList();
            //var cells = FilterEntityData<ulong>("cell", workItem.Data, ProtoDataType.Cell);
            if (cells.Any())
            {
                // Insert S2 cells
                await UpdateCellsAsync(cells);
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
                await UpdateClientWeatherAsync(clientWeather);
            }

            var forts = workItem.Data
                .Where(x => x.type == ProtoDataType.Fort)
                .ToList();
            if (forts.Any())
            {
                // Insert Forts
                await UpdateFortsAsync(workItem.Username, forts);
            }

            var fortDetails = workItem.Data
                .Where(x => x.type == ProtoDataType.FortDetails)
                .ToList();
            if (fortDetails.Any())
            {
                // Insert Fort Details
                await UpdateFortDetailsAsync(fortDetails);
            }

            var gymInfos = workItem.Data
                .Where(x => x.type == ProtoDataType.GymInfo)
                .ToList();
            if (gymInfos.Any())
            {
                // Insert gym info
                await UpdateGymInfoAsync(gymInfos);
            }

            var wildPokemon = workItem.Data
                .Where(x => x.type == ProtoDataType.WildPokemon)
                .ToList();
            if (wildPokemon.Any())
            {
                // Insert wild pokemon
                await UpdateWildPokemonAsync(wildPokemon);
            }

            var nearbyPokemon = workItem.Data
                .Where(x => x.type == ProtoDataType.NearbyPokemon)
                .ToList();
            if (nearbyPokemon.Any())
            {
                // Insert nearby pokemon
                await UpdateNearbyPokemonAsync(nearbyPokemon);
             }

            var mapPokemon = workItem.Data
                .Where(x => x.type == ProtoDataType.MapPokemon)
                .ToList();
            if (mapPokemon.Any())
            {
                // Insert map pokemon
                await UpdateMapPokemonAsync(mapPokemon);
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
                await UpdateQuestsAsync(quests);
            }

            var encounters = workItem.Data
                .Where(x => x.type == ProtoDataType.Encounter)
                .ToList();
            if (encounters.Any())
            {
                // Insert Pokemon encounters
                await UpdateEncountersAsync(encounters);
            }

            var diskEncounters = workItem.Data
                .Where(x => x.type == ProtoDataType.DiskEncounter)
                .ToList();
            if (diskEncounters.Any())
            {
                // Insert lured/disk Pokemon encounters
                await UpdateDiskEncountersAsync(diskEncounters);
            }

            PrintBenchmarkTimes(DataLogLevel.Summary, workItem.Data, "total entities", sw);
        }

        #endregion

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

        private async Task UpdateCellsAsync(IEnumerable<ulong> cells)
        //private async Task UpdateCellsAsync(MapDbContext context, IEnumerable<dynamic> cells)
        {
            try
            {
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                // Convert cell ids to Cell models
                var s2cells = cells
                    // Filter cells not already cached, cached cells expire every 60 minutes.
                    // Once expired they will be updated when found again.
                    .Where(cellId => !_memCache.IsSet<ulong, Cell>(cellId))
                    .Select(cellId => new Cell(cellId)) //var cached = _memCache.Get<ulong, Cell>(cellId); ?? new Cell(cellId)
                    .ToList();

                // Check if any new/need to be updated cells, otherwise skip
                if (!s2cells.Any())
                    return;

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var ts = DateTime.UtcNow.ToTotalSeconds();
                var cellsSql = s2cells.Select(cell => $"({cell.Id}, {cell.Level}, {cell.Latitude}, {cell.Longitude}, {ts})");
                var args = string.Join(",", cellsSql);
                var sql = string.Format(SqlQueries.S2Cells, args);

                var result = await context.Database.ExecuteSqlRawAsync(sql);
                // TODO: Check why result value returns double of entities amount it updated

                PrintBenchmarkTimes(DataLogLevel.S2Cells, s2cells, "S2 Cells", sw);

                //await context.Cells.BulkMergeAsync(s2cells, options => GetBulkOptions<Cell>(p => new { p.Updated }));

                foreach (var cell in s2cells)
                {
                    // Cache all S2 cell entities in memory cache
                    _memCache.Set(cell.Id, cell);

                    // Add S2 cells to ClearFortsHostedService
                    _clearFortsService.AddCell(cell.Id);
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

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

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                foreach (var wcell in weather)
                {
                    await wcell.UpdateAsync(context, _memCache);

                    // Check if 'SendWebhook' flag was triggered, if so relay webhook payload to communicator
                    if (wcell.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Weather, wcell);
                    }
                }

                var ts = DateTime.UtcNow.ToTotalSeconds();
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
                PrintBenchmarkTimes(DataLogLevel.Weather, weather, "Weather Cells", sw);

                /*
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
                */
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
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
                    var spawnpoint = await UpdateSpawnpointAsync(context, pokemon, data.TimeTillHiddenMs, timestampMs);
                    if (spawnpoint != null)
                    {
                        spawnpointsToUpsert.Add(spawnpoint);
                    }

                    await pokemon.UpdateAsync(context, _memCache, updateIv: false);
                    pokemonToUpsert.Add(pokemon);

                    if (pokemon.SendWebhook)
                    {
                        await SendWebhookPayloadAsync(WebhookPayloadType.Pokemon, pokemon);
                    }
                }

                if (spawnpointsToUpsert.Count > 0)
                {
                    await UpdateSpawnpointsAsync(context, spawnpointsToUpsert);
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

                    PrintBenchmarkTimes(DataLogLevel.WildPokemon, pokemonToUpsert, "Wild Pokemon", sw);

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

                //sw.Stop();
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                foreach (var nearby in nearbyPokemon)
                {
                    var cellId = nearby.cell;
                    var data = (NearbyPokemonProto)nearby.data;
                    var username = nearby.username;
                    var isEvent = nearby.isEvent;
                    var pokemon = new Pokemon(context, data, cellId, username, isEvent);
                    await pokemon.UpdateAsync(context, _memCache, updateIv: false);
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

                    PrintBenchmarkTimes(DataLogLevel.NearbyPokemon, pokemonToUpsert, "Nearby Pokemon", sw);

                    await SendPokemonAsync(pokemonToUpsert);
                }

                //sw.Stop();
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                foreach (var map in mapPokemon)
                {
                    var cellId = map.cell;
                    var data = (MapPokemonProto)map.data;
                    var username = map.username;
                    var isEvent = map.isEvent;
                    var pokemon = new Pokemon(context, data, cellId, username, isEvent);
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

                    PrintBenchmarkTimes(DataLogLevel.MapPokemon, pokemonToUpsert, "Map Pokemon", sw);

                    await SendPokemonAsync(pokemonToUpsert);
                }

                //sw.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateMapPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateFortsAsync(string? username, IEnumerable<dynamic> forts)
        //private async Task UpdateFortsAsync(MapDbContext context, string username, IEnumerable<dynamic> forts)
        {
            try
            {
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                // Send found/nearby forts with gRPC service for leveling instance
                var lvlForts = forts
                    .Where(fort => fort.data.FortType != FortType.Gym)
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
                            var pokestopWebhooks = await pokestop.UpdateAsync(context, _memCache, updateQuest: false);
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
                                    await incident.UpdateAsync(context, _memCache);
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
                            var gymWebhooks = await gym.UpdateAsync(context, _memCache);
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

                    if (Options.IsEnabled(DataLogLevel.Forts)) // TODO: Make separate pokestop/gym logging flags
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {pokestopsToUpsert.Count:N0} Pokestops in {seconds}s");
                    }
                }
                if (incidentsToUpsert.Count > 0)
                {
                    await context.Incidents.BulkMergeAsync(incidentsToUpsert, options => options.UseTableLock = true);

                    if (Options.IsEnabled(DataLogLevel.Incidents))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {incidentsToUpsert.Count:N0} Pokestop Incidents in {seconds}s");
                    }
                }
                if (gymsToUpsert.Count > 0)
                {
                    await context.Gyms.BulkMergeAsync(gymsToUpsert, options => options.UseTableLock = true);

                    if (Options.IsEnabled(DataLogLevel.Forts))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {gymsToUpsert.Count:N0} Gyms in {seconds}s");
                    }
                }

                sw.Stop();
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var pokestopsToUpsert = new List<Pokestop>();
                var gymsToUpsert = new List<Gym>();

                // Convert fort details protos to Pokestop/Gym models
                foreach (var fortDetail in fortDetails)
                {
                    var data = (FortDetailsOutProto)fortDetail.data;
                    switch (data.FortType)
                    {
                        case FortType.Checkpoint:
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
                                pokestopsToUpsert.Add(pokestop);
                            }
                            break;
                        case FortType.Gym:
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

                    if (Options.IsEnabled(DataLogLevel.FortDetails))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {pokestopsToUpsert.Count:N0} Pokestop Details in {seconds}s");
                    }
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

                    if (Options.IsEnabled(DataLogLevel.FortDetails))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {gymsToUpsert.Count:N0} Gym Details in {seconds}s");
                    }
                }

                sw.Stop();
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
                var sw = new Stopwatch();
                sw.Start();
                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var gymsToUpsert = new List<Gym>();
                var gymDefendersToUpsert = new List<GymDefender>();
                var gymTrainersToUpsert = new List<GymTrainer>();

                // Convert gym info protos to Gym models
                foreach (var gymInfo in gymInfos)
                {
                    var data = (GymGetInfoOutProto)gymInfo.data;
                    var fortId = data.GymStatusAndDefenders.PokemonFortProto.FortId;

                    //var gym = await context.Gyms.FindAsync(fortId);
                    //var gym = await (
                    //    from g in context.Gyms
                    //    select new Gym(new { g.Id, g.Name, g.Url })
                    //).FirstOrDefaultAsync();
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
                        gymsToUpsert.Add(gym);
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

                            // Send webhook
                            await SendWebhookPayloadAsync(WebhookPayloadType.GymTrainer, new GymWithTrainer(gym, gymTrainer));
                        }
                        if (gymDefenderData.MotivatedPokemon != null)
                        {
                            var gymDefender = new GymDefender(gymDefenderData, fortId);
                            gymDefendersToUpsert.Add(gymDefender);

                            // Send webhook
                            await SendWebhookPayloadAsync(WebhookPayloadType.GymDefender, new GymWithDefender(gym, gymDefender));
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

                    if (Options.IsEnabled(DataLogLevel.GymInfo))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {gymsToUpsert.Count:N0} Gym Information in {seconds}s");
                    }
                }

                if (gymTrainersToUpsert.Count > 0)
                {
                    await context.GymTrainers.BulkInsertAsync(gymTrainersToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                    });

                    if (Options.IsEnabled(DataLogLevel.GymTrainers))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {gymTrainersToUpsert.Count:N0} Gym Trainers in {seconds}s");
                    }
                }

                if (gymDefendersToUpsert.Count > 0)
                {
                    await context.GymDefenders.BulkInsertAsync(gymDefendersToUpsert, options =>
                    {
                        options.AllowDuplicateKeys = false;
                        options.UseTableLock = true;
                    });

                    if (Options.IsEnabled(DataLogLevel.GymDefenders))
                    {
                        var seconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                        _logger.LogInformation($"Upserted {gymDefendersToUpsert.Count:N0} Gym Defenders in {seconds}s");
                    }
                }

                sw.Stop();
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var questsToUpsert = new List<Pokestop>();

                // Convert quest protos to Pokestop models
                foreach (var quest in quests)
                {
                    var data = (QuestProto)quest.quest;
                    var title = quest.title;
                    var hasAr = quest.hasAr;
                    var fortId = data.FortId;

                    //var pokestop = await context.Pokestops.FindAsync(fortId);
                    /*
                    var pokestop = await (
                        from stop in context.Pokestops
                        where stop.Id == fortId
                        select new Pokestop(new
                        {
                            stop.Id,
                            stop.Name,
                            stop.Url,
                            stop.QuestType,
                            stop.QuestTitle,
                            stop.QuestTimestamp,
                            stop.QuestTemplate,
                            stop.QuestTarget,
                            stop.QuestRewards,
                            stop.QuestConditions,

                            stop.AlternativeQuestType,
                            stop.AlternativeQuestTitle,
                            stop.AlternativeQuestTimestamp,
                            stop.AlternativeQuestTemplate,
                            stop.AlternativeQuestTarget,
                            stop.AlternativeQuestRewards,
                            stop.AlternativeQuestConditions,
                        })
                    ).FirstOrDefaultAsync();
                    */
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
                        questsToUpsert.Add(pokestop);
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

                    PrintBenchmarkTimes(DataLogLevel.Quests, questsToUpsert, "Pokestop Quests", sw);
                }

                //sw.Stop();
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                //var leakMonitor = new ConnectionLeakWatcher(context);
                var pokemonToUpsert = new List<Pokemon>();
                var spawnpointsToUpsert = new List<Spawnpoint>();
                var s2cellsToUpsert = new List<ulong>();
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
                            s2cellsToUpsert.Add(cellId.Id);
                            pokemon = new Pokemon(data.Pokemon, cellId.Id, username, isEvent);
                        }
                        await pokemon.AddEncounterAsync(data, username);

                        var spawnpoint = await UpdateSpawnpointAsync(context, pokemon, data.Pokemon.TimeTillHiddenMs, timestampMs);
                        if (spawnpoint != null)
                        {
                            spawnpointsToUpsert.Add(spawnpoint);
                        }

                        if (pokemon.HasIvChanges)
                        {
                            SetPvpRankings(pokemon);
                        }
                        await pokemon.UpdateAsync(context, _memCache, updateIv: true);
                        pokemonToUpsert.Add(pokemon);

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

                if (spawnpointsToUpsert.Count > 0)
                {
                    await UpdateSpawnpointsAsync(context, spawnpointsToUpsert);
                }

                if (s2cellsToUpsert.Count > 0)
                {
                    await UpdateCellsAsync(s2cellsToUpsert);
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

                    PrintBenchmarkTimes(DataLogLevel.PokemonEncounters, pokemonToUpsert, "Pokemon Encounters", sw);

                    await SendPokemonAsync(pokemonToUpsert);
                }

                //sw.Stop();
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
                var sw = new Stopwatch();
                if (ShowBenchmarkTimes)
                {
                    sw.Start();
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                //using var context = _dbFactory.CreateDbContext();
                var pokemonToUpsert = new List<Pokemon>();
                foreach (var diskEncounter in diskEncounters)
                {
                    var data = (DiskEncounterOutProto)diskEncounter.data;
                    var username = diskEncounter.username;
                    var isEvent = diskEncounter.isEvent;
                    var displayId = Convert.ToString(data.Pokemon.PokemonDisplay.DisplayId);
                    var pokemon = await GetEntity<string, Pokemon>(context, displayId);
                    if (pokemon != null)
                    {
                        pokemon.AddDiskEncounter(data, username);
                        if (pokemon.HasIvChanges)
                        {
                            SetPvpRankings(pokemon);
                        }
                        await pokemon.UpdateAsync(context, _memCache, updateIv: true);
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

                    PrintBenchmarkTimes(DataLogLevel.PokemonDiskEncounters, pokemonToUpsert, "Disk Pokemon Encounters", sw);

                    await SendPokemonAsync(pokemonToUpsert);
                }

                //sw.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateSpawnpointsAsync(MapDbContext context, IReadOnlyList<Spawnpoint> spawnpoints)
        {
            if (!spawnpoints.Any())
                return;

            await context.Spawnpoints.BulkMergeAsync(spawnpoints, options =>
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

            PrintBenchmarkTimes(DataLogLevel.Spawnpoints, spawnpoints, "Spawnpoints");
        }

        private async Task<Spawnpoint?> UpdateSpawnpointAsync(MapDbContext context, Pokemon pokemon, int timeTillHiddenMs, ulong timestampMs)
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

        private static double BenchmarkAction(Action action, ushort precision = 5)
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

        private async Task<TEntity?> GetEntity<TKey, TEntity>(MapDbContext context, TKey key)
        {
            var entity = _memCache.Get<TKey, TEntity>(key);
            if (entity == null)
            {
                entity = (TEntity?)await context.FindAsync(typeof(TEntity), key);
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