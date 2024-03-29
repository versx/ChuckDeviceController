﻿namespace ChuckDeviceController.Services.DataConsumer;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Options;

using ChuckDeviceController.Collections.Extensions;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.HostedServices;

public class DataConsumerQueue : ConcurrentDictionary<SqlQueryType, ConcurrentBag<BaseEntity>>
{
    public DataConsumerQueue(IOptions<DataConsumerOptionsConfig> options)
        : base(options.Value.QueueConcurrencyLevelMultiplier * Environment.ProcessorCount,
               (int)options.Value.Queue.MaximumCapacity)
    {
    }
}

public class DataConsumerService : TimedHostedService, IDataConsumerService
{
    #region Variables

    private readonly CancellationTokenSource _tokenSource;
    private readonly DataConsumerQueue _queue;
    private readonly ILogger<IDataConsumerService> _logger;
    private readonly SqlBulk _bulk = new();
    //private static readonly IReadOnlyDictionary<SqlQueryType, ColumnDataExpression<dynamic>> upsertExpressions = new Dictionary<SqlQueryType, ColumnDataExpression<dynamic>>
    //{
    //    {
    //        SqlQueryType.CellOnMergeUpdate, new()
    //        {
    //            { "id", x => x.Id },
    //            { "center_lat", x => x.Latitude },
    //            { "center_lon", x => x.Longitude },
    //            { "level", x => x.Level },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.WeatherOnMergeUpdate, new()
    //        {
    //            { "id", x => x.Id },
    //            { "level", x => x.Level },
    //            { "latitude", x => x.Latitude },
    //            { "longitude", x => x.Longitude },
    //            { "gameplay_condition", x => x.GameplayCondition },
    //            { "cloud_level", x => x.CloudLevel },
    //            { "rain_level", x => x.RainLevel },
    //            { "snow_level", x => x.SnowLevel },
    //            { "fog_level", x => x.FogLevel },
    //            { "wind_level", x => x.WindLevel },
    //            { "wind_direction", x => x.WindDirection },
    //            { "warn_weather", x => x.WarnWeather },
    //            { "special_effect_level", x => x.SpecialEffectLevel },
    //            { "severity", x => x.Severity },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.SpawnpointOnMergeUpdate, new()
    //        {
    //            { "id", x => x.Id },
    //            { "lat", x => x.Latitude },
    //            { "lon", x => x.Longitude },
    //            { "despawn_sec", x => x.DespawnSecond },
    //            { "last_seen", x => x.LastSeen },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.PokestopIgnoreOnMerge, new()
    //        {
    //            { "lure_id", x => x.LureId },
    //            { "lure_expire_timestamp", x => x.LureExpireTimestamp },
    //            { "sponsor_id", x => x.SponsorId },
    //            { "ar_scan_eligible", x => x.IsArScanEligible },
    //            { "quest_type", x => x.QuestType },
    //            { "quest_template", x => x.QuestTemplate },
    //            { "quest_title", x => x.QuestTitle },
    //            { "quest_target", x => x.QuestTarget },
    //            { "quest_timestamp", x => x.QuestTimestamp },
    //            { "quest_conditions", x => x.QuestConditions },
    //            { "quest_rewards", x => x.QuestRewards },
    //            { "alternative_quest_type", x => x.AlternativeQuestType },
    //            { "alternative_quest_template", x => x.AlternativeQuestTemplate },
    //            { "alternative_quest_title", x => x.AlternativeQuestTitle },
    //            { "alternative_quest_target", x => x.AlternativeQuestTarget },
    //            { "alternative_quest_timestamp", x => x.AlternativeQuestTimestamp },
    //            { "alternative_quest_conditions", x => x.AlternativeQuestConditions },
    //            { "alternative_quest_rewards", x => x.AlternativeQuestRewards },
    //            { "id", x => x.Id },
    //            { "lat", x => x.Latitude },
    //            { "lon", x => x.Longitude },
    //            { "name", x => x.Name },
    //            { "url", x => x.Url },
    //            { "enabled", x => x.IsEnabled },
    //            { "deleted", x => x.IsDeleted },
    //            { "cell_id", x => x.CellId },
    //            { "power_up_points", x => x.PowerUpPoints },
    //            { "power_up_level", x => x.PowerUpLevel },
    //            { "power_up_end_timestamp", x => x.PowerUpEndTimestamp },
    //            { "first_seen_timestamp", x => x.FirstSeenTimestamp },
    //            { "last_modified_timestamp", x => x.LastModifiedTimestamp },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.PokestopOnMergeUpdate, new()
    //        {
    //            { "lure_id", x => x.LureId },
    //            { "lure_expire_timestamp", x => x.LureExpireTimestamp },
    //            { "sponsor_id", x => x.SponsorId },
    //            { "ar_scan_eligible", x => x.IsArScanEligible },
    //            { "quest_type", x => x.QuestType },
    //            { "quest_template", x => x.QuestTemplate },
    //            { "quest_title", x => x.QuestTitle },
    //            { "quest_target", x => x.QuestTarget },
    //            { "quest_timestamp", x => x.QuestTimestamp },
    //            { "quest_conditions", x => x.QuestConditions },
    //            { "quest_rewards", x => x.QuestRewards },
    //            { "alternative_quest_type", x => x.AlternativeQuestType },
    //            { "alternative_quest_template", x => x.AlternativeQuestTemplate },
    //            { "alternative_quest_title", x => x.AlternativeQuestTitle },
    //            { "alternative_quest_target", x => x.AlternativeQuestTarget },
    //            { "alternative_quest_timestamp", x => x.AlternativeQuestTimestamp },
    //            { "alternative_quest_conditions", x => x.AlternativeQuestConditions },
    //            { "alternative_quest_rewards", x => x.AlternativeQuestRewards },
    //            { "id", x => x.Id },
    //            { "lat", x => x.Latitude },
    //            { "lon", x => x.Longitude },
    //            { "name", x => x.Name },
    //            { "url", x => x.Url },
    //            { "enabled", x => x.IsEnabled },
    //            { "deleted", x => x.IsDeleted },
    //            { "cell_id", x => x.CellId },
    //            { "power_up_points", x => x.PowerUpPoints },
    //            { "power_up_level", x => x.PowerUpLevel },
    //            { "power_up_end_timestamp", x => x.PowerUpEndTimestamp },
    //            { "first_seen_timestamp", x => x.FirstSeenTimestamp },
    //            { "last_modified_timestamp", x => x.LastModifiedTimestamp },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.IncidentOnMergeUpdate, new()
    //        {
    //            { "id", x => x.Id },
    //            { "pokestop_id", x => x.PokestopId },
    //            { "start", x => x.Start },
    //            { "expiration", x => x.Expiration },
    //            { "display_type", x => x.DisplayType },
    //            { "style", x => x.Style },
    //            { "`character`", x => x.Character },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.GymOnMergeUpdate, new()
    //        {
    //            { "guarding_pokemon_id", x => x.GuardingPokemonId },
    //            { "available_slots", x => x.AvailableSlots },
    //            { "team_id", x => x.Team },
    //            { "in_battle", x => x.InBattle },
    //            { "ex_raid_eligible", x => x.IsExRaidEligible },
    //            { "raid_level", x => x.RaidLevel },
    //            { "raid_end_timestamp", x => x.RaidEndTimestamp },
    //            { "raid_spawn_timestamp", x => x.RaidSpawnTimestamp },
    //            { "raid_battle_timestamp", x => x.RaidBattleTimestamp },
    //            { "raid_pokemon_id", x => x.RaidPokemonId },
    //            { "raid_pokemon_move_1", x => x.RaidPokemonMove1 },
    //            { "raid_pokemon_move_2", x => x.RaidPokemonMove2 },
    //            { "raid_pokemon_form", x => x.RaidPokemonForm },
    //            { "raid_pokemon_costume", x => x.RaidPokemonCostume },
    //            { "raid_pokemon_cp", x => x.RaidPokemonCP },
    //            { "raid_pokemon_evolution", x => x.RaidPokemonEvolution },
    //            { "raid_pokemon_gender", x => x.RaidPokemonGender },
    //            { "raid_is_exclusive", x => x.RaidIsExclusive },
    //            { "total_cp", x => x.TotalCP },
    //            { "sponsor_id", x => x.SponsorId },
    //            { "ar_scan_eligible", x => x.IsArScanEligible },
    //            { "id", x => x.Id },
    //            { "lat", x => x.Latitude },
    //            { "lon", x => x.Longitude },
    //            { "name", x => x.Name },
    //            { "url", x => x.Url },
    //            { "enabled", x => x.IsEnabled },
    //            { "deleted", x => x.IsDeleted },
    //            { "cell_id", x => x.CellId },
    //            { "power_up_points", x => x.PowerUpPoints },
    //            { "power_up_level", x => x.PowerUpLevel },
    //            { "power_up_end_timestamp", x => x.PowerUpEndTimestamp },
    //            { "first_seen_timestamp", x => x.FirstSeenTimestamp },
    //            { "last_modified_timestamp", x => x.LastModifiedTimestamp },
    //            { "updated", x => x.Updated },
    //        }
    //    },
    //    {
    //        SqlQueryType.PokemonIgnoreOnMerge, new()
    //        {
    //            { "id", x => x.Id },
    //            { "pokemon_id", x => x.PokemonId },
    //            { "lat", x => x.Latitude },
    //            { "lon", x => x.Longitude },
    //            { "spawn_id", x => x.SpawnId },
    //            { "expire_timestamp", x => x.ExpireTimestamp },
    //            { "atk_iv", x => x.AttackIV },
    //            { "def_iv", x => x.DefenseIV },
    //            { "sta_iv", x => x.StaminaIV },
    //            { "move_1", x => x.Move1 },
    //            { "move_2", x => x.Move2 },
    //            { "gender", x => x.Gender },
    //            { "form", x => x.Form },
    //            { "costume", x => x.Costume },
    //            { "cp", x => x.CP },
    //            { "level", x => x.Level },
    //            { "weight", x => x.Weight },
    //            { "size", x => x.Size },
    //            { "weather", x => x.Weather },
    //            { "shiny", x => x.IsShiny },
    //            { "username", x => x.Username },
    //            { "pokestop_id", x => x.PokestopId },
    //            { "first_seen_timestamp", x => x.FirstSeenTimestamp },
    //            { "updated", x => x.Updated },
    //            { "changed", x => x.Changed },
    //            { "cell_id", x => x.CellId },
    //            { "expire_timestamp_verified", x => x.IsExpireTimestampVerified },
    //            { "capture_1", x => x.Capture1 },
    //            { "capture_2", x => x.Capture2 },
    //            { "capture_3", x => x.Capture3 },
    //            { "is_ditto", x => x.IsDitto },
    //            { "display_pokemon_id", x => x.DisplayPokemonId },
    //            { "base_height", x => x.BaseHeight },
    //            { "base_weight", x => x.BaseWeight },
    //            { "is_event", x => x.IsEvent },
    //            { "seen_type", x => x.SeenType },
    //            { "pvp", x => x.PvpRankings },
    //        }
    //    },
    //    {
    //        SqlQueryType.PokemonOnMergeUpdate, new()
    //        {
    //            { "id", x => x.Id },
    //            { "pokemon_id", x => x.PokemonId },
    //            { "lat", x => x.Latitude },
    //            { "lon", x => x.Longitude },
    //            { "spawn_id", x => x.SpawnId },
    //            { "expire_timestamp", x => x.ExpireTimestamp },
    //            { "atk_iv", x => x.AttackIV },
    //            { "def_iv", x => x.DefenseIV },
    //            { "sta_iv", x => x.StaminaIV },
    //            { "move_1", x => x.Move1 },
    //            { "move_2", x => x.Move2 },
    //            { "gender", x => x.Gender },
    //            { "form", x => x.Form },
    //            { "costume", x => x.Costume },
    //            { "cp", x => x.CP },
    //            { "level", x => x.Level },
    //            { "weight", x => x.Weight },
    //            { "size", x => x.Size },
    //            { "weather", x => x.Weather },
    //            { "shiny", x => x.IsShiny },
    //            { "username", x => x.Username },
    //            { "pokestop_id", x => x.PokestopId },
    //            { "first_seen_timestamp", x => x.FirstSeenTimestamp },
    //            { "updated", x => x.Updated },
    //            { "changed", x => x.Changed },
    //            { "cell_id", x => x.CellId },
    //            { "expire_timestamp_verified", x => x.IsExpireTimestampVerified },
    //            { "capture_1", x => x.Capture1 },
    //            { "capture_2", x => x.Capture2 },
    //            { "capture_3", x => x.Capture3 },
    //            { "is_ditto", x => x.IsDitto },
    //            { "display_pokemon_id", x => x.DisplayPokemonId },
    //            { "base_height", x => x.BaseHeight },
    //            { "base_weight", x => x.BaseWeight },
    //            { "is_event", x => x.IsEvent },
    //            { "seen_type", x => x.SeenType },
    //            { "pvp", x => x.PvpRankings },
    //        }
    //    },
    //};

    #endregion

    #region Properties

    public DataConsumerOptionsConfig Options { get; }

    #endregion

    #region Constructor

    public DataConsumerService(
        ILogger<IDataConsumerService> logger,
        DataConsumerQueue queue,
        IOptions<DataConsumerOptionsConfig> options)
        : base(logger, options.Value.IntervalS)
    {
        Options = options.Value ?? new();

        _logger = logger;
        _queue = queue;
        _tokenSource = new CancellationTokenSource();
    }

    #endregion

    #region Public Methods

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"{nameof(IDataConsumerService)} is now running in the background.");

        await Task.CompletedTask;
    }

    protected override async Task RunJobAsync(CancellationToken stoppingToken) =>
        await ConsumeDataAsync(stoppingToken);

    #endregion

    #region Private Methods

    private async Task ConsumeDataAsync(CancellationToken stoppingToken)
    {
        var entityCount = _queue.GetCount();
        if (entityCount == 0)
            return;

        var requestId = Guid.NewGuid().ToString()[..8];
        CheckQueueLength(requestId);

        try
        {
            var entitiesToUpsert = await _queue.TakeAllAsync(new SqlQueryTypeComparer(), stoppingToken);
            //var entitiesToUpsert = await _queue.TakeAsync(new SqlQueryTypeComparer(), (int)Options.Queue.MaximumBatchSize, stoppingToken);
            //var entitiesToUpsert = _queue.Take(new SqlQueryTypeComparer(), (int)Options.Queue.MaximumBatchSize);

            var sw = new Stopwatch();
            if (Options.ShowProcessingTimes)
            {
                sw.Start();
                _logger.LogTrace($"[{requestId}] Prepared {entityCount:N0} data entities for MySQL database upsert...");
            }

            var sqls = _bulk.PrepareSqlQuery(entitiesToUpsert, (int)Options.Queue.MaximumBatchSize);
            var result = await EntityRepository.ExecuteAsync(sqls, stoppingToken: stoppingToken);

            if (Options.ShowProcessingTimes)
            {
                sw.Stop();
            }

            var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, Options.DecimalPrecision);
            var rowsAffected = result; //results.Sum(x => x.RowsAffected);
            var batchCount = sqls.Count(); //results.Sum(x => x.BatchCount);
            var expectedCount = entityCount; //results.Sum(x => x.ExpectedCount);

            PrintBenchmarkResults(requestId, DataLogLevel.Summary,
                new BenchmarkResults(
                    rowsAffected, expectedCount, batchCount,
                    "total entities", totalSeconds, sw),
                entitiesToUpsert
            );

            ProtoDataStatistics.Instance.TotalEntitiesUpserted += (uint)rowsAffected;
            ProtoDataStatistics.Instance.AddTimeEntry(new(Convert.ToUInt64(entityCount), totalSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogError($"[{requestId}] ConsumeDataAsync: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    //private async Task<SqlBulkResult> UpsertEntitiesAsync(SqlQueryType sqlType, IEnumerable<BaseEntity> entities, CancellationToken stoppingToken = default)
    //{
    //    // ~ 2-5s
    //    //var entity = entities.FirstOrDefault();
    //    //if (entity == null)
    //    //{
    //    //    _logger.LogError($"Fail");
    //    //    continue;
    //    //}

    //    //var tableName = EntityRepository.GetTableAttribute(entity.GetType());
    //    //if (string.IsNullOrEmpty(tableName))
    //    //{
    //    //    _logger.LogError($"Failed to get table name for entity type '{entity.GetType().Name}'");
    //    //    continue;
    //    //}

    //    //if (!upsertExpressions.ContainsKey(sqlType))
    //    //{
    //    //    _logger.LogWarning($"Upsert expression does not exist for SQL query type '{sqlType}', skipping...");
    //    //    continue;
    //    //}

    //    //var affectedRows = await EntityRepository.ExecuteBulkAsync(tableName, entities, upsertExpressions[sqlType], stoppingToken);
    //    //var result = new SqlBulkResult(true, 1, affectedRows, entityCount);


    //    // ~ 1-3s
    //    //string sqlQuery, sqlValues;
    //    //if (_sqlCache.ContainsKey(sqlType))
    //    //{
    //    //    (sqlQuery, sqlValues) = _sqlCache[sqlType];
    //    //}
    //    //else
    //    //{
    //    //    (sqlQuery, sqlValues) = SqlQueryBuilder.GetQuery(sqlType);
    //    //    _sqlCache.Add(sqlType, (sqlQuery, sqlValues));
    //    //}

    //    //var includedProperties = _fortDetailTypes.Contains(sqlType)
    //    //    ? _fortDetailColumns
    //    //    : null;
    //    //var result = await _bulk.InsertBulkRawAsync(
    //    //    sqlQuery,
    //    //    sqlValues,
    //    //    entities,
    //    //    batchSize: Options?.MaximumBatchSize ?? DataConsumerOptionsConfig.DefaultMaxBatchSize,
    //    //    includedProperties,
    //    //    ignoredProperties: null,
    //    //    stoppingToken
    //    //);
    //    //return result;
    //    return null;
    //}

    private void PrintBenchmarkResults(string requestId, DataLogLevel logLevel, BenchmarkResults results, SortedDictionary<SqlQueryType, ConcurrentBag<BaseEntity>> entities)
    {
        var time = string.Empty;
        if (Options.ShowProcessingTimes)
        {
            results.Stopwatch?.Stop();
            var totalSeconds = Math.Round(results.Stopwatch?.Elapsed.TotalSeconds ?? 0, Options.DecimalPrecision).ToString("F5");
            time = results.Stopwatch != null
                ? $" in {totalSeconds}s"
                : string.Empty;
        }

        //_logger.LogInformation($"{nameof(DataConsumerService)} upserted {results.RowsAffected:N0}/{results.EntityCount:N0} {results.Text} between {results.BatchCount:N0} batches{time}");
        var grouped = entities.Select(pair => $"{pair.Key}: {pair.Value.Count:N0}");
        var sb = new StringBuilder();
        //sb.AppendLine();
        //sb.Append(nameof(DataConsumerService));
        //sb.Append(' ');
        sb.Append($"[{requestId}]");
        sb.Append(' ');
        sb.Append("Upserted");
        sb.Append(' ');
        sb.Append($"{results.RowsAffected:N0}");
        sb.Append('/');
        sb.Append($"{results.EntityCount:N0}");
        sb.Append(' ');
        sb.Append(results.Text);
        sb.Append(' ');
        sb.Append("between");
        sb.Append(' ');
        sb.Append(results.BatchCount);
        sb.Append(' ');
        sb.Append("batches");
        sb.AppendLine(time);
        //sb.Append("Details:");
        //sb.Append(' ');
        sb.Append('[');
        sb.Append(string.Join(", ", grouped));
        sb.Append(']');
        sb.AppendLine();
        var message = sb.ToString();
        _logger.LogTrace(message);
    }

    private void CheckQueueLength(string requestId)
    {
        var count = _queue.Sum(x => x.Value.Count);
        var usage = $"{count:N0}/{Options.Queue.MaximumCapacity:N0}";
        if (count >= Options.Queue.MaximumCapacity)
        {
            _logger.LogError($"[{requestId}] Data consumer queue is at maximum capacity! {usage}");
        }
        else if (count >= Options.Queue.MaximumSizeWarning)
        {
            _logger.LogWarning($"[{requestId}] Data consumer queue is over normal capacity with {usage} items total, consider increasing 'MaximumQueueBatchSize'");
        }
    }

    #endregion
}

public class SqlQueryTypeComparer : IComparer<SqlQueryType>
{
    public int Compare(SqlQueryType x, SqlQueryType y) => ((int)x).CompareTo((int)y);
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