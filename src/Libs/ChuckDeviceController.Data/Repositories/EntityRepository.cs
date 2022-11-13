namespace ChuckDeviceController.Data.Repositories
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Reflection;

    using Dapper;
    using Microsoft.Extensions.Logging;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    // TODO: Create new MySQL connection instance for each method

    public class EntityRepository
    {
        private const uint DefaultConnectionWaitTimeS = 3;
        private const int DefaultCommandTimeoutS = 30;
        private const double DefaultExpiryLimitM = 15;

        #region Variables

        private static readonly ILogger<EntityRepository> _logger =
            new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug)));
        private static readonly SemaphoreSlim _sem = new(1);
        private static readonly SemaphoreSlim _entitySem = new(1);
        private static readonly IEnumerable<ConnectionState> _invalidConnectionStates = new[]
        {
            ConnectionState.Broken,
            ConnectionState.Closed,
        };
        private static MySqlConnection? _connection;
        private static string ConnectionString;
        private readonly string _connectionString;
        private readonly bool _openConnection;

        #endregion

        #region Singleton

        private static EntityRepository? _instance = null;
        private static readonly object _mutex = new();

        public static EntityRepository Instance =>
            _instance ?? throw new Exception($"{nameof(EntityRepository)} singleton has not been initialized");

        public static EntityRepository InstanceWithOptions(string connectionString, bool openConnection = true)
        {
            ConnectionString = connectionString;
            if (_instance == null)
            {
                lock (_mutex)
                {
                    _instance ??= new EntityRepository(connectionString, openConnection);
                }
            }
            return _instance;
        }

        #endregion

        #region Constructor

        private EntityRepository(string connectionString, bool openConnection = true)
        {
            _connectionString = connectionString;
            _openConnection = openConnection;

            Task.Run(async () => _connection = await CreateConnectionAsync()).ConfigureAwait(false);
        }

        #endregion

        #region GetEntity Methods

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity>(
            TKey key,
            IMemoryCacheHostedService memCache,
            bool setCache = true,
            double expiryLimitM = DefaultExpiryLimitM)
            where TKey : notnull //class
            where TEntity : BaseEntity
        {
            await _entitySem.WaitAsync();

            var defaultExpiry = TimeSpan.FromMinutes(expiryLimitM);
            TEntity? GetFromCache(TKey key)
            {
                TEntity? entity = default;
                try
                {
                    entity = memCache.Get<TKey, TEntity>(key);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[GetEntityAsync] GetFromCache: {ex.InnerException?.Message ?? ex.Message}");
                }
                return entity;
            }

            //async Task<TEntity?> GetFromDatabase(TKey key)
            //{
            //    TEntity? entity = default;
            //    try
            //    {
            //        entity = await GetEntityAsync<TKey, TEntity>(key);
            //        if (setCache)
            //        {
            //            memCache.Set(key, entity, defaultExpiry);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError($"[GetEntityAsync] GetFromDatabase: {ex.InnerException?.Message ?? ex.Message}");
            //    }
            //    return entity;
            //}

            //var entity = GetFromCache(key) ?? await GetFromDatabase(key);
            var entity = GetFromCache(key);
            if (entity == null)
            {
                entity = await GetEntityAsync<TKey, TEntity>(key);
                if (setCache)
                {
                    memCache.Set(key, entity, defaultExpiry);
                }
            }
            _entitySem.Release();

            return entity;
        }

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity>(TKey key, IEnumerable<string>? includeColumns = null)
            where TKey : notnull //class
            where TEntity : BaseEntity
        {
            string sql;
            TEntity? entity = default;
            try
            {
                var tableName = GetTableAttribute<TEntity>();
                if (string.IsNullOrEmpty(tableName))
                {
                    // TODO: Fallback to name of entity?
                    return null;
                }
                var keyName = GetKeyAttribute<TEntity>();
                if (string.IsNullOrEmpty(keyName))
                {
                    // TODO: Fallback to `id`?
                    return null;
                }

                var columns = (includeColumns?.Any() ?? false)
                    ? string.Join(", ", includeColumns)
                    : "*";
                sql = $"SELECT {columns} FROM {tableName} WHERE {keyName} = '{key}'";

                // TODO: await _sem.WaitAsync();
                using var connection = new MySqlConnection(ConnectionString);
                await connection.OpenAsync();
                entity = await connection.QueryFirstOrDefaultAsync<TEntity>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetEntityAsync]: {ex.InnerException?.Message ?? ex.Message}");
            }
            // TODO: _sem.Release();

            return entity;
        }

        #endregion

        #region Execute Methods

        /// <summary>
        /// Execute parameterized SQL query.
        /// </summary>
        /// <param name="sql">SQL query to execute.</param>
        /// <param name="param">Parameter for SQL query.</param>
        /// <param name="commandTimeoutS">SQL command timeout in seconds.</param>
        /// <returns>Returns the number of rows affected by the query.</returns>
        /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
        public static async Task<int> ExecuteAsync(string sql, object? param = null, int? commandTimeoutS = DefaultCommandTimeoutS)
        {
            if (_connection == null)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }

            var rowsAffected = 0;
            await _sem.WaitAsync();

            try
            {
                rowsAffected = await _connection.ExecuteAsync(sql, param, commandTimeout: commandTimeoutS);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }

            _sem.Release();
            return rowsAffected;
        }

        /// <summary>
        /// Execute raw SQL query.
        /// </summary>
        /// <param name="sqls">List of SQL queries to execute.</param>
        /// <param name="commandTimeoutS">SQL command timeout in seconds.</param>
        /// <param name="stoppingToken">Cancellation token to signal exit.</param>
        /// <returns>Returns the number of rows affected by the query.</returns>
        /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
        public static async Task<int> ExecuteAsync(IEnumerable<string> sqls, int? commandTimeoutS = DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
        {
            if (_connection == null)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }

            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            // TODO: _connection
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync(stoppingToken);
            using var trans = await connection.BeginTransactionAsync(stoppingToken);
            try
            {
                foreach (var sql in sqls)
                {
                    rowsAffected += await connection.ExecuteAsync(sql, transaction: trans, commandTimeout: commandTimeoutS);
                }
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
                await trans.RollbackAsync(stoppingToken);
            }

            _sem.Release();
            return rowsAffected;
        }

        /// <summary>
        /// Execute parameterized SQL query.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="tableName">Name of SQL table.</param>
        /// <param name="entities">List of data entities to include.</param>
        /// <param name="dataFunc">Function to determine what columns to include.</param>
        /// <param name="stoppingToken">Cancellation token to signal exit.</param>
        /// <returns>Returns the number of rows affected by the query.</returns>
        /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
        public static async Task<int> ExecuteBulkAsync<TEntity>(string tableName, IEnumerable<TEntity> entities, DataUpdater<TEntity> dataFunc, CancellationToken stoppingToken = default)
        {
            if (_connection == null)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }

            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            using var trans = await _connection.BeginTransactionAsync();
            try
            {
                rowsAffected += await _connection.BulkInsertAsync(tableName, entities, dataFunc, trans, includeOnDuplicateQuery: true);
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
                await trans.RollbackAsync();   
            }

            _sem.Release();
            return rowsAffected;
        }

        #endregion

        #region Private Methods

        private async Task<MySqlConnection?> CreateConnectionAsync(CancellationToken stoppingToken = default)
        {
            //using var connection = new MySqlConnection(connectionString);
            if (_connection == null ||
                _invalidConnectionStates.Contains(_connection?.State ?? ConnectionState.Closed))
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync().ConfigureAwait(false);
                }

                _connection ??= new MySqlConnection(_connectionString);
                if (_openConnection)
                {
                    await _connection.OpenAsync(stoppingToken);
                    await WaitForConnectionAsync();
                }
            }
            return _connection;
        }

        private static async Task WaitForConnectionAsync(uint waitTimeS = DefaultConnectionWaitTimeS)
        {
            while ((_connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
            {
                await Task.Delay(TimeSpan.FromSeconds(waitTimeS));
            }
        }

        #endregion

        #region Attribute Helpers

        public static string? GetTableAttribute<TEntity>()
        {
            var attr = typeof(TEntity).GetCustomAttribute<TableAttribute>();
            var table = attr?.Name;
            return table;
        }

        public static string? GetKeyAttribute<TEntity>()
        {
            var type = typeof(TEntity);
            var properties = type.GetProperties();
            var attr = properties.FirstOrDefault(prop => prop.GetCustomAttribute<KeyAttribute>() != null);
            var key = attr?.Name;
            return key;
        }

        #endregion
    }
}