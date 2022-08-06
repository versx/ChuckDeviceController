namespace ChuckDeviceController.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Nito.AsyncEx;
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Pvp;
    using ChuckDeviceController.Pvp.Models;
    using ChuckDeviceController.Services.Rpc;

    /*
    public interface IDataConsumer
    {
    }

    public class DataConsumer : IDataConsumer
    {
        private readonly ILogger<IProtoProcessorService> _logger;
        //private readonly IConfiguration _config;
        private readonly IDbContextFactory<MapDataContext> _dbFactory;
        private readonly MapDataContext _context;

        public DataConsumer(
            ILogger<IProtoProcessorService> logger,
            //IConfiguration config,
            IDbContextFactory<MapDataContext> factory)
        {
            _logger = logger;
            //_config = config;
            _dbFactory = factory;
            _context = _dbFactory.CreateDbContext();
        }

        public void ProcessCells(List<dynamic> cells)
        {
        }
    }
    */

    // TODO: Use/benchmark Dapper Micro ORM
    // TODO: Implement memory cache for all map data entities
    // TODO: Split up/refactor class

    public class DataProcessorService : BackgroundService, IDataProcessorService
    {
        #region Variables

        private readonly ILogger<IProtoProcessorService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IDbContextFactory<MapDataContext> _dbFactory;
        private readonly IMemoryCache _diskCache;
        private readonly IGrpcClientService _grpcClientService;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell = new();
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell = new();

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

        #endregion

        #region Properties

        public bool ClearOldForts { get; set; } = true; // TODO: Make 'ClearOldForts' configurable (load from ChuckDeviceController config)

        #endregion

        #region Constructor

        public DataProcessorService(
            ILogger<IProtoProcessorService> logger,
            IBackgroundTaskQueue taskQueue,
            IDbContextFactory<MapDataContext> factory,
            IMemoryCache diskCache,
            IGrpcClientService grpcClientService)
        {
            _logger = logger;
            _taskQueue = (DefaultBackgroundTaskQueue)taskQueue;
            _dbFactory = factory;
            _diskCache = diskCache;
            _grpcClientService = grpcClientService;
        }

        #endregion

        #region Background Service

        public async Task ConsumeDataAsync(string username, List<dynamic> data)
        {
            await _taskQueue.EnqueueAsync(async token =>
                await ProcessWorkItemAsync(username, data, token));
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

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    //await workItem(stoppingToken);
                    var workItems = await _taskQueue.DequeueMultipleAsync(Strings.MaximumQueueBatchSize, stoppingToken);
                    //var tasks = workItems.Select(item => Task.Factory.StartNew(async () => await item(stoppingToken)));
                    //Task.WaitAll(tasks.ToArray(), stoppingToken);
                    await Task.Run(() =>
                    {
                        foreach (var workItem in workItems)
                        {
                            Task.Factory.StartNew(async () => await workItem(stoppingToken));
                        }
                    }, stoppingToken);

                    /*
                    foreach (var workItem in workItems)
                    {
                        await workItem(stoppingToken);
                    }
                    */
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task work item.");
                }
                //await Task.Delay(10, stoppingToken);
                Thread.Sleep(5);
            }

            _logger.LogError("Exited background processing...");
        }

        private async Task<CancellationToken> ProcessWorkItemAsync(
            string username,
            List<dynamic> data,
            CancellationToken stoppingToken)
        {
            if (data.Count == 0)
            {
                return stoppingToken;
            }

            CheckQueueLength();

            var count = data.Count;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var playerData = data.Where(x => x.type == ProtoDataType.PlayerData)
                                 .Select(x => x.data)
                                 .ToList();
            if (playerData.Count > 0)
            {
                // Insert account player data
                await UpdatePlayerDataAsync(playerData);
            }

            var cells = data.Where(x => x.type == ProtoDataType.Cell)
                            .Select(x => x.cell)
                            .Distinct()
                            .ToList();
            if (cells.Count > 0)
            {
                using (await _cellsLock.LockAsync(stoppingToken))
                {
                    // Insert S2 cells
                    await UpdateCellsAsync(cells);
                }
            }

            var clientWeather = data.Where(x => x.type == ProtoDataType.ClientWeather)
                                    .Select(x => x.data)
                                    .Distinct()
                                    .ToList();
            if (clientWeather.Count > 0)
            {
                using (await _weatherLock.LockAsync(stoppingToken))
                {
                    // Insert weather cells
                    await UpdateClientWeatherAsync(clientWeather);
                }
            }

            var wildPokemon = data.Where(x => x.type == ProtoDataType.WildPokemon)
                                  .ToList();
            if (wildPokemon.Count > 0)
            {
                using (await _wildPokemonLock.LockAsync(stoppingToken))
                {
                    // Insert wild pokemon
                    await UpdateWildPokemonAsync(wildPokemon);
                }
            }

            var nearbyPokemon = data.Where(x => x.type == ProtoDataType.NearbyPokemon)
                                    .ToList();
            if (nearbyPokemon.Count > 0)
            {
                using (await _nearbyPokemonLock.LockAsync(stoppingToken))
                {
                    // Insert nearby pokemon
                    await UpdateNearbyPokemonAsync(nearbyPokemon);
                }
            }

            var mapPokemon = data.Where(x => x.type == ProtoDataType.MapPokemon)
                                 .ToList();
            if (mapPokemon.Count > 0)
            {
                using (await _mapPokemonLock.LockAsync(stoppingToken))
                {
                    // Insert map pokemon
                    await UpdateMapPokemonAsync(mapPokemon);
                }
            }

            //if (wildPokemon.Count > 0 || nearbyPokemon.Count > 0 || mapPokemon.Count > 0)
            //{
            //    await UpdatePokemonAsync(wildPokemon, nearbyPokemon, mapPokemon);
            //}

            var forts = data.Where(x => x.type == ProtoDataType.Fort)
                            .ToList();
            if (forts.Count > 0)
            {
                using (await _fortsLock.LockAsync(stoppingToken))
                {
                    // Insert Forts
                    await UpdateFortsAsync(username, forts);
                }
            }

            var fortDetails = data.Where(x => x.type == ProtoDataType.FortDetails)
                                  .ToList();
            if (fortDetails.Count > 0)
            {
                using (await _fortDetailsLock.LockAsync(stoppingToken))
                {
                    // Insert Fort Details
                    await UpdateFortDetailsAsync(fortDetails);
                }
            }

            var gymInfos = data.Where(x => x.type == ProtoDataType.GymInfo)
                               .ToList();
            if (gymInfos.Count > 0)
            {
                using (await _fortDetailsLock.LockAsync(stoppingToken))
                {
                    // Insert gym info
                    await UpdateGymInfoAsync(gymInfos);
                }
            }

            var quests = data.Where(x => x.type == ProtoDataType.Quest)
                             .ToList();
            if (quests.Count > 0)
            {
                using (await _questsLock.LockAsync(stoppingToken))
                {
                    // Insert quests
                    await UpdateQuestsAsync(quests);
                }
            }

            var encounters = data.Where(x => x.type == ProtoDataType.Encounter)
                                 .ToList();
            if (encounters.Count > 0)
            {
                using (await _encountersLock.LockAsync(stoppingToken))
                {
                    // Insert Pokemon encounters
                    await UpdateEncountersAsync(encounters);
                }
            }

            var diskEncounters = data.Where(x => x.type == ProtoDataType.DiskEncounter)
                                     .ToList();
            if (diskEncounters.Count > 0)
            {
                using (await _diskEncountersLock.LockAsync(stoppingToken))
                {
                    // Insert lured Pokemon encounters
                    await UpdateDiskEncountersAsync(diskEncounters);
                }
            }

            stopwatch.Stop();
            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
            _logger.LogInformation($"Data processer inserted {count:N0} items in {totalSeconds}s");

            if (ClearOldForts)
            {
                // Clear any old Gyms or Pokestops that might have been removed from the game
                await ClearOldFortsAsync();
            }

            return stoppingToken;
        }

        #endregion

        #region Data Handling Methods

        private async Task UpdatePlayerDataAsync(List<dynamic> playerData)
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

        private async Task UpdateCellsAsync(List<dynamic> cells)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                //var raw = context.Database.ExecuteSqlRawAsync("", cells);
                try
                {
                    // Convert cell ids to Cell models
                    var s2cells = cells.Select(cell => new Cell(cell))
                                       .ToList();
                    await context.Cells.BulkMergeAsync(s2cells, options => options.UseTableLock = true); //options.AllowDuplicateKeys = false;

                    foreach (var cell in s2cells)
                    {
                        lock (_gymIdsPerCell)
                        {
                            if (!_gymIdsPerCell.ContainsKey(cell.Id))
                            {
                                _gymIdsPerCell.Add(cell.Id, new());
                            }
                        }
                        lock (_stopIdsPerCell)
                        {
                            if (!_stopIdsPerCell.ContainsKey(cell.Id))
                            {
                                _stopIdsPerCell.Add(cell.Id, new());
                            }
                        }
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} S2 cells");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateCellsAsync: {ex}");
                }
            }
        }

        private async Task UpdateClientWeatherAsync(List<dynamic> clientWeather)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
                    // Convert weather protos to Weather models
                    var weather = clientWeather.Select(weather => new Weather(weather))
                                               .ToList();
                    foreach (var weatherCell in weather)
                    {
                        await weatherCell.UpdateAsync(context);

                        if (weatherCell.SendWebhook)
                        {
                            await SendWebhookPayloadAsync(WebhookPayloadType.Weather, weatherCell);
                        }
                    }

                    await context.Weather.BulkMergeAsync(weather, options => options.UseTableLock = true);
                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Client weather cells");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateClientWeatherAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateWildPokemonAsync(List<dynamic> wildPokemon)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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

                    if (pokemonToUpsert.Count > 0)
                    {
                        await context.Pokemon.BulkMergeAsync(pokemonToUpsert, options =>
                        {
                            // Do not update IV specific columns
                            options.UseTableLock = true;
                            //options.IgnoreOnMergeUpdate = true;
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

                        await SendPokemonAsync(pokemonToUpsert);
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
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Wild Pokemon");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateWildPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateNearbyPokemonAsync(List<dynamic> nearbyPokemon)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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
                            options.UseTableLock = true;
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

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Nearby Pokemon");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateNearbyPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateMapPokemonAsync(List<dynamic> mapPokemon)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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
                            options.UseTableLock = true;
                        });

                        await SendPokemonAsync(pokemonToUpsert);
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Map Pokemon");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateMapPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateFortsAsync(string username, List<dynamic> forts)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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

                                lock (_stopIdsPerCell)
                                {
                                    if (!_stopIdsPerCell.ContainsKey(cellId))
                                    {
                                        _stopIdsPerCell.Add(cellId, new());
                                    }
                                    _stopIdsPerCell[cellId].Add(data.FortId);
                                }
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

                                lock (_gymIdsPerCell)
                                {
                                    if (!_gymIdsPerCell.ContainsKey(cellId))
                                    {
                                        _gymIdsPerCell.Add(cellId, new());
                                    }
                                    _gymIdsPerCell[cellId].Add(data.FortId);
                                }
                                break;
                        }
                    }

                    if (pokestopsToUpsert.Count > 0)
                    {
                        await context.Pokestops.BulkMergeAsync(pokestopsToUpsert, options =>
                        {
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
                    }
                    if (gymsToUpsert.Count > 0)
                    {
                        await context.Gyms.BulkMergeAsync(gymsToUpsert, options => options.UseTableLock = true);
                    }

                    //await context.BulkSaveChangesAsync();
                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Forts");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateFortsAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateFortDetailsAsync(List<dynamic> fortDetails)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
                    var pokestopsToUpsert = new List<Pokestop>();
                    var gymsToUpsert = new List<Gym>();

                    // Convert fort details protos to Pokestop/Gym models
                    foreach (var fortDetail in fortDetails)
                    {
                        var data = (FortDetailsOutProto)fortDetail.data;
                        switch (data.FortType)
                        {
                            case FortType.Checkpoint:
                                var pokestop = await context.Pokestops.FindAsync(data.Id);
                                if (pokestop != null)
                                {
                                    pokestop.AddDetails(data);
                                    var webhooks = await pokestop.UpdateAsync(context);
                                    foreach (var webhook in webhooks)
                                    {
                                        var type = ConvertWebhookType(webhook.Key);
                                        await SendWebhookPayloadAsync(type, pokestop);
                                    }
                                    if (pokestop.HasChanges)
                                    {
                                        //context.Update(pokestop);
                                        pokestopsToUpsert.Add(pokestop);
                                    }
                                }
                                break;
                            case FortType.Gym:
                                var gym = await context.Gyms.FindAsync(data.Id);
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
                                        //context.Update(gym);
                                        gymsToUpsert.Add(gym);
                                    }
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
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Fort details");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateFortDetailsAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateGymInfoAsync(List<dynamic> gymInfos)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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
                            // TODO: GymDefender and GymTrainer webhooks
                            if (gymDefenderData.TrainerPublicProfile != null)
                            {
                                var gymTrainer = new GymTrainer(gymDefenderData.TrainerPublicProfile);
                                gymTrainersToUpsert.Add(gymTrainer);
                            }
                            if (gymDefenderData.MotivatedPokemon != null)
                            {
                                var gymDefender = new GymDefender(gymDefenderData, fortId);
                                gymDefendersToUpsert.Add(gymDefender);
                            }
                        }
                    }

                    if (gymsToUpsert.Count > 0)
                    {
                        await context.Gyms.BulkInsertAsync(gymsToUpsert, options =>
                        {
                            options.UseTableLock = true;
                            options.OnMergeUpdateInputExpression = p => new
                            {
                                // Only update necessary columns
                                p.Id,
                                p.Name,
                                p.Url,
                            };
                        });
                    }

                    if (gymTrainersToUpsert.Count > 0)
                    {
                        await context.GymTrainers.BulkInsertAsync(gymTrainersToUpsert, options =>
                        {
                            options.UseTableLock = true;
                        });
                    }

                    if (gymDefendersToUpsert.Count > 0)
                    {
                        await context.GymDefenders.BulkInsertAsync(gymDefendersToUpsert, options =>
                        {
                            options.UseTableLock = true;
                        });
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Gym info");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateQuestsAsync(List<dynamic> quests)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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
                    }
                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Pokestop quests");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateEncountersAsync(List<dynamic> encounters)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var timestampMs = now * 1000;

            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
                    var pokemonToUpsert = new List<Pokemon>();
                    var spawnpointsToUpsert = new List<Spawnpoint>();
                    foreach (var encounter in encounters)
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

                        await SendPokemonAsync(pokemonToUpsert);
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
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Pokemon encounters");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateDiskEncountersAsync(List<dynamic> diskEncounters)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
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

                        await SendPokemonAsync(pokemonToUpsert);
                    }

                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Lured Pokemon encounters");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private static async Task<Spawnpoint> UpdateSpawnpointAsync(MapDataContext context, Pokemon pokemon, WildPokemonProto wild, ulong timestampMs)
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
            if (_taskQueue.Count == Strings.MaximumQueueCapacity)
            {
                _logger.LogWarning($"Data processing queue is at maximum capacity! {_taskQueue.Count:N0}/{Strings.MaximumQueueCapacity:N0}");
            }
            else if (_taskQueue.Count > Strings.MaximumQueueSizeWarning)
            {
                _logger.LogWarning($"Data processing queue is {_taskQueue.Count:N0} items long.");
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

        private async Task ClearOldFortsAsync()
        {
            // TODO: Use BackgroundTask/HostedService

            var gymsToDelete = new List<Gym>();
            var stopsToDelete = new List<Pokestop>();

            using (var context = _dbFactory.CreateDbContext())
            {
                foreach (var (cellId, pokestopIds) in _stopIdsPerCell)
                {
                    var pokestops = context.Pokestops.Where(stop => stop.CellId == cellId && !stop.IsDeleted)
                                                     .ToList();
                    if (pokestopIds.Count > 0)
                    {
                        pokestops = pokestops.Where(stop => !pokestopIds.Contains(stop.Id))
                                             .ToList();
                    }
                    if (pokestops.Count > 0)
                    {
                        pokestops.ForEach(stop => stop.IsDeleted = true);
                        stopsToDelete.AddRange(pokestops);
                    }
                }

                foreach (var (cellId, gymIds) in _gymIdsPerCell)
                {
                    var gyms = context.Gyms.Where(gym => gym.CellId == cellId && !gym.IsDeleted)
                                           .ToList();
                    if (gymIds.Count > 0)
                    {
                        gyms = gyms.Where(gym => !gymIds.Contains(gym.Id))
                                   .ToList();
                    }
                    if (gyms.Count > 0)
                    {
                        gyms.ForEach(gym => gym.IsDeleted = true);
                        gymsToDelete.AddRange(gyms);
                    }
                }

                if (stopsToDelete.Count > 0)
                {
                    _logger.LogInformation($"Marking {stopsToDelete.Count:N0} Pokestops as deleted since they seem to no longer exist.");
                    await context.Pokestops.BulkMergeAsync(stopsToDelete);
                }
                if (gymsToDelete.Count > 0)
                {
                    _logger.LogInformation($"Marking {gymsToDelete.Count:N0} Gyms as deleted since they seem to no longer exist.");
                    await context.Gyms.BulkMergeAsync(gymsToDelete);
                }
            }
        }

        private static WebhookPayloadType ConvertWebhookType(WebhookType type)
        {
            switch (type)
            {
                case WebhookType.Pokemon:
                    return WebhookPayloadType.Pokemon;
                case WebhookType.Pokestops:
                    return WebhookPayloadType.Pokestop;
                case WebhookType.Lures:
                    return WebhookPayloadType.Lure;
                case WebhookType.Invasions:
                    return WebhookPayloadType.Invasion;
                case WebhookType.Quests:
                    return WebhookPayloadType.Quest;
                case WebhookType.AlternativeQuests:
                    return WebhookPayloadType.AlternativeQuest;
                case WebhookType.Gyms:
                    return WebhookPayloadType.Gym;
                case WebhookType.GymInfo:
                    return WebhookPayloadType.GymInfo;
                case WebhookType.Eggs:
                    return WebhookPayloadType.Egg;
                case WebhookType.Raids:
                    return WebhookPayloadType.Raid;
                case WebhookType.Weather:
                    return WebhookPayloadType.Weather;
                case WebhookType.Accounts:
                    return WebhookPayloadType.Account;
                default:
                    return WebhookPayloadType.Pokemon;
            }
        }

        /*
        private async Task AddOrUpdateAsync<T>(List<T> entities) where T : BaseEntity
        {
            try
            {
                var isolationOptions = new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadUncommitted
                };
                using (var context = _dbFactory.CreateDbContext())
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, isolationOptions))
                    {
                        await context.BulkMergeAsync(entities, x =>
                        {
                            x.AutoMap = AutoMapType.ByIndexerName;
                            x.BatchSize = 100;
                            //x.BatchTimeout = 10 * 1000;
                            x.InsertIfNotExists = true;
                            x.InsertKeepIdentity = true;
                            x.MergeKeepIdentity = true;
                            x.Resolution = ResolutionType.Smart;
                            x.UseTableLock = true;
                            x.AllowDuplicateKeys = true;
                            //x.ColumnPrimaryKeyExpression = entity => entity.Id || entity.Uuid;
                        }).ConfigureAwait(false);
                        //scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AddOrUpdateAsync: {ex}");
            }
        }
        */

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
}