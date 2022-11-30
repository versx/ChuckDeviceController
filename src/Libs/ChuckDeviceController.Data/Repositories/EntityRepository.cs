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
        private static ulong _instanceCount;
        public static ulong InstanceCount => _instanceCount;

        #region Constants

        private const uint DefaultConnectionWaitTimeS = 5;
        private const int DefaultCommandTimeoutS = 30;
        private const double DefaultExpiryLimitM = 15;

        #endregion

        #region Variables

        //private static readonly ILogger<EntityRepository> _logger =
        //    new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Trace)));
        private static readonly SemaphoreSlim _sem = new(1);
        private static readonly SemaphoreSlim _entitySem = new(25); // TODO: Make entity fetching concurrency level configurable
        //private static readonly SemaphoreSlim _entitySem = new(1);
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromSeconds(15); // TODO: Make entity fetch lock wait time configurable
        private static readonly IEnumerable<ConnectionState> _invalidConnectionStates = new[]
        {
            ConnectionState.Broken,
            ConnectionState.Closed,
        };
        private static MySqlConnection? _connection;
        private static string? _connectionString;
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

            Task.Run(async () => _connection = await CreateConnectionAsync()).Wait();
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
            if (_connection == null)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }

            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            using var trans = await _connection.BeginTransactionAsync(stoppingToken);
            try
            {
                rowsAffected = await _connection.ExecuteAsync(sql, param, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteAsync] Error: {ex}");
                await trans.RollbackAsync(stoppingToken);
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
            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            // TODO: _connection
            using var connection = await CreateConnectionAsync(stoppingToken: stoppingToken);
            if (connection == null)
            {
                Console.WriteLine("[ExecuteAsync} Error: Not connected to MySQL database server!");
                return rowsAffected;
            }
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
                Console.WriteLine($"[ExecuteAsync] Error: {ex}");
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
        public static async Task<int> ExecuteBulkAsync<TEntity>(string tableName, IEnumerable<TEntity> entities, ColumnDataExpression<TEntity> dataFunc, CancellationToken stoppingToken = default)
            where TEntity : BaseEntity
        {
            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            using var connection = await CreateConnectionAsync(stoppingToken: stoppingToken);
            if (connection == null)
            {
                Console.WriteLine("[ExecuteBulkAsync} Error: Not connected to MySQL database server!");
                return rowsAffected;
            }
            using var trans = await connection.BeginTransactionAsync(stoppingToken);
            try
            {
                rowsAffected += await connection.BulkInsertAsync(tableName, entities, dataFunc, trans, includeOnDuplicateQuery: true);
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteBulkAsync] Error: {ex}");
                await trans.RollbackAsync(stoppingToken);
            }

            _sem.Release();
            return rowsAffected;
        }

        #endregion

        #region CreateConnection Method

        public static MySqlConnection? CreateConnection(
            bool openConnection = true,
            bool runLeakWatcher = true,
            uint waitTimeS = DefaultConnectionWaitTimeS)
        {
            var task = Task.Run(async () =>  await CreateConnectionAsync(openConnection, runLeakWatcher, waitTimeS));
            task.Wait();
            return task.Result;
        }

        public static async Task<MySqlConnection?> CreateConnectionAsync(
            bool openConnection = true,
            bool runLeakWatcher = true,
            uint waitTimeS = DefaultConnectionWaitTimeS,
            CancellationToken stoppingToken = default)
        {
            var connection = new MySqlConnection(_connectionString);
            if (openConnection)
            {
                await connection.OpenAsync(stoppingToken);
                await connection.WaitForConnectionAsync(waitTimeS, stoppingToken);
            }

            Interlocked.Increment(ref _instanceCount);

            if (runLeakWatcher)
            {
                _ = new ConnectionLeakWatcher(connection);
            }
            return connection;
        }

        #endregion
    }
}