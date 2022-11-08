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
    using ChuckDeviceController.Extensions.Http.Caching;

    public class EntityRepository
    {
        private const uint ConnectionWaitTimeS = 3;

        #region Variables

        private static readonly ILogger<EntityRepository> _logger =
            new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Warning)));
        private static readonly SemaphoreSlim _sem = new(1);
        private static readonly IEnumerable<ConnectionState> _invalidConnectionStates = new[]
        {
            ConnectionState.Broken,
            ConnectionState.Closed,
        };
        private static MySqlConnection? _connection;
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
            if (_instance == null)
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new EntityRepository(connectionString, openConnection);
                    }
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

        #region Public Methods

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity>(
            TKey key,
            IMemoryCacheHostedService memCache,
            bool setCache = true,
            double expiryLimitM = 15)
            where TKey : notnull //class
            where TEntity : BaseEntity
        {
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

            async Task<TEntity?> GetFromDatabase(TKey key)
            {
                TEntity? entity = default;
                try
                {
                    entity = await GetEntityAsync<TKey, TEntity>(key);
                    if (setCache)
                    {
                        memCache.Set(key, entity, defaultExpiry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[GetEntityAsync] GetFromDatabase: {ex.InnerException?.Message ?? ex.Message}");
                }
                return entity;
            }

            var entity = GetFromCache(key) ?? await GetFromDatabase(key);
            return entity;
        }

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity>(TKey key, IEnumerable<string>? includeColumns = null)
            where TKey : notnull //class
            where TEntity : BaseEntity
        {
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
                var sql = $"SELECT {columns} FROM {tableName} WHERE {keyName} = '{key}'";

                //await WaitForConnectionAsync();
                //using var connection = new MySqlConnection(_connectionString);
                await _sem.WaitAsync();
                var results = await _connection.QueryFirstOrDefaultAsync<TEntity>(sql);
                _sem.Release();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetEntityAsync]: {ex.InnerException?.Message ?? ex.Message}");
            }
            return null;
        }

        public static async Task<int> ExecuteAsync(
            string sql,
            object? param = null,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            await _sem.WaitAsync();
            var result = await _connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            _sem.Release();
            return result;
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

        private static async Task WaitForConnectionAsync(uint waitTimeS = ConnectionWaitTimeS)
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