namespace ChuckDeviceController.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Transactions;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using POGOProtos.Rpc;
    using Z.BulkOperations;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Extensions;
    using ChuckDeviceController.HostedServices;
    using ChuckDeviceController.Protos;

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

    public class DataProcessorService : BackgroundService, IDataProcessorService
    {
        #region Variables

        private readonly ILogger<IProtoProcessorService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IDbContextFactory<MapDataContext> _dbFactory;
        private readonly IMemoryCache _diskCache;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell;
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell;

        #endregion

        #region Constructor

        public DataProcessorService(
            ILogger<IProtoProcessorService> logger,
            IBackgroundTaskQueue taskQueue,
            IDbContextFactory<MapDataContext> factory,
            IMemoryCache diskCache)
        {
            _logger = logger;
            _taskQueue = (DefaultBackgroundTaskQueue)taskQueue;
            _dbFactory = factory;
            _diskCache = diskCache;

            _gymIdsPerCell = new Dictionary<ulong, List<string>>();
            _stopIdsPerCell = new Dictionary<ulong, List<string>>();
        }

        #endregion

        #region Background Service

        public async Task ConsumeDataAsync(List<dynamic> data)
        {
            await _taskQueue.EnqueueAsync(async token =>
                await ProcessWorkItemAsync(data, token));
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
                // Insert S2 cells
                await UpdateCellsAsync(cells);
            }

            var clientWeather = data.Where(x => x.type == ProtoDataType.ClientWeather)
                                    .Select(x => x.data)
                                    .Distinct()
                                    .ToList();
            if (clientWeather.Count > 0)
            {
                // Insert weather cells
                await UpdateClientWeatherAsync(clientWeather);
            }

            var wildPokemon = data.Where(x => x.type == ProtoDataType.WildPokemon)
                                  .ToList();
            if (wildPokemon.Count > 0)
            {
                // Insert wild pokemon
                await UpdateWildPokemonAsync(wildPokemon);
            }

            var nearbyPokemon = data.Where(x => x.type == ProtoDataType.NearbyPokemon)
                                    .ToList();
            if (nearbyPokemon.Count > 0)
            {
                // Insert nearby pokemon
                await UpdateNearbyPokemonAsync(nearbyPokemon);
            }

            var mapPokemon = data.Where(x => x.type == ProtoDataType.MapPokemon)
                                 .ToList();
            if (mapPokemon.Count > 0)
            {
                // Insert map pokemon
                await UpdateMapPokemonAsync(mapPokemon);
            }

            //if (wildPokemon.Count > 0 || nearbyPokemon.Count > 0 || mapPokemon.Count > 0)
            //{
            //    await UpdatePokemonAsync(wildPokemon, nearbyPokemon, mapPokemon);
            //}

            var forts = data.Where(x => x.type == ProtoDataType.Fort)
                            .ToList();
            if (forts.Count > 0)
            {
                // Insert Forts
                await UpdateFortsAsync(forts);

            }

            var fortDetails = data.Where(x => x.type == ProtoDataType.FortDetails)
                                  .ToList();
            if (fortDetails.Count > 0)
            {
                // Insert Fort Details
                await UpdateFortDetailsAsync(fortDetails);
            }

            var gymInfos = data.Where(x => x.type == ProtoDataType.GymInfo)
                               .ToList();
            if (gymInfos.Count > 0)
            {
                // Insert gym info
                await UpdateGymInfoAsync(gymInfos);
            }

            var quests = data.Where(x => x.type == ProtoDataType.Quest)
                             .ToList();
            if (quests.Count > 0)
            {
                // Insert quests
                await UpdateQuestsAsync(quests);
            }

            var encounters = data.Where(x => x.type == ProtoDataType.Encounter)
                                 .ToList();
            if (encounters.Count > 0)
            {
                // Insert Pokemon encounters
                await UpdateEncountersAsync(encounters);
            }

            var diskEncounters = data.Where(x => x.type == ProtoDataType.DiskEncounter)
                                     .ToList();
            if (diskEncounters.Count > 0)
            {
                // Insert lured Pokemon encounters
                await UpdateDiskEncountersAsync(diskEncounters);
            }

            stopwatch.Stop();
            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
            _logger.LogInformation($"Data processer inserted {count:N0} items in {totalSeconds}s");

            // TODO: PlayerData

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
                    /*
                    string username = data.username;
                    ushort level = data.level;
                    uint xp = data.xp;
                    await HandlePlayerDataAsync(username, level, xp);
                    */
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdatePlayerDataAsync: {ex}");
            }
        }

        private async Task UpdateCellsAsync(List<dynamic> cells)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                //context.Set<Cell>().AddOrUpdate(cells);
                //var raw = context.Database.ExecuteSqlRawAsync("", cells);
                try
                {
                    // Convert cell ids to Cell models
                    var cellModels = cells.Select(cell => new Cell(cell)).ToList();

                    foreach (var cellModel in cellModels)
                    {
                        if (context.Cells.AsNoTracking().Any(cell => cell.Id == cellModel.Id))
                        {
                            context.Update(cellModel);
                        }
                        else
                        {
                            await context.AddAsync(cellModel);
                        }

                        lock (_gymIdsPerCell)
                        {
                            if (!_gymIdsPerCell.ContainsKey(cellModel.Id))
                            {
                                _gymIdsPerCell.Add(cellModel.Id, new List<string>());
                            }
                        }
                        lock (_stopIdsPerCell)
                        {
                            if (!_stopIdsPerCell.ContainsKey(cellModel.Id))
                            {
                                _stopIdsPerCell.Add(cellModel.Id, new List<string>());
                            }
                        }
                    }

                    //await context.BulkMergeAsync(cellModels);
                    await context.BulkSaveChangesAsync();
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
                    var weatherModels = clientWeather.Select(weather => new Weather(weather)).ToList();

                    /*
                    foreach (var weatherModel in weatherModels)
                    {
                        if (context.Weather.AsNoTracking().Any(weather => weather.Id == weatherModel.Id))
                        {
                            context.Update(weatherModel);
                        }
                        else
                        {
                            await context.AddAsync(weatherModel);
                        }
                    }
                    */

                    await context.BulkMergeAsync(weatherModels);
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
                    foreach (var wild in wildPokemon)
                    {
                        var cellId = wild.cell;
                        var data = (WildPokemonProto)wild.data;
                        var timestampMs = wild.timestampMs;
                        var username = wild.username;
                        var isEvent = wild.isEvent;
                        var pokemon = new Pokemon(context, data, cellId, timestampMs, username, isEvent);
                        await pokemon.UpdateAsync(context, updateIv: false);
                        pokemonToUpsert.Add(pokemon);
                        /*
                        if (context.Pokemon.AsNoTracking().Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                        */
                    }

                    await context.BulkMergeAsync(pokemonToUpsert);

                    await HandlePokemonAsync(pokemonToUpsert);

                    //await context.BulkSaveChangesAsync();
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

                        /*
                        if (context.Pokemon.AsNoTracking().Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                        */
                    }

                    await context.BulkMergeAsync(pokemonToUpsert);

                    await HandlePokemonAsync(pokemonToUpsert);

                    //await context.BulkSaveChangesAsync();
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

                        pokemonToUpsert.Add(pokemon);

                        // Check if we have a pending disk encounter cache
                        var displayId = data.PokemonDisplay.DisplayId;
                        var cachedDiskEncounter = _diskCache.Get<DiskEncounterOutProto>(displayId);
                        if (cachedDiskEncounter == null)
                        {
                            // Failed to get DiskEncounter from cache
                            _logger.LogWarning($"Failed to fetch cached disk encounter with id '{displayId}' from cache");
                            continue;
                        }

                        // Thanks Fabio <3
                        pokemon.AddDiskEncounter(cachedDiskEncounter, username);
                        await pokemon.UpdateAsync(context, updateIv: true);
                        _logger.LogDebug($"Found Pokemon disk encounter in cache with id '{displayId}'");
                        // TODO: Remove above upsert list addition: pokemonToUpsert.Add(pokemon);

                        /*
                        if (context.Pokemon.AsNoTracking().Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                        */
                    }

                    await context.BulkMergeAsync(pokemonToUpsert);

                    await HandlePokemonAsync(pokemonToUpsert);

                    //await context.BulkSaveChangesAsync();
                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Map Pokemon");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateMapPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        /*
        private async Task UpdatePokemonAsync(List<dynamic> wildPokemon, List<dynamic> nearbyPokemon, List<dynamic> mapPokemon)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
                    foreach (var wild in wildPokemon)
                    {
                        var cellId = wild.cell;
                        var data = (WildPokemonProto)wild.data;
                        var timestampMs = wild.timestampMs;
                        var username = wild.username;
                        var isEvent = wild.isEvent;
                        var pokemon = new Pokemon(context, data, cellId, timestampMs, username, isEvent);
                        await pokemon.UpdateAsync(context);

                        if (context.Pokemon.AsNoTracking().Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                    }

                    foreach (var nearby in nearbyPokemon)
                    {
                        var cellId = nearby.cell;
                        var data = (NearbyPokemonProto)nearby.data;
                        var username = nearby.username;
                        var isEvent = nearby.isEvent;
                        var pokemon = new Pokemon(context, data, cellId, username, isEvent);
                        await pokemon.UpdateAsync(context);

                        if (context.Pokemon.AsNoTracking().Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                    }

                    foreach (var map in mapPokemon)
                    {
                        var cellId = map.cell;
                        var data = (MapPokemonProto)map.data;
                        var username = map.username;
                        var isEvent = map.isEvent;
                        var pokemon = new Pokemon(context, data, cellId, username, isEvent);
                        await pokemon.UpdateAsync(context);

                        if (context.Pokemon.AsNoTracking().Any(pkmn => pkmn.Id == pokemon.Id))
                        {
                            context.Update(pokemon);
                        }
                        else
                        {
                            await context.AddAsync(pokemon);
                        }
                    }

                    await context.BulkSaveChangesAsync();
                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Wild Pokemon");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateWildPokemonAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }
        */

        private async Task UpdateFortsAsync(List<dynamic> forts)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
                    // Convert fort protos to Pokestop/Gym models
                    foreach (var fort in forts)
                    {
                        var cellId = (ulong)fort.cell;
                        var data = (PokemonFortProto)fort.data;
                        var username = (string)fort.username;

                        switch (data.FortType)
                        {
                            case FortType.Checkpoint:
                                // Init Pokestop model from fort proto data
                                var pokestop = new Pokestop(data, cellId);
                                await pokestop.UpdateAsync(context);

                                // Loop incidents
                                if ((pokestop.Incidents?.Count ?? 0) > 0)
                                {
                                    foreach (var incident in pokestop.Incidents)
                                    {
                                        if (context.Incidents.AsNoTracking().Any(inc => inc.Id == incident.Id))
                                        {
                                            context.Update(incident);
                                        }
                                        else
                                        {
                                            await context.AddAsync(incident);
                                        }
                                    }
                                }

                                if (context.Pokestops.AsNoTracking().Any(stop => stop.Id == pokestop.Id))
                                {
                                    // If any Pokestop properties have changed, set which to update,
                                    // otherwise EF will overwrite properties
                                    context.UpdatePokestopProperties(pokestop, false);
                                    //context.Update(pokestop);
                                }
                                else
                                {
                                    await context.AddAsync(pokestop);
                                }

                                lock (_stopIdsPerCell)
                                {
                                    if (!_stopIdsPerCell.ContainsKey(cellId))
                                    {
                                        _stopIdsPerCell.Add(cellId, new List<string>());
                                    }
                                    _stopIdsPerCell[cellId].Add(data.FortId);
                                }
                                break;
                            case FortType.Gym:
                                // Send found/nearby forts with gRPC service for leveling instance
                                await HandleGymAsync(data, username);

                                // Init Gym model from fort proto data
                                var gym = new Gym(data, cellId);
                                await gym.UpdateAsync(context);

                                if (context.Gyms.AsNoTracking().Any(g => g.Id == gym.Id))
                                {
                                    context.Update(gym);
                                }
                                else
                                {
                                    await context.AddAsync(gym);
                                }

                                lock (_gymIdsPerCell)
                                {
                                    if (!_gymIdsPerCell.ContainsKey(cellId))
                                    {
                                        _gymIdsPerCell.Add(cellId, new List<string>());
                                    }
                                    _gymIdsPerCell[cellId].Add(data.FortId);
                                }
                                break;
                        }
                    }

                    await context.BulkSaveChangesAsync();
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
                                    await pokestop.UpdateAsync(context);
                                    if (pokestop.HasChanges)
                                    {
                                        context.Update(pokestop);
                                    }
                                }
                                break;
                            case FortType.Gym:
                                var gym = await context.Gyms.FindAsync(data.Id);
                                if (gym != null)
                                {
                                    gym.AddDetails(data);
                                    await gym.UpdateAsync(context);
                                    if (gym.HasChanges)
                                    {
                                        context.Update(gym);
                                    }
                                }
                                break;
                        }
                    }

                    await context.BulkSaveChangesAsync();
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
                    // Convert gym info protos to Gym models
                    foreach (var gymInfo in gymInfos)
                    {
                        var data = (GymGetInfoOutProto)gymInfo.data;
                        var fortId = data.GymStatusAndDefenders.PokemonFortProto.FortId;

                        var gym = await context.Gyms.FindAsync(fortId);
                        if (gym != null)
                        {
                            gym.AddDetails(data);
                            await gym.UpdateAsync(context);
                            if (gym.HasChanges)
                            {
                                context.Update(gym);
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
                                if (context.GymTrainers.AsNoTracking().Any(trainer => trainer.Name == gymTrainer.Name))
                                {
                                    context.Update(gymTrainer);
                                }
                                else
                                {
                                    await context.AddAsync(gymTrainer);
                                }
                            }
                            if (gymDefenderData.MotivatedPokemon != null)
                            {
                                var gymDefender = new GymDefender(gymDefenderData, fortId);
                                if (context.GymDefenders.AsNoTracking().Any(defender => defender.Id == gymDefender.Id))
                                {
                                    context.Update(gymDefender);
                                }
                                else
                                {
                                    await context.AddAsync(gymDefender);
                                }
                            }
                        }
                    }

                    await context.BulkSaveChangesAsync();
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
                            await pokestop.UpdateAsync(context, true);

                            if (pokestop.HasChanges && (pokestop.HasQuestChanges || pokestop.HasAlternativeQuestChanges))
                            {
                                context.UpdatePokestopProperties(pokestop, true);
                                //context.Update(pokestop);
                            }
                        }
                    }

                    // TODO: BulkSaveChangesAsync
                    var inserted = await context.SaveChangesAsync();
                    _logger.LogInformation($"Inserted {inserted:N0} Pokestop quests");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateGymInfoAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private async Task UpdateEncountersAsync(List<dynamic> encounters)
        {
            using (var context = _dbFactory.CreateDbContext())
            {
                try
                {
                    var pokemonToUpsert = new List<Pokemon>();
                    foreach (var encounter in encounters)
                    {
                        var data = (EncounterOutProto)encounter.data;
                        var username = encounter.username;
                        var isEvent = encounter.isEvent;
                        //if (data.Status != EncounterOutProto.Types.Status.EncounterSuccess)
                        //{
                        //}
                        var encounterId = data.Pokemon.EncounterId.ToString();
                        var pokemon = await context.Pokemon.FindAsync(encounterId);
                        if (pokemon != null)
                        {
                            await pokemon.AddEncounterAsync(context, data, username);
                            await pokemon.UpdateAsync(context, updateIv: true);

                            //context.Update(pokemon);
                            pokemonToUpsert.Add(pokemon);
                        }
                        else
                        {
                            // New Pokemon
                            var cellId = S2CellExtensions.S2CellIdFromLatLng(data.Pokemon.Latitude, data.Pokemon.Longitude);
                            var newPokemon = new Pokemon(context, data.Pokemon, cellId.Id, DateTime.UtcNow.ToTotalSeconds(), username, isEvent);
                            await newPokemon.AddEncounterAsync(context, data, username);
                            await newPokemon.UpdateAsync(context, updateIv: true);

                            //await context.AddAsync(newPokemon);
                            pokemonToUpsert.Add(newPokemon);
                        }
                    }

                    await context.BulkMergeAsync(pokemonToUpsert);
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
                        //if (data.Status != EncounterOutProto.Types.Status.EncounterSuccess)
                        //{
                        //    continue;
                        //}
                        var displayId = data.Pokemon.PokemonDisplay.DisplayId;
                        var pokemon = await context.Pokemon.FindAsync(displayId);
                        if (pokemon != null)
                        {
                            pokemon.AddDiskEncounter(data, username);
                            await pokemon.UpdateAsync(context, updateIv: true);

                            //context.Update(pokemon);
                            pokemonToUpsert.Add(pokemon);
                        }
                        else
                        {
                            _diskCache.Set(displayId, data, TimeSpan.FromMinutes(30));
                            _logger.LogInformation($"Disk encounter with id '{displayId}' added to cache");
                        }
                    }

                    await context.BulkMergeAsync(pokemonToUpsert);
                    //var inserted = await context.SaveChangesAsync();
                    //_logger.LogInformation($"Inserted {inserted:N0} Lured Pokemon encounters");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"UpdateDiskEncountersAsync: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
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

        private static async Task HandlePokemonAsync(List<Pokemon> pokemon)
        {
            foreach (var pkmn in pokemon)
            {
                if (pkmn.IsNewPokemon)
                {
                    // Send got Pokemon proto message
                    await GrpcClientService.SendRpcPayloadAsync(pkmn, PayloadType.Pokemon);
                }

                if (pkmn.IsNewPokemonWithIV)
                {
                    // Send got Pokemon IV proto message
                    await GrpcClientService.SendRpcPayloadAsync(pkmn, PayloadType.Pokemon, hasIV: true);
                }
            }
        }

        private static async Task HandleGymAsync(PokemonFortProto fort, string username)
        {
            await GrpcClientService.SendRpcPayloadAsync(fort, PayloadType.Fort, username);
        }

        private static async Task HandlePlayerDataAsync(string username, ushort level, uint xp)
        {
            var payload = new
            {
                username,
                level,
                xp,
            };
            await GrpcClientService.SendRpcPayloadAsync(payload, PayloadType.PlayerData, username);
        }

        #endregion
    }
}