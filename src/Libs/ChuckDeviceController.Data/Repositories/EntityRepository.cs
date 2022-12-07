namespace ChuckDeviceController.Data.Repositories
{
    using System.Data;

    using Dapper;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using static Dapper.SqlMapper;

    public class EntityRepository
    {
        #region Constants

        private const ushort DefaultConnectionLeakTimeoutS = 300; // 5 minutes
        private const double DefaultExpiryLimitM = 15;
        private const uint DefaultConnectionWaitTimeS = 30;
        private const int DefaultCommandTimeoutS = 30;

        #endregion

        #region Variables

        //private static readonly ILogger<EntityRepository> _logger =
        //    new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Trace)));
        private static readonly SemaphoreSlim _sem = new(5, 5);
        private static readonly SemaphoreSlim _semEntity = new(10); // TODO: Make entity fetching concurrency level configurable
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromSeconds(15); // TODO: Make entity fetch lock wait time configurable
        private static readonly IEnumerable<ConnectionState> _invalidConnectionStates = new[]
        {
            ConnectionState.Broken,
            ConnectionState.Closed,
        };
        //private static MySqlConnection? _connection;
        private static string? _connectionString;
        private readonly bool _openConnection;
        private static EntityDataRepository _entityRepository = default!;

        #endregion

        #region Properties

        private static ulong _instanceCount;
        public static ulong InstanceCount => _instanceCount;

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

            //_entityRepository = new EntityDataRepository(connectionString);
            _entityRepository = new EntityDataRepository();
            //Task.Run(async () => _connection = await CreateConnectionAsync()).Wait();
        }

        #endregion

        #region GetEntity Methods

        public static async Task<bool> EntityExistsAsync<TKey, TEntity>(
            MySqlConnection connection,
            TKey key,
            IMemoryCacheHostedService memCache,
            bool skipCache = false)
        {
            if (!skipCache)
            {
                var existsInCache = memCache.IsSet<TKey, TEntity>(key);
                return existsInCache;
            }

            try
            {
                var tableName = typeof(TEntity).GetTableAttribute();
                var keyName = typeof(TEntity).GetKeyAttribute();
                var sql = $"SELECT id FROM {tableName} WHERE {keyName} = '{key}'";
                var results = await connection.ExecuteAsync(sql, commandTimeout: DefaultCommandTimeoutS, commandType: CommandType.Text);
                var result = results == 1;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EntityExistsAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
            }
            return false;
        }

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
            TEntity? entity = null;
            if (!skipCache)
            {
                entity = memCache.Get<TKey, TEntity>(key);
                if (entity != null)
                {
                    return entity;
                }
            }

            //await _semEntity.WaitAsync();
            //entity = await _entityRepository.GetByIdAsync<TKey, TEntity>(connection, key);
            try
            {
                //EnsureConnectionIsOpen(attemptReopen: true);

                var tableName = typeof(TEntity).GetTableAttribute();
                var keyName = typeof(TEntity).GetKeyAttribute();
                var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{key}'";

                entity = await connection.QueryFirstOrDefaultAsync<TEntity>(
                    sql,
                    commandTimeout: DefaultCommandTimeoutS,
                    commandType: CommandType.Text
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetByIdAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
            }

            //_semEntity.Release();

            if (setCache && entity != null)
            {
                var defaultExpiry = TimeSpan.FromMinutes(expiryLimitM);
                memCache.Set(key, entity, defaultExpiry);
            }

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
        public static async Task<int> ExecuteAsync(string sql, object? param = null, int? commandTimeoutS = EntityDataRepository.DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
        {
            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            using var connection = await CreateConnectionAsync($"{nameof(EntityRepository)}::ExecuteAsync[Param]", stoppingToken: stoppingToken);
            if (connection == null)
            {
                Console.WriteLine("[ExecuteAsync} Error: Not connected to MySQL database server!");
                _sem.Release();
                return rowsAffected;
            }
            using var trans = await connection.BeginTransactionAsync(stoppingToken);
            try
            {
                rowsAffected = await connection.ExecuteAsync(sql, param, trans, commandTimeoutS, CommandType.Text);
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
                await trans.RollbackAsync(stoppingToken);
            }

            _sem.Release();
            return rowsAffected;
        }

        /// <summary>
        /// Execute parameterized SQL query.
        /// </summary>
        /// <param name="sql">SQL query to execute.</param>
        /// <param name="param">Parameter for SQL query.</param>
        /// <param name="commandTimeoutS">SQL command timeout in seconds.</param>
        /// <returns>Returns the number of rows affected by the query.</returns>
        /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
        public static async Task<int> ExecuteAsync(MySqlConnection connection, string sql, object? param = null, int? commandTimeoutS = EntityDataRepository.DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
        {
            var rowsAffected = 0;
            //await _sem.WaitAsync(stoppingToken);

            using var trans = await connection.BeginTransactionAsync(stoppingToken);
            try
            {
                rowsAffected = await connection.ExecuteAsync(sql, param, trans, commandTimeoutS, CommandType.Text);
                await trans.CommitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
                await trans.RollbackAsync(stoppingToken);
            }

            //_sem.Release();
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
        public static async Task<int> ExecuteAsync(IEnumerable<string> sqls, int? commandTimeoutS = EntityDataRepository.DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
        {
            var rowsAffected = 0;
            await _sem.WaitAsync(stoppingToken);

            using var connection = await CreateConnectionAsync($"{nameof(EntityRepository)}::ExecuteAsync[Raw]", stoppingToken: stoppingToken);
            if (connection == null)
            {
                Console.WriteLine("[ExecuteAsync} Error: Not connected to MySQL database server!");
                _sem.Release();
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
                Console.WriteLine($"[ExecuteAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
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

            using var connection = await CreateConnectionAsync($"{nameof(EntityRepository)}::ExecuteBulkAsync", stoppingToken: stoppingToken);
            if (connection == null)
            {
                Console.WriteLine("[ExecuteBulkAsync} Error: Not connected to MySQL database server!");
                _sem.Release();
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
                Console.WriteLine($"[ExecuteBulkAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
                await trans.RollbackAsync(stoppingToken);
            }

            _sem.Release();
            return rowsAffected;
        }

        #endregion

        #region CreateConnection Methods

        public static MySqlConnection? CreateConnection(
            string name,
            bool openConnection = true,
            bool runLeakWatcher = true,
            uint waitTimeS = EntityDataRepository.DefaultConnectionWaitTimeS)
        {
            var task = Task.Run(async () =>  await CreateConnectionAsync(name, openConnection, runLeakWatcher, waitTimeS));
            task.Wait();
            return task.Result;
        }

        public static async Task<MySqlConnection?> CreateConnectionAsync(
            string name,
            bool openConnection = true,
            bool runLeakWatcher = true,
            uint waitTimeS = EntityDataRepository.DefaultConnectionWaitTimeS,
            uint connectionLeakTimeoutS = DefaultConnectionLeakTimeoutS,
            CancellationToken stoppingToken = default)
        {
            var connection = new MySqlConnection(_connectionString);
            if (openConnection)
            {
                try
                {
                    await connection.OpenAsync(stoppingToken);
                    await connection.WaitForConnectionAsync(waitTimeS, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateConnectionAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            Interlocked.Increment(ref _instanceCount);

            if (runLeakWatcher)
            {
                //_ = new ConnectionLeakWatcher(name, connection, connectionLeakTimeoutS);
            }
            return connection;
        }

        #endregion
    }
}