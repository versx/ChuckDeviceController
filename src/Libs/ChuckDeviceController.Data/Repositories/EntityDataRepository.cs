namespace ChuckDeviceController.Data.Repositories
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;

    using Dapper;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.TypeHandlers;
    using ChuckDeviceController.Common.Data;

    public interface IEntityDataRepository
    {
        Task<TEntity?> GetByIdAsync<TKey, TEntity>(TKey key, CancellationToken stoppingToken = default)
            where TKey : notnull
            where TEntity : BaseEntity;

        Task<IEnumerable<TEntity>?> GetAllAsync<TEntity>(CancellationToken stoppingToken = default)
            where TEntity : BaseEntity;
    }

    public class EntityDataRepository : IEntityDataRepository
    {
        private const uint DefaultConnectionWaitTimeS = 3;
        private const int DefaultCommandTimeoutS = 30;

        private readonly SemaphoreSlim _sem = new(1);
        private readonly string _connectionString;
        private MySqlConnection? _connection;

        public EntityDataRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            AddTypeMappers();
            OpenConnection();

            var csb = MySqlConnectorFactory.Instance.CreateConnectionStringBuilder();
            csb.ConnectionString = _connectionString;
        }

        #region Public Methods

        public virtual async Task<TEntity?> GetByIdAsync<TKey, TEntity>(TKey key, CancellationToken stoppingToken = default)
            where TKey : notnull
            where TEntity : BaseEntity
        {
            await _sem.WaitAsync(stoppingToken);

            TEntity? result = null;
            try
            {
                EnsureConnectionIsOpen();

                var tableName = EntityRepository.GetTableAttribute<TEntity>();
                var keyName = EntityRepository.GetKeyAttribute<TEntity>();
                var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{key}'";
                result = await _connection.QueryFirstOrDefaultAsync<TEntity>(
                    sql,
                    commandTimeout: DefaultCommandTimeoutS,
                    commandType: CommandType.Text
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetByIdAsync] Error: {ex}");
            }

            _sem.Release();
            return result;
        }

        public virtual async Task<IEnumerable<TEntity>?> GetAllAsync<TEntity>(CancellationToken stoppingToken = default)
            where TEntity : BaseEntity
        {
            await _sem.WaitAsync(stoppingToken);

            IEnumerable<TEntity>? result = null;
            try
            {
                EnsureConnectionIsOpen();

                var tableName = EntityRepository.GetTableAttribute<TEntity>();
                var sql = $"SELECT * FROM {tableName}";
                result = await _connection.QueryAsync<TEntity>(
                    sql,
                    commandTimeout: DefaultCommandTimeoutS,
                    commandType: CommandType.Text
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAllAsync] Error: {ex}");
            }

            _sem.Release();
            return result;
        }

        #endregion

        #region Private Methods

        private void OpenConnection()
        {
            Task.Run(async () => await OpenConnectionAsync()).Wait();
        }

        private async Task OpenConnectionAsync(CancellationToken stoppingToken = default)
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }

            _connection = new MySqlConnection(_connectionString);
            await _connection.OpenAsync(stoppingToken);
            await WaitForConnectionAsync(stoppingToken);
        }

        private async Task WaitForConnectionAsync(CancellationToken stoppingToken = default)
        {
            var maxAttempts = 3;
            var attempts = 0;

            while ((_connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
            {
                if (attempts >= maxAttempts)
                    break;

                attempts++;
                await Task.Delay(TimeSpan.FromSeconds(DefaultConnectionWaitTimeS), stoppingToken);
            }
        }

        private void EnsureConnectionIsOpen()
        {
            if (_connection == null || (_connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }
        }

        #endregion

        public static void AddTypeMappers()
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
            SqlMapper.AddTypeHandler(typeof(SeenType), SeenTypeTypeHandler.Default);
            //GymDefender.Gender
            //GymTrainer.Team
            //Weather.WeatherCondition
        }

        public static void SetTypeMap<TEntity>()
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
}