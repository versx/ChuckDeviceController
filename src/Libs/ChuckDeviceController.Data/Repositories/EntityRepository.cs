namespace ChuckDeviceController.Data.Repositories
{
    using System.Data;

    using Dapper;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    public class EntityRepository
    {
        public static ulong InstanceCount;

        #region Constants

        private const uint DefaultConnectionWaitTimeS = 5;
        private const int DefaultCommandTimeoutS = 30;
        private const double DefaultExpiryLimitM = 15;

        #endregion

        #region Variables

        //private static readonly ILogger<EntityRepository> _logger =
        //    new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Trace)));
        private static readonly SemaphoreSlim _sem = new(5);
        private static readonly SemaphoreSlim _entitySem = new(25); // TODO: Make entity fetching concurrency level configurable
        //private static readonly SemaphoreSlim _entitySem = new(1);
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromSeconds(15); // TODO: Make entity fetch lock wait time configurable
        private static readonly IEnumerable<ConnectionState> _invalidConnectionStates = new[]
        {
            ConnectionState.Broken,
            ConnectionState.Closed,
        };
        private static MySqlConnection? _connection;
        public static string ConnectionString;
        private readonly string _connectionString;
        private readonly bool _openConnection;
        private static EntityDataRepository _entityRepository;

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

            EntityDataRepository.AddTypeMappers();

            _entityRepository = new EntityDataRepository(connectionString);

            Task.Run(async () => _connection = await CreateConnectionAsync()).ConfigureAwait(false);
        }

        #endregion

        #region GetEntity Methods

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity>(
            MySqlConnection connection,
            TKey key,
            IMemoryCacheHostedService memCache,
            bool skipCache = false,
            bool setCache = true,
            double expiryLimitM = DefaultExpiryLimitM)
            where TKey : notnull //class
            where TEntity : BaseEntity
        {
            TEntity? entity;
            if (!skipCache)
            {
                entity = memCache.Get<TKey, TEntity>(key);
                if (entity != null)
                {
                    //_entitySem.Release();
                    return entity;
                }
            }

            //entity = await _entityRepository.GetByIdAsync<TKey, TEntity>(connection, key);
            entity = await _entityRepository.GetByIdAsync<TKey, TEntity>(connection, key);

            if (setCache && entity != null)
            {
                var defaultExpiry = TimeSpan.FromMinutes(expiryLimitM);
                memCache.Set(key, entity, defaultExpiry);
            }

            return entity;

            //await _entitySem.WaitAsync(_semWaitTime);
            ////var entity = GetFromCache(key) ?? await GetFromDatabase(key);

            //string sql;
            //TEntity? entity = null;

            //try
            //{
            //    if (!skipCache)
            //    {
            //        entity = memCache.Get<TKey, TEntity>(key);
            //        if (entity != null)
            //        {
            //            _entitySem.Release();
            //            return entity;
            //        }
            //    }

            //    //entity ??= await GetEntityAsync<TKey, TEntity>(key);
            //    var tableName = GetTableAttribute<TEntity>();
            //    if (string.IsNullOrEmpty(tableName))
            //    {
            //        _entitySem.Release();
            //        return null;
            //    }
            //    var keyName = GetKeyAttribute<TEntity>();
            //    if (string.IsNullOrEmpty(keyName))
            //    {
            //        _entitySem.Release();
            //        return null;
            //    }

            //    using var connection = new MySqlConnection(ConnectionString);
            //    if (connection.State != ConnectionState.Open)
            //    {
            //        await connection.OpenAsync();
            //        await WaitForConnectionAsync(connection);
            //    }

            //    sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{key}'";
            //    //entity = await _connection.QueryFirstOrDefaultAsync<TEntity>(sql, commandTimeout: DefaultCommandTimeoutS, commandType: CommandType.Text);
            //    entity = await connection.QueryFirstOrDefaultAsync<TEntity>(sql, commandTimeout: DefaultCommandTimeoutS, commandType: CommandType.Text);
            //    if (setCache && entity != null)
            //    {
            //        var defaultExpiry = TimeSpan.FromMinutes(expiryLimitM);
            //        memCache.Set(key, entity, defaultExpiry);
            //    }

            //    _entitySem.Release();
            //    return entity;
            //}
            //catch (Exception ex)
            //{
            //    //_logger.LogError($"Error: {ex}");
            //    Console.WriteLine($"[GetEntityAsync] Error: {ex}");
            //    _entitySem.Release();
            //}

            //return null;
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
        public static async Task<int> ExecuteAsync(string sql, object? param = null, int? commandTimeoutS = DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
        {
            //var result = await _entityRepository.ExecuteAsync(sql, param, commandTimeoutS);
            //return result;
            if (_connection == null)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }

            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            try
            {
                rowsAffected = await _connection.ExecuteAsync(sql, param, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Error: {ex}");
                Console.WriteLine($"[ExecuteAsync] Error: {ex}");
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
            //var result = await _entityRepository.ExecuteAsync(sqls, commandTimeoutS, stoppingToken: stoppingToken);
            //return result;
            //if (_connection == null)
            //{
            //    throw new Exception($"Not connected to MySQL database server!");
            //}

            var rowsAffected = 0;
            //await _sem.WaitAsync(stoppingToken);

            // TODO: _connection
            using var connection = new MySqlConnection(ConnectionString);
            //var leakMonitor = new ConnectionLeakWatcher(connection);

            await connection.OpenAsync(stoppingToken);
            using var trans = await connection.BeginTransactionAsync(stoppingToken);
            try
            {
                foreach (var sql in sqls)
                {
                    rowsAffected += await connection.ExecuteAsync(sql, transaction: trans, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
                }
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Error: {ex}");
                Console.WriteLine($"[ExecuteAsync] Error: {ex}");
                await trans.RollbackAsync(stoppingToken);
            }

            //_sem.Release();
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
        public static async Task<int> ExecuteBulkAsync<TEntity>(string tableName, IEnumerable<TEntity> entities, ColumnDataExpression<TEntity> dataFunc, CancellationToken stoppingToken = default)
            where TEntity : BaseEntity
        {
            //var result = await _entityRepository.ExecuteBulkAsync(tableName, entities, dataFunc, stoppingToken);
            //return result;
            //if (_connection == null)
            //{
            //    throw new Exception($"Not connected to MySQL database server!");
            //}

            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync(stoppingToken);
            using var trans = await connection.BeginTransactionAsync(stoppingToken);
            try
            {
                rowsAffected += await connection.BulkInsertAsync(tableName, entities, dataFunc, trans, includeOnDuplicateQuery: true);
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Error: {ex}");
                Console.WriteLine($"[ExecuteBulkAsync] Error: {ex}");
                await trans.RollbackAsync(stoppingToken);
            }

            _sem.Release();
            return rowsAffected;
        }

        #endregion

        #region Private Methods

        public static async Task<MySqlConnection?> CreateConnectionAsync(bool openConnection = true, CancellationToken stoppingToken = default)
        {
            var connection = new MySqlConnection(ConnectionString);
            if (openConnection)
            {
                await connection.OpenAsync(stoppingToken);
                while ((connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
                {
                    await Task.Delay(TimeSpan.FromSeconds(DefaultConnectionWaitTimeS), stoppingToken);
                }
            }

            Interlocked.Increment(ref InstanceCount);
            return connection;
        }

        private static async Task WaitForConnectionAsync(MySqlConnection connection, uint waitTimeS = DefaultConnectionWaitTimeS)
        {
            while ((connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
            {
                await Task.Delay(TimeSpan.FromSeconds(waitTimeS));
            }
        }

        #endregion
    }
}