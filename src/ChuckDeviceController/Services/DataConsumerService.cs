namespace ChuckDeviceController.Services
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Services.Rpc;

    // TODO: Add method to entities to build sql query with parameters
    // TODO: Add support for passing values sql to queues

    public class SqlQueryBuilder
    {
        public SqlQueryBuilder()
        {
        }

        public string BuildQuery(string sqlQuery, params object[] args)
        {
            var query = string.Format(sqlQuery, args);
            return query;
        }

        /// <summary>
        /// Returns SQL query related to provided query type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string GetQuery(SqlQueryType type)
        {
            return type switch
            {
                // Update IV
                //SqlQueryType.PokemonOnMergeUpdate => SqlQueries.PokemonOnMergeUpdate,
                // Do not update IV
                SqlQueryType.PokemonIgnoreOnMerge => SqlQueries.PokemonIgnoreOnMerge,
                // Insert everything
                SqlQueryType.PokemonOptions => SqlQueries.PokemonOptions,
                // Do not update quest properties
                SqlQueryType.PokestopIgnoreOnMerge => SqlQueries.PokestopIgnoreOnMerge,
                // Only update name/url/updated
                SqlQueryType.PokestopDetailsOnMergeUpdate => SqlQueries.PokestopDetailsOnMergeUpdate,
                SqlQueryType.IncidentOptions => SqlQueries.IncidentOnMergeUpdate,
                SqlQueryType.GymOptions => SqlQueries.GymOptions,
                // Only update name/url/updated
                SqlQueryType.GymDetailsOnMergeUpdate => SqlQueries.GymDetailsOnMergeUpdate,
                SqlQueryType.GymDefenderOptions => SqlQueries.GymDefenderOnMergeUpdate,
                SqlQueryType.GymTrainerOptions => SqlQueries.GymTrainerOnMergeUpdate,
                SqlQueryType.SpawnpointOnMergeUpdate => SqlQueries.SpawnpointOnMergeUpdate,
                SqlQueryType.CellOnMergeUpdate => SqlQueries.CellOnMergeUpdate,
                SqlQueryType.WeatherOnMergeUpdate => SqlQueries.WeatherOnMergeUpdate,
                _ => throw new NotImplementedException(),
            };
        }
    }

    public enum SqlQueryType
    {
        PokemonOnMergeUpdate,
        PokemonIgnoreOnMerge,
        PokemonOptions,
        PokestopIgnoreOnMerge,
        PokestopDetailsOnMergeUpdate,
        IncidentOptions,
        GymOptions,
        GymDetailsOnMergeUpdate,
        GymDefenderOptions,
        GymTrainerOptions,
        SpawnpointOnMergeUpdate,
        CellOnMergeUpdate,
        WeatherOnMergeUpdate,
    }

    public class DataConsumerService : IDataConsumerService
    {
        #region Constants

        private const uint DataConsumerIntervalS = 10;

        #endregion

        #region Variables

        //private static readonly ConcurrentDictionaryQueue<TEntity> _queue = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Pokemon>> _pokemonToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Pokestop>> _pokestopsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Gym>> _gymsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<GymDefender>> _gymDefendersToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<GymTrainer>> _gymTrainersToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Incident>> _incidentsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Spawnpoint>> _spawnpointsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Cell>> _cellsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Weather>> _weatherToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<string, List<Account>> _accountsToUpsert = new();

        private static readonly System.Timers.Timer _timer = new();

        private readonly ILogger<IDataConsumerService> _logger;
        private readonly IGrpcClientService _grpcClientService;
        //private readonly IConfiguration _configuration;
        //private readonly string _connectionString;
        private readonly SqlBulk _bulk;

        #endregion

        #region Constructor

        public DataConsumerService(
            ILogger<IDataConsumerService> logger,
            IGrpcClientService grpcClientService)
            //IConfiguration configuration)
        {
            _logger = logger;
            _grpcClientService = grpcClientService;
            //_configuration = configuration;

            _timer.Interval = DataConsumerIntervalS * 1000;
            _timer.Elapsed += async (sender, e) => await ConsumeDataAsync(new());
            _timer.Start();

            //_connectionString = _configuration.GetConnectionString("DefaultConnection");
            _bulk = new SqlBulk();
        }

        #endregion

        #region Public Methods

        public async Task AddPokemonAsync(string query, Pokemon entity)
        {
            if (_pokemonToUpsert.ContainsKey(query))
            {
                _pokemonToUpsert[query].Add(entity);
            }
            else
            {
                if (!_pokemonToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _pokemonToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddPokestopAsync(string query, Pokestop entity)
        {
            if (_pokestopsToUpsert.ContainsKey(query))
            {
                _pokestopsToUpsert[query].Add(entity);
            }
            else
            {
                if (!_pokestopsToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _pokestopsToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddGymAsync(string query, Gym entity)
        {
            if (_gymsToUpsert.ContainsKey(query))
            {
                _gymsToUpsert[query].Add(entity);
            }
            else
            {
                if (!_gymsToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _gymsToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddGymDefenderAsync(string query, GymDefender entity)
        {
            if (_gymDefendersToUpsert.ContainsKey(query))
            {
                _gymDefendersToUpsert[query].Add(entity);
            }
            else
            {
                if (!_gymDefendersToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _gymDefendersToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddGymTrainerAsync(string query, GymTrainer entity)
        {
            if (_gymTrainersToUpsert.ContainsKey(query))
            {
                _gymTrainersToUpsert[query].Add(entity);
            }
            else
            {
                if (!_gymTrainersToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _gymTrainersToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddIncidentAsync(string query, Incident entity)
        {
            if (_incidentsToUpsert.ContainsKey(query))
            {
                _incidentsToUpsert[query].Add(entity);
            }
            else
            {
                if (!_incidentsToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _incidentsToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddIncidentsAsync(string query, IEnumerable<Incident> entities)
        {
            if (_incidentsToUpsert.ContainsKey(query))
            {
                _incidentsToUpsert[query].AddRange(entities);
            }
            else
            {
                var list = entities.ToList();
                if (!_incidentsToUpsert.TryAdd(query, list))
                {
                    // Key already exists, add entity to queue
                    _incidentsToUpsert[query].AddRange(list);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddSpawnpointAsync(string query, Spawnpoint entity)
        {
            if (_spawnpointsToUpsert.ContainsKey(query))
            {
                _spawnpointsToUpsert[query].Add(entity);
            }
            else
            {
                if (!_spawnpointsToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _spawnpointsToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddWeatherAsync(string query, Weather entity)
        {
            if (_weatherToUpsert.ContainsKey(query))
            {
                _weatherToUpsert[query].Add(entity);
            }
            else
            {
                if (!_weatherToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _weatherToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddCellAsync(string query, Cell entity)
        {
            if (_cellsToUpsert.ContainsKey(query))
            {
                _cellsToUpsert[query].Add(entity);
            }
            else
            {
                if (!_cellsToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _cellsToUpsert[query].Add(entity);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddCellsAsync(string query, IEnumerable<Cell> entities)
        {
            if (_cellsToUpsert.ContainsKey(query))
            {
                _cellsToUpsert[query].AddRange(entities);
            }
            else
            {
                var list = entities.ToList();
                if (!_cellsToUpsert.TryAdd(query, list))
                {
                    // Key already exists, add entity to queue
                    _cellsToUpsert[query].AddRange(list);
                }
            }

            await Task.CompletedTask;
        }

        public async Task AddAccountAsync(string query, Account entity)
        {
            if (_accountsToUpsert.ContainsKey(query))
            {
                _accountsToUpsert[query].Add(entity);
            }
            else
            {
                if (!_accountsToUpsert.TryAdd(query, new() { entity }))
                {
                    // Key already exists, add entity to queue
                    _accountsToUpsert[query].Add(entity);
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

        #endregion

        #region Private Methods

        private async Task ConsumeDataAsync(CancellationToken stoppingToken)
        {
            try
            {
                var pokestopsToUpsert = await _pokestopsToUpsert.TakeAllAsync(stoppingToken);
                var incidentsToUpsert = await _incidentsToUpsert.TakeAllAsync(stoppingToken);
                var gymsToUpsert = await _gymsToUpsert.TakeAllAsync(stoppingToken);
                var gymDefendersToUpsert = await _gymDefendersToUpsert.TakeAllAsync(stoppingToken);
                var gymTrainersToUpsert = await _gymTrainersToUpsert.TakeAllAsync(stoppingToken);
                var spawnpointsToUpsert = await _spawnpointsToUpsert.TakeAllAsync(stoppingToken);
                var pokemonToUpsert = await _pokemonToUpsert.TakeAllAsync(stoppingToken);
                var weatherToUpsert = await _weatherToUpsert.TakeAllAsync(stoppingToken);
                var cellsToUpsert = await _cellsToUpsert.TakeAllAsync(stoppingToken);
                var accountsToUpsert = await _accountsToUpsert.TakeAllAsync(stoppingToken);

                var entityCount = pokestopsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                    incidentsToUpsert.Sum(x => x.Value?.Count ?? 0) +
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

                var results = new List<SqlBulkResult>();
                var sw = new Stopwatch();
                sw.Start();

                if (cellsToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, cells) in cellsToUpsert)
                        {
                            var result = await _bulk.InsertInBulkRawAsync(
                                sql,
                                SqlQueries.CellValuesRaw,
                                cells,
                                stoppingToken
                            );
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Cell] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (weatherToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, weather) in weatherToUpsert)
                        {
                            var result = await _bulk.InsertInBulkRawAsync(
                                sql,
                                SqlQueries.WeatherValuesRaw,
                                weather,
                                stoppingToken
                            );
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Weather] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (pokestopsToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, pokestops) in pokestopsToUpsert)
                        {
                            var result = await _bulk.InsertInBulkRawAsync(
                                sql,
                                SqlQueries.PokestopValuesRaw,
                                pokestops,
                                stoppingToken
                            );
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Pokestop] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (incidentsToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, incidents) in incidentsToUpsert)
                        {
                            var result = await _bulk.InsertInBulkRawAsync(
                                sql,
                                SqlQueries.IncidentValuesRaw,
                                incidents,
                                stoppingToken
                            );
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Incident] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (gymsToUpsert.Any())
                {
                    try
                    {
                        // TODO: Pass values sql when adding to queue to accommodate for gym/gym_details
                        foreach (var (sql, gyms) in gymsToUpsert)
                        {
                            var result = await _bulk.InsertInBulkRawAsync(
                                sql,
                                SqlQueries.GymValuesRaw,
                                gyms,
                                stoppingToken
                            );
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Gym] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (gymTrainersToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, gymTrainers) in gymTrainersToUpsert)
                        {
                            //var result = await _bulk.InsertInBulkRawAsync(
                            //    sql,
                            //    SqlQueries.GymTrainerValuesRaw,
                            //    gymTrainers,
                            //    stoppingToken
                            //);
                            //results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[GymTrainer] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (gymDefendersToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, gymDefenders) in gymDefendersToUpsert)
                        {
                            //var result = await _bulk.InsertInBulkRawAsync(
                            //    sql,
                            //    SqlQueries.GymDefenderValuesRaw,
                            //    gymDefenders,
                            //    stoppingToken
                            //);
                            //results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[GymDefender] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (spawnpointsToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, spawnpoints) in spawnpointsToUpsert)
                        {
                            var result = await _bulk.InsertInBulkRawAsync(
                                sql,
                                SqlQueries.SpawnpointValuesRaw,
                                spawnpoints,
                                stoppingToken
                            );
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Spawnpoint] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                if (pokemonToUpsert.Any())
                {
                    try
                    {
                        foreach (var (sql, pokemon) in pokemonToUpsert)
                        {
                            //var result = await _bulk.InsertInBulkRawAsync(
                            //    sql,
                            //    SqlQueries.PokemonValuesRaw,
                            //    pokemon,
                            //    stoppingToken
                            //);
                            //results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Pokemon] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }

                    await SendPokemonAsync(pokemonToUpsert.SelectMany(x => x.Value).ToList());
                }

                if (accountsToUpsert.Any())
                {
                    try
                    {
                        foreach (var (options, accounts) in accountsToUpsert)
                        {
                            //var result = await _bulk.InsertInBulkRawAsync(
                            //    sql,
                            //    SqlQueries.AccountValuesRaw,
                            //    accounts,
                            //    stoppingToken
                            //);
                            //results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Account] Error: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                sw.Stop();

                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                var rowsAffected = results.Sum(x => x.RowsAffected);
                var batchCount = results.Sum(x => x.BatchCount);
                var expectedCount = entityCount; //results.Sum(x => x.ExpectedCount);
                _logger.LogInformation($"Upserted {rowsAffected:N0}/{expectedCount:N0} entities in {totalSeconds}s between {batchCount:N0} batches");
                //PrintBenchmarkTimes(DataLogLevel.Summary, entityCount, "total entities", sw);

                ProtoDataStatistics.Instance.AddTimeEntry(new(Convert.ToUInt64(entityCount), totalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ConsumeDataAsync: {ex}");
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
                await Task.Run(() =>
                {
                    new Thread(async () =>
                    {
                        // Send got Pokemon proto message
                        await _grpcClientService.SendRpcPayloadAsync(
                                newPokemon,
                                PayloadType.PokemonList,
                                hasIV: false
                            );
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
                        await _grpcClientService.SendRpcPayloadAsync(
                            newPokemonWithIV,
                            PayloadType.PokemonList,
                            hasIV: true
                        );
                    })
                    { IsBackground = true }.Start();
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

        private static DataTable ConvertToDataTable<T>(IEnumerable<T> data)
        {
            var properties = TypeDescriptor.GetProperties(typeof(T));
            var dataTable = new DataTable();

            foreach (PropertyDescriptor prop in properties)
            {
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        #endregion
    }
}