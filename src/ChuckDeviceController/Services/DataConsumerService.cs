namespace ChuckDeviceController.Services
{
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;

    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Services.Rpc;

    public class DataConsumerService : IDataConsumerService
    {
        #region Constants

        private const uint DataConsumerIntervalS = 3; //10;
        private const int DefaultMaxBatchSize = 1000;

        #endregion

        #region Variables

        private static readonly ConcurrentDictionaryQueue<SqlQueryType, List<BaseEntity>> _queue = new();
        private static readonly System.Timers.Timer _timer = new();

        private readonly ILogger<IDataConsumerService> _logger;
        private readonly IGrpcClientService _grpcClientService;
        private readonly SqlBulk _bulk;
        private readonly Dictionary<SqlQueryType, (string, string)> _sqlCache = new();
        private readonly IEnumerable<SqlQueryType> _fortDetailTypes = new[]
        {
            SqlQueryType.PokestopDetailsOnMergeUpdate,
            SqlQueryType.GymDetailsOnMergeUpdate,
        };

        #endregion

        public DataConsumerOptionsConfig Options { get; }

        #region Constructor

        public DataConsumerService(
            ILogger<IDataConsumerService> logger,
            IGrpcClientService grpcClientService,
            IOptions<DataConsumerOptionsConfig> options)
        {
            _logger = logger;
            _grpcClientService = grpcClientService;

            Options = options.Value;

            _timer.Interval = Options.IntervalS * 1000;
            _timer.Elapsed += async (sender, e) => await ConsumeDataAsync(new());
            _timer.Start();

            _bulk = new SqlBulk();
        }

        #endregion

        #region Public Methods

        public async Task AddEntityAsync(SqlQueryType query, BaseEntity entity)
        {
            if (_queue.ContainsKey(query))
            {
                _queue[query].Add(entity);
                return;
            }

            if (!_queue.TryAdd(query, new() { entity }))
            {
                // Key already exists, add entity to queue
                _queue[query].Add(entity);
            }

            await Task.CompletedTask;
        }

        public async Task AddEntitiesAsync(SqlQueryType query, IEnumerable<BaseEntity> entities)
        {
            try
            {
                if (_queue.ContainsKey(query))
                {
                    // TODO: Look into index outside of bounds exception. (list too big?)
                    _queue[query].AddRange(entities);
                }
                else
                {
                    var list = entities.ToList();
                    if (!_queue.TryAdd(query, list))
                    {
                        // Key already exists, add entity to queue
                        _queue[query].AddRange(entities);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task ConsumeDataAsync(CancellationToken stoppingToken)
        {
            if (!_queue.Any())
            {
                return;
            }

            try
            {
                var entitiesToUpsert = await _queue.TakeAllAsync(stoppingToken);
                var entityCount = entitiesToUpsert.Sum(x => x.Value?.Count ?? 0);
                if (entityCount == 0)
                    return;

                var results = new List<SqlBulkResult>();
                var sw = new Stopwatch();
                sw.Start();

                foreach (var (sqlType, entities) in entitiesToUpsert)
                {
                    //if (sqlType != SqlQueryType.CellOnMergeUpdate)
                    //    continue;

                    //var cells = entities.Select(x => (Cell)x);
                    //var result = await EntityRepository.ExecuteBulkAsync(tableName: "s2cell",  cells, new DataUpdater<Cell>
                    //{
                    //    { "id", x => x.Id },
                    //    { "center_lat", x => x.Latitude },
                    //    { "center_lon", x => x.Longitude },
                    //    { "level", x => x.Level },
                    //    { "updated", x => x.Updated },
                    //}, stoppingToken);
                    //results.Add(new SqlBulkResult(success: true, 1, result, cells.Count()));

                    string sqlQuery;
                    string sqlValues;
                    if (_sqlCache.ContainsKey(sqlType))
                    {
                        (sqlQuery, sqlValues) = _sqlCache[sqlType];
                    }
                    else
                    {
                        (sqlQuery, sqlValues) = SqlQueryBuilder.GetQuery(sqlType);
                        _sqlCache.Add(sqlType, (sqlQuery, sqlValues));
                    }

                    var includedProperties = _fortDetailTypes.Contains(sqlType)
                        ? new[] { "id", "name", "url" }
                        : null;
                    var result = await _bulk.InsertBulkRawAsync(
                        sqlQuery,
                        sqlValues,
                        entities,
                        batchSize: Options?.MaximumBatchSize ?? DefaultMaxBatchSize,
                        includedProperties,
                        null,
                        stoppingToken
                    );
                    if (!result.Success)
                    {
                        _logger.LogError($"Failed to insert {entities.Count:N0} entities");
                        continue;
                    }
                    results.Add(result);
                }

                // TODO: Move to DataProcessorService to be completely stateless
                //var pokemon = entitiesToUpsert
                //    .SelectMany(x => x.Value)
                //    .Where(x => x.GetType() == typeof(Pokemon))
                //    .Select(x => (Pokemon)x)
                //    .ToList();
                //if (pokemon.Any())
                //{
                //    await SendPokemonAsync(pokemon);
                //}

                sw.Stop();

                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                var rowsAffected = results.Sum(x => x.RowsAffected);
                var batchCount = results.Sum(x => x.BatchCount);
                var expectedCount = results.Sum(x => x.ExpectedCount);

                PrintBenchmarkResults(
                    DataLogLevel.Summary,
                    new BenchmarkResults(
                        rowsAffected, expectedCount, batchCount,
                        "total entities", totalSeconds, sw)
                );

                ProtoDataStatistics.Instance.AddTimeEntry(new(Convert.ToUInt64(entityCount), totalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ConsumeDataAsync: {ex.InnerException?.Message ?? ex.Message}");
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

        private void PrintBenchmarkResults(DataLogLevel logLevel, BenchmarkResults results)
        {
            //var time = string.Empty;
            //if (ShowBenchmarkTimes)
            //{
            results.Stopwatch?.Stop();
            var totalSeconds = Math.Round(results.Stopwatch?.Elapsed.TotalSeconds ?? 0, 5).ToString("F5");
            var time = results.Stopwatch != null
                ? $" in {totalSeconds}s"
                : string.Empty;
            //}
            _logger.LogInformation($"{nameof(DataConsumerService)} upserted {results.RowsAffected:N0}/{results.EntityCount:N0} {results.Text} between {results.BatchCount:N0} batches{time}");
        }

        #endregion
    }

    public class BenchmarkResults
    {
        public int RowsAffected { get; }

        public int EntityCount { get; }

        public int BatchCount { get; }

        public string? Text { get; }

        public double Seconds { get; }

        public Stopwatch? Stopwatch { get; }

        public BenchmarkResults(int rowsAffected, int entityCount, int batchCount, string? text, double seconds, Stopwatch? stopwatch)
        {
            RowsAffected = rowsAffected;
            EntityCount = entityCount;
            BatchCount = batchCount;
            Text = text ?? "total entities";
            Seconds = seconds;
            Stopwatch = stopwatch;
        }
    }
}