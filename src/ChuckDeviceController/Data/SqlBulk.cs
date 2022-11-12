namespace ChuckDeviceController.Data
{
    using System.Collections.Generic;
    using System.Data;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;

    // TODO: Use nvarchar instead of varchar for string columns
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
            new Logger<SqlBulk>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug)));

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
                    rowsAffected += await EntityRepository.ExecuteAsync(sql, (object)args, commandTimeoutS: DefaultCommandTimeoutS);
                }
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError($"Error: {ex}");
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

            // TODO: Fix bandaid
            entities = entities.Where(x => x != null);

            bool success;
            var rowsAffected = 0;
            var batchCount = 0;
            var expectedCount = entities.Count();
            IEnumerable<string>? sqls = default;

            try
            {
                sqls = GenerateSqlQueryBatchesRaw(sqlQuery, sqlValues, entities, batchSize, includedProperties, ignoredProperties);
                batchCount = sqls.Count();
                rowsAffected += await EntityRepository.ExecuteAsync(sqls, commandTimeoutS: 30, stoppingToken);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError($"Error: {ex}");
            }

            //_sem.Release();
            return new SqlBulkResult(success, batchCount, rowsAffected, expectedCount);
        }

        public async Task<SqlBulkResult> UpdateAsync<TEntity>(string sqlQuery, TEntity entity)
        {
            //await _sem.WaitAsync(stoppingToken);

            bool success;
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
                success = false;
                _logger.LogError($"Error: {ex}");
            }

            //_sem.Release();
            return new SqlBulkResult(success, batchCount, rowsAffected, expectedCount);
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
            IEnumerable<string>? ignoredProperties = null)
            where TEntity : BaseEntity
        {
            var batchCount = (int)Math.Ceiling((double)entities.Count() / batchSize);
            var sqlQueries = new List<string>();

            for (var i = 0; i < batchCount; i++)
            {
                var entityBatch = entities.Skip(i * batchSize).Take(batchSize);
                var queryValues = string.Join(',', entityBatch.Select(entity =>
                {
                    var propValues = entity!.GetPropertyValues(includedProperties, ignoredProperties);
                    var result = string.Format(sqlValues, propValues.ToArray());
                    return result;
                }));
                var query = string.Format(sqlQuery, queryValues);
                sqlQueries.Add(query);
            }

            return sqlQueries;
        }

        #endregion
    }
}