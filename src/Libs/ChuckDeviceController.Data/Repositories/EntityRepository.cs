namespace ChuckDeviceController.Data.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions.Http.Caching;

    public class EntityRepository
    {
        private static readonly ILogger<EntityRepository> _logger =
            new Logger<EntityRepository>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Warning)));
        //private static readonly List<ConnectionState> _invalidConnectionStates = new()
        //{
        //    ConnectionState.Broken,
        //    ConnectionState.Closed,
        //};
        private static MySqlConnection? _connection;

        //public static async Task<MySqlConnection?> CreateConnectionAsync(string connectionString, bool openConnection = true, CancellationToken stoppingToken = default)
        public static MySqlConnection? CreateConnection(string connectionString)
        {
            ////using var connection = new MySqlConnection(connectionString);
            //if (_connection == null) //||
            //    _invalidConnectionStates.Contains(_connection?.State ?? ConnectionState.Closed))
            //{
            //    if (_connection?.State == ConnectionState.Connecting)
            //    {
            //        // Wait for connection request to finish
            //        await Task.Delay(3000, stoppingToken).ConfigureAwait(false);
            //    }

            //    if (_connection != null)
            //    {
            //        await _connection.DisposeAsync().ConfigureAwait(false);
            //    }
            //    _connection = new MySqlConnection(connectionString);

            //    if (openConnection)
            //    {
            //        await _connection.OpenAsync(stoppingToken);
            //    }
            //}
            //return _connection;
            return _connection ??= new MySqlConnection(connectionString);
        }

        public static async Task<TEntity?> GetEntityAsync<TKey, TEntity, TDbContext>(
            TDbContext context,
            IMemoryCacheHostedService memCache,
            TKey key,
            bool setCache = true,
            double expiryLimitM = 15)
            where TKey : notnull //class
            where TEntity : BaseEntity
            where TDbContext : DbContext
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
                    entity = await context.FindAsync<TEntity>(key);
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
    }
}