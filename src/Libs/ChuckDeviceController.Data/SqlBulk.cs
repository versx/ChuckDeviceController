namespace ChuckDeviceController.Data;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

using Microsoft.Extensions.Logging;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Logging;

// TODO: Use Stored Procedure for upsert queries
public class SqlBulk
{
    #region Constants

    private const int DefaultBatchSize = 1000;
    private const int DefaultCommandTimeoutS = 30;

    #endregion

    #region Variables

    //private static readonly SemaphoreSlim _sem = new(1, 1);
    private static readonly ILogger<SqlBulk> _logger =
        GenericLoggerFactory.CreateLogger<SqlBulk>();
    private readonly Dictionary<SqlQueryType, (string, string)> _sqlCache = new();
    private readonly IEnumerable<SqlQueryType> _fortDetailTypes = new[]
    {
        SqlQueryType.PokestopDetailsUpdateOnMerge,
        SqlQueryType.GymDetailsUpdateOnMerge,
    };
    private readonly IEnumerable<string> _fortDetailColumns = new[]
    {
        "Id",
        "Name",
        "Url",
    };

    #endregion

    #region Bulk Insert

    /// <summary>
    /// Parameterized
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="sqlQuery"></param>
    /// <param name="sqlValues"></param>
    /// <param name="entities"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task<SqlBulkResult> InsertBulkAsync<TEntity>(
        string sqlQuery,
        string sqlValues,
        IEnumerable<TEntity> entities,
        int batchSize = DefaultBatchSize,
        CancellationToken stoppingToken = default)
        where TEntity : BaseEntity
    {
        //await _sem.WaitAsync(stoppingToken);

        bool success;
        var rowsAffected = 0;
        var batchCount = 0;
        var expectedCount = entities.Count();

        try
        {
            var sqls = GenerateSqlQueryBatches(sqlQuery, sqlValues, entities, batchSize);
            batchCount = sqls.Count();
            foreach (var (sql, args) in sqls)
            {
                rowsAffected += await EntityRepository.ExecuteAsync(sql, (object)args, commandTimeoutS: DefaultCommandTimeoutS, stoppingToken);
            }
            success = true;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }

        //_sem.Release();
        return new SqlBulkResult(success, batchCount, rowsAffected, expectedCount);
    }

    /// <summary>
    /// Raw
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="sqlQuery"></param>
    /// <param name="sqlValues"></param>
    /// <param name="entities"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task<SqlBulkResult> InsertBulkRawAsync<TEntity>(
        string sqlQuery,
        string sqlValues,
        IEnumerable<TEntity> entities,
        int batchSize = DefaultBatchSize,
        IEnumerable<string>? includedProperties = null,
        IEnumerable<string>? ignoredProperties = null,
        CancellationToken stoppingToken = default)
        where TEntity : BaseEntity
    {
        //await _sem.WaitAsync(stoppingToken);

        bool success;
        var rowsAffected = 0;
        var batchCount = 0;
        var expectedCount = entities.Count();
        IEnumerable<string>? sqls;

        try
        {
            var useComma = !string.IsNullOrEmpty(sqlValues);
            sqls = GenerateSqlQueryBatchesRaw(sqlQuery, sqlValues, entities, batchSize, includedProperties, ignoredProperties, useCommaInsteadOfEndingStatement: useComma);
            batchCount = sqls.Count();
            rowsAffected += await EntityRepository.ExecuteAsync(sqls, commandTimeoutS: DefaultCommandTimeoutS, stoppingToken);
            success = true;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }

        //_sem.Release();
        return new SqlBulkResult(success, batchCount, rowsAffected, expectedCount);
    }

    public async Task<SqlBulkResult> UpdateAsync<TEntity>(string sqlQuery, TEntity entity)
    {
        //await _sem.WaitAsync(stoppingToken);

        var success = false;
        var rowsAffected = 0;
        var batchCount = 0;
        var expectedCount = 1;

        try
        {
            rowsAffected += await EntityRepository.ExecuteAsync(sqlQuery, entity, commandTimeoutS: DefaultCommandTimeoutS);
            success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }

        //_sem.Release();
        return new SqlBulkResult(success, batchCount, rowsAffected, expectedCount);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entitiesToUpsert"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public IEnumerable<string> PrepareSqlQuery<TEntity>(SortedDictionary<SqlQueryType, ConcurrentBag<TEntity>> entitiesToUpsert, int batchSize = DefaultBatchSize)
        where TEntity : BaseEntity
    {
        var sqls = new List<string>();

        try
        {
            foreach (var (sqlType, entities) in entitiesToUpsert)
            {
                string sqlQuery, sqlValues;
                if (_sqlCache.ContainsKey(sqlType))
                {
                    (sqlQuery, sqlValues) = _sqlCache[sqlType];
                }
                else
                {
                    (sqlQuery, sqlValues) = SqlQueryBuilder.GetQuery(sqlType);
                    _sqlCache.Add(sqlType, (sqlQuery, sqlValues));
                }

                var useComma = !string.IsNullOrEmpty(sqlValues);
                var sql = GenerateSqlQueryBatchesRaw(
                    sqlQuery,
                    sqlValues,
                    entities,
                    batchSize,
                    _fortDetailTypes.Contains(sqlType)
                        ? _fortDetailColumns
                        : null,
                    null,
                    useCommaInsteadOfEndingStatement: useComma
                );
                sqls.AddRange(sql);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {Message}", ex.InnerException?.Message ?? ex.Message);
        }

        return sqls;
    }

    #endregion

    #region Generate SQL Query Methods

    // https://stackoverflow.com/a/56250588

    /// <summary>
    /// Generates a SQL query in batches using parameterized queries.
    /// </summary>
    /// <typeparam name="TEntity">Entity data model</typeparam>
    /// <param name="sqlQuery">SQL query</param>
    /// <param name="sqlValues">SQL insert query string</param>
    /// <param name="entities">Entities to insert</param>
    /// <param name="batchSize">Maximum number of entities per batch</param>
    /// <returns>Returns a list of tuples containing the SQL query and the entities to pass as an argument.</returns>
    private static IEnumerable<(string, dynamic)> GenerateSqlQueryBatches<TEntity>(
        string sqlQuery,
        string sqlValues,
        IEnumerable<TEntity> entities,
        int batchSize = DefaultBatchSize)
        where TEntity : BaseEntity
    {
        var sqlsToExecute = new List<(string, dynamic)>();
        var batchCount = (int)Math.Ceiling((double)entities.Count() / batchSize);

        for (var i = 0; i < batchCount; i++)
        {
            var entityBatch = entities.Skip(i * batchSize).Take(batchSize);
            var queryValues = string.Join(',', entityBatch.Select(x => sqlValues));
            var query = string.Format(sqlQuery, queryValues);
            sqlsToExecute.Add((query, entityBatch));
        }

        return sqlsToExecute;
    }

    /// <summary>
    /// Generates a SQL query in batches using raw (possible SQL injection prone) queries.
    /// </summary>
    /// <typeparam name="TEntity">Entity data model</typeparam>
    /// <param name="sqlQuery">SQL query</param>
    /// <param name="sqlValues">SQL insert query string</param>
    /// <param name="entities">Entities to insert</param>
    /// <param name="batchSize">Maximum number of entities per batch</param>
    /// <returns>Returns a list of SQL queries including the entity data.</returns>
    private static IEnumerable<string> GenerateSqlQueryBatchesRaw<TEntity>(
        string sqlQuery,
        string sqlValues,
        IEnumerable<TEntity> entities,
        int batchSize = DefaultBatchSize,
        IEnumerable<string>? includedProperties = null,
        IEnumerable<string>? ignoredProperties = null,
        bool useCommaInsteadOfEndingStatement = true)
        where TEntity : BaseEntity
    {
        var sqlQueries = new List<string>();
        var batchCount = (int)Math.Ceiling((double)entities.Count() / batchSize);
        var endingStatement = useCommaInsteadOfEndingStatement
            ? ','
            : ';';

        for (var i = 0; i < batchCount; i++)
        {
            var entityBatch = entities.Skip(i * batchSize).Take(batchSize);
            var queryValues = string.Join(endingStatement, entityBatch.Select(entity =>
            {
                var propValues = entity!.GetPropertyValues(includedProperties, ignoredProperties);
                var result = string.IsNullOrEmpty(sqlValues)
                    ? string.Format(sqlQuery, propValues.ToArray())
                    : string.Format(sqlValues, propValues.ToArray());
                return result;
            }));
            var query = string.IsNullOrEmpty(sqlValues)
                ? queryValues
                : string.Format(sqlQuery, queryValues);
            sqlQueries.Add(query);
        }

        return sqlQueries;
    }

    #endregion
}