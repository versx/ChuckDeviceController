namespace ChuckDeviceController.Services
{
    using System.Diagnostics;

    using Microsoft.EntityFrameworkCore;
    using Z.BulkOperations;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Protos;
    using ChuckDeviceController.Services.Rpc;

    public class DataConsumableQueueItem<T> where T : BaseEntity
    {
        public List<T> Entities { get; set; } = new();

        public BulkOperation<T> Options { get; set; }
    }

    public class DataConsumerService : IDataConsumerService
    {
        #region Constants

        private const uint DataConsumerIntervalMs = 10 * 1000;
        private const int EntitySemMax = 3;
        private const uint SemaphoreLockWaitTimeMs = 3 * 1000;

        #endregion

        #region Variables

        private readonly IAsyncQueue<DataConsumableQueueItem<BaseEntity>> _entityQueue;

        private static readonly ConcurrentDictionaryQueue<Pokemon> _pokemonToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Pokestop> _pokestopsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Gym> _gymsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<GymDefender> _gymDefendersToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<GymTrainer> _gymTrainersToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Incident> _incidentsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Spawnpoint> _spawnpointsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Cell> _cellsToUpsert = new();
        private static readonly ConcurrentDictionaryQueue<Weather> _weatherToUpsert = new();

        private static readonly SemaphoreSlim _upsertSem = new(0, 1);// EntitySemMax);
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromMilliseconds(SemaphoreLockWaitTimeMs);

        private readonly ILogger<IDataConsumerService> _logger;
        //private readonly IAsyncQueue<BaseEntity> _entityQueue;
        private readonly IGrpcClientService _grpcClientService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly System.Timers.Timer _timer = new();

        #endregion

        #region Constructor

        public DataConsumerService(
            ILogger<IDataConsumerService> logger,
            IAsyncQueue<DataConsumableQueueItem<BaseEntity>> entityQueue,
            //IAsyncQueue<BaseEntity> entityQueue,
            IGrpcClientService grpcClientService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _entityQueue = entityQueue;
            _grpcClientService = grpcClientService;
            _serviceScopeFactory = serviceScopeFactory;

            _timer.Interval = DataConsumerIntervalMs;
            _timer.Elapsed += async (sender, e) => await ConsumeCacheAsync(new());
            _timer.Start();
        }

        #endregion

        private async Task ConsumeCacheAsync(CancellationToken stoppingToken)
        {
            var pokestopsToUpsert = await _pokestopsToUpsert.TakeAllAsync();
            var gymsToUpsert = await _gymsToUpsert.TakeAllAsync();
            var gymDefendersToUpsert = await _gymDefendersToUpsert.TakeAllAsync();
            var gymTrainersToUpsert = await _gymTrainersToUpsert.TakeAllAsync();
            var spawnpointsToUpsert = await _spawnpointsToUpsert.TakeAllAsync();
            var pokemonToUpsert = await _pokemonToUpsert.TakeAllAsync();
            var weatherToUpsert = await _weatherToUpsert.TakeAllAsync();
            var cellsToUpsert = await _cellsToUpsert.TakeAllAsync();

            var entityCount = pokestopsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                gymsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                gymDefendersToUpsert.Sum(x => x.Value?.Count ?? 0) +
                gymTrainersToUpsert.Sum(x => x.Value?.Count ?? 0) +
                spawnpointsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                pokemonToUpsert.Sum(x => x.Value?.Count ?? 0) +
                cellsToUpsert.Sum(x => x.Value?.Count ?? 0) +
                weatherToUpsert.Sum(x => x.Value?.Count ?? 0);
            if (entityCount == 0)
                return;

            var batchCount = pokestopsToUpsert.Count +
                gymsToUpsert.Count +
                gymDefendersToUpsert.Count +
                gymTrainersToUpsert.Count +
                spawnpointsToUpsert.Count +
                pokemonToUpsert.Count +
                cellsToUpsert.Count +
                weatherToUpsert.Count;

            _logger.LogInformation($"Preparing to upsert {entityCount:N0} entities between {batchCount:N0} batches...");

            await _upsertSem.WaitAsync(_semWaitTime, stoppingToken);

            var sw = new Stopwatch();
            sw.Start();

            using var scope = _serviceScopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<MapDbContext>();

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

            sw.Stop();

            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
            _logger.LogInformation($"Upserted {entityCount:N0} entities in {totalSeconds}s between {batchCount:N0} batches");

            ProtoDataStatistics.Instance.AddTimeEntry(new(Convert.ToUInt64(entityCount), totalSeconds));
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
    }
}