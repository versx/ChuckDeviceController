namespace ChuckDeviceController.Services
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Services.Rpc;

    public class DataConsumerService : IDataConsumerService
    {
        #region Constants

        private const int MaxBatchSize = 1000;
        private const int MaxCapacity = 1024 * 1024;
        private static readonly int ConcurrencyLevel = Environment.ProcessorCount * 4;

        #endregion

        #region Variables

        private readonly ConcurrentDictionary<SqlQueryType, ConcurrentBag<BaseEntity>> _queue = new(ConcurrencyLevel, MaxCapacity);
        private readonly System.Timers.Timer _timer = new();
        private readonly ILogger<IDataConsumerService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IGrpcClientService _grpcClientService;
        private readonly SqlBulk _bulk = new();
        private readonly Dictionary<SqlQueryType, (string, string)> _sqlCache = new();
        private readonly IEnumerable<SqlQueryType> _fortDetailTypes = new[]
        {
            SqlQueryType.PokestopDetailsOnMergeUpdate,
            SqlQueryType.GymDetailsOnMergeUpdate,
        };
        private readonly IEnumerable<string> _fortDetailColumns = new[]
        {
            "Id",
            "Name",
            "Url",
        };

        #endregion

        #region Properties

        public DataConsumerOptionsConfig Options { get; }

        public bool ShowBenchmarkTimes => _env?.IsDevelopment() ?? false;

        #endregion

        #region Constructor

        public DataConsumerService(
            ILogger<IDataConsumerService> logger,
            IWebHostEnvironment env,
            IGrpcClientService grpcClientService,
            IOptions<DataConsumerOptionsConfig> options)
        {
            _logger = logger;
            _env = env;
            _grpcClientService = grpcClientService;

            Options = options.Value;

            _timer.Interval = Options.IntervalS * 1000;
            _timer.Elapsed += async (sender, e) => await ConsumeDataAsync(new());
            _timer.Start();
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

        #endregion

        #region Private Methods

        private async Task ConsumeDataAsync(CancellationToken stoppingToken)
        {
            if (!_queue.Any())
                return;

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
                        ? _fortDetailColumns
                        : null;
                    var result = await _bulk.InsertBulkRawAsync(
                        sqlQuery,
                        sqlValues,
                        entities,
                        batchSize: Options?.MaximumBatchSize ?? MaxBatchSize,
                        includedProperties,
                        null,
                        stoppingToken
                    );
                    if (!result.Success)
                    {
                        _logger.LogError($"Failed to insert {entityCount:N0} entities");
                        //continue;
                    }
                    results.Add(result);
                }

                sw.Stop();

                var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 5);
                var rowsAffected = results.Sum(x => x.RowsAffected);
                var batchCount = results.Sum(x => x.BatchCount);
                var expectedCount = results.Sum(x => x.ExpectedCount);

                PrintBenchmarkResults(DataLogLevel.Summary,
                    new BenchmarkResults(
                        rowsAffected, expectedCount, batchCount,
                        "total entities", totalSeconds, sw)
                );

                ProtoDataStatistics.Instance.TotalEntitiesUpserted += (uint)rowsAffected;
                ProtoDataStatistics.Instance.AddTimeEntry(new(Convert.ToUInt64(entityCount), totalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ConsumeDataAsync: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void PrintBenchmarkResults(DataLogLevel logLevel, BenchmarkResults results)
        {
            var time = string.Empty;
            if (ShowBenchmarkTimes)
            {
                results.Stopwatch?.Stop();
                var totalSeconds = Math.Round(results.Stopwatch?.Elapsed.TotalSeconds ?? 0, 5).ToString("F5");
                time = results.Stopwatch != null
                    ? $" in {totalSeconds}s"
                    : string.Empty;
            }
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