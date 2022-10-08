namespace ChuckDeviceController.Services
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.EntityFrameworkCore;
    using Z.BulkOperations;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Services.Rpc;

    public class DataConsumerService : IDataConsumerService
    {
        #region Constants

        private const uint DataConsumerIntervalMs = 10 * 1000;
        private const int EntitySemMax = 3;
        private const uint SemaphoreLockWaitTimeMs = 3 * 1000;

        #endregion

        #region Variables

        //private static readonly ConcurrentDictionaryQueue<TEntity> _queue = new();

        private static readonly ConcurrentDictionaryQueue<Pokemon> _pokemonToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Pokestop> _pokestopsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Gym> _gymsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<GymDefender> _gymDefendersToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<GymTrainer> _gymTrainersToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Incident> _incidentsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Spawnpoint> _spawnpointsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Cell> _cellsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Weather> _weatherToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Account> _accountsToUpsert = new();

        //private static readonly SemaphoreSlim _upsertSem = new(0, EntitySemMax);
        //private static readonly object _cellLock = new();
        private static readonly System.Timers.Timer _timer = new();
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromMilliseconds(SemaphoreLockWaitTimeMs);

        private readonly ILogger<IDataConsumerService> _logger;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IDbContextFactory<MapDbContext> _factory;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private MapDbContext context;

        #endregion

        #region Constructor

        public DataConsumerService(
            ILogger<IDataConsumerService> logger,
            IGrpcClientService grpcClientService,
            IDbContextFactory<MapDbContext> factory,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _grpcClientService = grpcClientService;
            _factory = factory;
            _serviceScopeFactory = serviceScopeFactory;

            _timer.Interval = DataConsumerIntervalMs;
            _timer.Elapsed += async (sender, e) => await ConsumeDataAsync(new());
            _timer.Start();
        }

        #endregion

        public async Task AddPokemonAsync(BulkOperation<Pokemon> options, Pokemon entity)
        {
            if (_pokemonToUpsert.ContainsKey(options))
            {
                _pokemonToUpsert[options].Add(entity);
            }
            else
            {
                if (!_pokemonToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add wild Pokemon to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddPokestopAsync(BulkOperation<Pokestop> options, Pokestop entity)
        {
            if (_pokestopsToUpsert.ContainsKey(options))
            {
                _pokestopsToUpsert[options].Add(entity);
            }
            else
            {
                if (!_pokestopsToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Pokestop to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddGymAsync(BulkOperation<Gym> options, Gym entity)
        {
            if (_gymsToUpsert.ContainsKey(options))
            {
                _gymsToUpsert[options].Add(entity);
            }
            else
            {
                if (!_gymsToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Gym to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddGymDefenderAsync(BulkOperation<GymDefender> options, GymDefender entity)
        {
            if (_gymDefendersToUpsert.ContainsKey(options))
            {
                _gymDefendersToUpsert[options].Add(entity);
            }
            else
            {
                if (!_gymDefendersToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Gym Defender to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddGymTrainerAsync(BulkOperation<GymTrainer> options, GymTrainer entity)
        {
            if (_gymTrainersToUpsert.ContainsKey(options))
            {
                _gymTrainersToUpsert[options].Add(entity);
            }
            else
            {
                if (!_gymTrainersToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Gym Trainer to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddIncidentAsync(BulkOperation<Incident> options, Incident entity)
        {
            if (_incidentsToUpsert.ContainsKey(options))
            {
                _incidentsToUpsert[options].Add(entity);
            }
            else
            {
                if (!_incidentsToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Pokestop Incident to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddIncidentsAsync(BulkOperation<Incident> options, IEnumerable<Incident> entities)
        {
            if (_incidentsToUpsert.ContainsKey(options))
            {
                _incidentsToUpsert[options].AddRange(entities);
            }
            else
            {
                if (!_incidentsToUpsert.TryAdd(options, entities.ToList()))
                {
                    _logger.LogError($"Failed to add Pokestop Incident to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddSpawnpointAsync(BulkOperation<Spawnpoint> options, Spawnpoint entity)
        {
            if (_spawnpointsToUpsert.ContainsKey(options))
            {
                _spawnpointsToUpsert[options].Add(entity);
            }
            else
            {
                if (!_spawnpointsToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Spawnpoint to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddWeatherAsync(BulkOperation<Weather> options, Weather entity)
        {
            if (_weatherToUpsert.ContainsKey(options))
            {
                _weatherToUpsert[options].Add(entity);
            }
            else
            {
                if (!_weatherToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Weather cell to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddCellAsync(BulkOperation<Cell> options, Cell entity)
        {
            if (_cellsToUpsert.ContainsKey(options))
            {
                _cellsToUpsert[options].Add(entity);
            }
            else
            {
                if (!_cellsToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add S2 cell to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddCellsAsync(BulkOperation<Cell> options, IEnumerable<Cell> entities)
        {
            if (_cellsToUpsert.ContainsKey(options))
            {
                _cellsToUpsert[options].AddRange(entities);
            }
            else
            {
                if (!_cellsToUpsert.TryAdd(options, entities.ToList()))
                {
                    _logger.LogError($"Failed to add S2 cell to queue");
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddAccountAsync(BulkOperation<Account> options, Account entity)
        {
            if (_accountsToUpsert.ContainsKey(options))
            {
                _accountsToUpsert[options].Add(entity);
            }
            else
            {
                if (!_accountsToUpsert.TryAdd(options, new() { entity }))
                {
                    _logger.LogError($"Failed to add Account to queue");
                }
            }

            await Task.CompletedTask;
        }

        //public async Task AddEntityAsync(BulkOperation<TEntity> options, TEntity entity)
        //{
        //    /*
        //    if (typeof(TEntity) == typeof(Pokemon))
        //    {
        //        if (_pokemonToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_pokemonToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add wild Pokemon to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Pokestop))
        //    {
        //        if (_pokestopsToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_pokestopsToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Gym))
        //    {
        //        if (_gymsToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_gymsToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(GymTrainer))
        //    {
        //        if (_gymTrainersToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_gymTrainersToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(GymDefender))
        //    {
        //        if (_gymDefendersToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_gymDefendersToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Incident))
        //    {
        //        if (_incidentsToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_incidentsToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Spawnpoint))
        //    {
        //        if (_spawnpointsToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_spawnpointsToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Weather))
        //    {
        //        if (_weatherToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_weatherToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Cell))
        //    {
        //        if (_cellsToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_cellsToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else if (typeof(TEntity) == typeof(Account))
        //    {
        //        if (_accountsToUpsert.ContainsKey(options))
        //        {
        //            _queue[options].Add(entity);
        //        }
        //        else
        //        {
        //            if (!_accountsToUpsert.TryAdd(options, new() { entity }))
        //            {
        //                _logger.LogError($"Failed to add Pokestop to queue");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Unknown entity type provided
        //    }
        //    */


        //    if (_queue.ContainsKey(options))
        //    {
        //        _queue[options].Add(entity);
        //    }
        //    else
        //    {
        //        if (!_queue.TryAdd(options, new() { entity }))
        //        {
        //            _logger.LogError($"Failed to add wild Pokemon to queue");
        //        }
        //    }

        //    await Task.CompletedTask;
        //}

        //public async Task AddEntitiesAsync(BulkOperation<TEntity> options, IEnumerable<TEntity> entities)
        //{
        //    if (_queue.ContainsKey(options))
        //    {
        //        _queue[options].AddRange(entities);
        //    }
        //    else
        //    {
        //        if (!_queue.TryAdd(options, entities.ToList()))
        //        {
        //            _logger.LogError($"Failed to add wild Pokemon to queue");
        //        }
        //    }

        //    await Task.CompletedTask;
        //}

        public async Task ConsumeDataAsync(CancellationToken stoppingToken)
        {
            try
            {
                var pokestopsToUpsert = await _pokestopsToUpsert.TakeAllAsync();
                var gymsToUpsert = await _gymsToUpsert.TakeAllAsync();
                var gymDefendersToUpsert = await _gymDefendersToUpsert.TakeAllAsync();
                var gymTrainersToUpsert = await _gymTrainersToUpsert.TakeAllAsync();
                var spawnpointsToUpsert = await _spawnpointsToUpsert.TakeAllAsync();
                var pokemonToUpsert = await _pokemonToUpsert.TakeAllAsync();
                var weatherToUpsert = await _weatherToUpsert.TakeAllAsync();
                var cellsToUpsert = await _cellsToUpsert.TakeAllAsync();
                var accountsToUpsert = await _accountsToUpsert.TakeAllAsync();

                var entityCount = pokestopsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    gymsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    gymDefendersToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    gymTrainersToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    spawnpointsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    pokemonToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    cellsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    weatherToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    accountsToUpsert.Sum(x => x.Value?.Count ?? 0);
                if (entityCount == 0)
                    return;

                var batchCount = pokestopsToUpsert.Count +
                    gymsToUpsert.Count +
                    gymDefendersToUpsert.Count +
                    gymTrainersToUpsert.Count +
                    spawnpointsToUpsert.Count +
                    pokemonToUpsert.Count +
                    cellsToUpsert.Count +
                    weatherToUpsert.Count +
                    accountsToUpsert.Count;

                _logger.LogInformation($"Preparing to upsert {entityCount:N0} entities between {batchCount:N0} batches...");
                //await _upsertSem.WaitAsync(_semWaitTime, stoppingToken);

                var sw = new Stopwatch();
                sw.Start();

                //using var scope = _serviceScopeFactory.CreateAsyncScope();
                //using var context = scope.ServiceProvider.GetRequiredService<MapDbContext>();

                if (context == null)
                {
                    context = await _factory.CreateDbContextAsync(stoppingToken);
                }

                if (cellsToUpsert.Any())
                {
                    try
                    {
                        var ts = DateTime.UtcNow.ToTotalSeconds();
                        var cellsSql = cellsToUpsert
                            .SelectMany(x => x.Value)
                            .Select(cell => $"({cell.Id}, {cell.Level}, {cell.Latitude}, {cell.Longitude}, {ts})");
                        var args = string.Join(",", cellsSql);
                        var sql = string.Format(SqlQueries.S2Cells, args);
                        var result = await context.Database.ExecuteSqlRawAsync(sql, stoppingToken);
                        _logger.LogInformation($"[Cell] Raw Result: {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Cell] Error: {ex.Message}");
                    }
                }

                if (weatherToUpsert.Any())
                {
                    foreach (var (options, weather) in weatherToUpsert)
                    {
                        try
                        {
                            await context.Weather.BulkMergeAsync(weather, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Weather] Error: {ex.Message}");
                        }
                    }
                }

                if (pokestopsToUpsert.Any())
                {
                    foreach (var (options, pokestops) in pokestopsToUpsert)
                    {
                        try
                        {
                            await context.Pokestops.BulkMergeAsync(pokestops, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Pokestop] Error: {ex.Message}");
                        }
                    }
                }

                if (gymsToUpsert.Any())
                {
                    foreach (var (options, gyms) in gymsToUpsert)
                    {
                        try
                        {
                            await context.Gyms.BulkMergeAsync(gyms, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Gym] Error: {ex.Message}");
                        }
                    }
                }

                if (gymTrainersToUpsert.Any())
                {
                    foreach (var (options, gymTrainers) in gymTrainersToUpsert)
                    {
                        try
                        {
                            await context.GymTrainers.BulkMergeAsync(gymTrainers, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[GymTrainer] Error: {ex.Message}");
                        }
                    }
                }

                if (gymDefendersToUpsert.Any())
                {
                    foreach (var (options, gymDefenders) in gymDefendersToUpsert)
                    {
                        try
                        {
                            await context.GymDefenders.BulkMergeAsync(gymDefenders, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[GymDefender] Error: {ex.Message}");
                        }
                    }
                }

                if (spawnpointsToUpsert.Any())
                {
                    foreach (var (options, spawnpoints) in spawnpointsToUpsert)
                    {
                        try
                        {
                            await context.Spawnpoints.BulkMergeAsync(spawnpoints, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Spawnpoint] Error: {ex.Message}");
                        }
                    }
                }

                if (pokemonToUpsert.Any())
                {
                    foreach (var (options, pokemon) in pokemonToUpsert)
                    {
                        try
                        {
                            await context.Pokemon.BulkMergeAsync(pokemon, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Pokemon] Error: {ex.Message}");
                        }
                    }

                    await SendPokemonAsync(pokemonToUpsert.SelectMany(x => x.Value).ToList());
                }

                if (accountsToUpsert.Any())
                {
                    // TODO: Use ControllerDbContext
                    foreach (var (options, accounts) in accountsToUpsert)
                    {
                        try
                        {
                            // TODO: await context.Accounts.BulkMergeAsync(accounts, o => o = options, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Account] Error: {ex.Message}");
                        }
                    }
                }

                sw.Stop();

                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                _logger.LogInformation($"Upserted {entityCount:N0} entities in {totalSeconds}s between {batchCount:N0} batches");
                PrintBenchmarkTimes(DataLogLevel.Summary, entityCount, "total entities", sw);

                ProtoDataStatistics.Instance.AddTimeEntry(new(Convert.ToUInt64(entityCount), totalSeconds));

                //_upsertSem.Release();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
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

        private void PrintBenchmarkTimes(DataLogLevel logLevel, int entityCount, string text = "total entities", Stopwatch? sw = null)
        {
            var time = string.Empty;
            //if (ShowBenchmarkTimes)
            {
                sw?.Stop();
                var totalSeconds = Math.Round(sw?.Elapsed.TotalSeconds ?? 0, 5).ToString("F5");
                time = sw != null
                    ? $" in {totalSeconds}s"
                    : string.Empty;
            }
            _logger.LogInformation($"{nameof(DataConsumerService)} upserted {entityCount:N0} {text}{time}");
        }
    }
}