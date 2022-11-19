namespace ChuckDeviceController.Data.Repositories
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Reflection;

    using Dapper;
    using Microsoft.Extensions.Logging;
    using MySqlConnector;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;
    using ChuckDeviceController.Extensions.Json;

    // TODO: Create new MySQL connection instance for each method

    public class EntityRepository
    {
        private const uint DefaultConnectionWaitTimeS = 5;
        private const int DefaultCommandTimeoutS = 30;
        private const double DefaultExpiryLimitM = 15;

        #region Variables

        private static readonly ILogger<EntityRepository> _logger =
            new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug)));
        private static readonly SemaphoreSlim _sem = new(1);
        private static readonly SemaphoreSlim _entitySem = new(5);
        private static readonly TimeSpan _semWaitTime = TimeSpan.FromSeconds(15);
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

            AddTypeMappers();

            Task.Run(async () => _connection = await CreateConnectionAsync()).ConfigureAwait(false);
        }

        #endregion

        #region GetEntity Methods

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity>(
            MySqlConnection connection2,
            TKey key,
            IMemoryCacheHostedService memCache,
            bool skipCache = false,
            bool setCache = true,
            double expiryLimitM = DefaultExpiryLimitM)
            where TKey : notnull //class
            where TEntity : BaseEntity
        {
            try
            {
                await _entitySem.WaitAsync(_semWaitTime);
                //var entity = GetFromCache(key) ?? await GetFromDatabase(key);

                TEntity? entity = null;
                if (!skipCache)
                {
                    entity = memCache.Get<TKey, TEntity>(key);
                    if (entity != null)
                    {
                        _entitySem.Release();
                        return entity;
                    }
                }

                //entity ??= await GetEntityAsync<TKey, TEntity>(key);
                var tableName = GetTableAttribute<TEntity>();
                if (string.IsNullOrEmpty(tableName))
                {
                    _entitySem.Release();
                    return null;
                }
                var keyName = GetKeyAttribute<TEntity>();
                if (string.IsNullOrEmpty(keyName))
                {
                    _entitySem.Release();
                    return null;
                }

                using var connection = new MySqlConnection(ConnectionString);
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                    await WaitForConnectionAsync(connection);
                }

                var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{key}'";
                //entity = await _connection.QueryFirstOrDefaultAsync<TEntity>(sql, commandTimeout: DefaultCommandTimeoutS, commandType: CommandType.Text);
                entity = await connection.QueryFirstOrDefaultAsync<TEntity>(sql, commandTimeout: DefaultCommandTimeoutS, commandType: CommandType.Text);
                if (setCache && entity != null)
                {
                    var defaultExpiry = TimeSpan.FromMinutes(expiryLimitM);
                    memCache.Set(key, entity, defaultExpiry);
                }

                _entitySem.Release();
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
                _entitySem.Release();
            }

            return null;
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
                rowsAffected = await _connection.ExecuteAsync(sql, param, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
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
                    rowsAffected += await connection.ExecuteAsync(sql, transaction: trans, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
                    //rowsAffected += await connection.ExecuteAsync(sql, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
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

            using var trans = await _connection.BeginTransactionAsync(stoppingToken);
            try
            {
                rowsAffected += await _connection.BulkInsertAsync(tableName, entities, dataFunc, trans, includeOnDuplicateQuery: true);
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

        private static void AddTypeMappers()
        {
            SetTypeMap<Account>();
            SetTypeMap<Device>();
            SetTypeMap<Pokestop>();
            SetTypeMap<Pokemon>();
            SetTypeMap<Gym>();
            SetTypeMap<GymDefender>();
            SetTypeMap<GymTrainer>();
            SetTypeMap<Incident>();
            SetTypeMap<Cell>();
            SetTypeMap<Weather>();
            SetTypeMap<Spawnpoint>();

            SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Dictionary<string, dynamic>>>());
            SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, dynamic>>());
            SqlMapper.AddTypeHandler(new SeenTypeTypeHandler());
            //GymDefender.Gender
            //GymTrainer.Team
            //Weather.WeatherCondition
        }

        private static void SetTypeMap<TEntity>()
        {
            SqlMapper.SetTypeMap(
                typeof(TEntity),
                new CustomPropertyTypeMap(
                    typeof(TEntity),
                    (type, columnName) =>
                        type.GetProperties().FirstOrDefault(prop =>
                            prop.GetCustomAttributes(false)
                                .OfType<ColumnAttribute>()
                                .Any(attr => attr.Name == columnName))));
        }
    }

    public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        public override T Parse(object value)
        {
            var json = value.ToString();
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            var obj = json.FromJson<T>();
            return obj ?? default;
        }

        public override void SetValue(IDbDataParameter parameter, T value)
        {
            var json = value?.ToJson();
            parameter.Value = json ?? null;
        }
    }

    public class SeenTypeTypeHandler : SqlMapper.TypeHandler<SeenType>
    {
        public override SeenType Parse(object value)
        {
            return Pokemon.StringToSeenType(value?.ToString() ?? string.Empty);
        }

        public override void SetValue(IDbDataParameter parameter, SeenType value)
        {
            var val = "'" + Pokemon.SeenTypeToString(value) + "'";
            parameter.Value = val;
        }
    }
}