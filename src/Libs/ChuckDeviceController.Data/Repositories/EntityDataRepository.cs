namespace ChuckDeviceController.Data.Repositories;

using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.TypeHandlers;
using ChuckDeviceController.Logging;

public class EntityDataRepository : IEntityDataRepository
{
    #region Constants

    public const uint DefaultConnectionWaitTimeS = 30;
    public const int DefaultCommandTimeoutS = 30;

    #endregion

    #region Variables

    private readonly ILogger<IEntityDataRepository> _logger =
        GenericLoggerFactory.CreateLogger<IEntityDataRepository>();
    private readonly SemaphoreSlim _sem = new(1, 1);
    private readonly SemaphoreSlim _semEntity = new(1, 1); //new(15, 15);
    //private readonly string _connectionString;
    private MySqlConnection? _connection;

    #endregion

    #region Constructor

    public EntityDataRepository()
    {
        AddTypeMappers();
        OpenConnection($"{nameof(EntityDataRepository)}::ctor");
    }

    #endregion

    #region Get Entity Methods

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="key"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public virtual async Task<TEntity?> GetByIdAsync<TKey, TEntity>(MySqlConnection connection, TKey key, CancellationToken stoppingToken = default)
        where TKey : notnull
        where TEntity : BaseEntity
    {
        //await _semEntity.WaitAsync(stoppingToken);

        TEntity? result = null;
        try
        {
            //EnsureConnectionIsOpen(attemptReopen: true);

            var tableName = typeof(TEntity).GetTableAttribute();
            var keyName = typeof(TEntity).GetKeyAttribute();
            var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{key}'";

            result = await connection.QueryFirstOrDefaultAsync<TEntity>(
                sql,
                commandTimeout: DefaultCommandTimeoutS,
                commandType: CommandType.Text
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[GetByIdAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
        }

        //_semEntity.Release();
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TEntity>?> GetAllAsync<TEntity>(CancellationToken stoppingToken = default)
        where TEntity : BaseEntity
    {
        await _sem.WaitAsync(stoppingToken);

        IEnumerable<TEntity>? result = null;
        try
        {
            EnsureConnectionIsOpen(attemptReopen: true);

            var tableName = typeof(TEntity).GetTableAttribute();
            var sql = $"SELECT * FROM {tableName}";
            result = await _connection.QueryAsync<TEntity>(
                sql,
                commandTimeout: DefaultCommandTimeoutS,
                commandType: CommandType.Text
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[GetAllAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
        }

        _sem.Release();
        return result;
    }

    #endregion

    #region Execute Methods

    /// <summary>
    /// Execute parameterized SQL query.
    /// </summary>
    /// <param name="sql">SQL query to execute.</param>
    /// <param name="param">Parameter for SQL query.</param>
    /// <param name="commandTimeoutS">SQL command timeout in seconds.</param>
    /// <param name="stoppingToken">Cancellation token to signal exit.</param>
    /// <returns>Returns the number of rows affected by the query.</returns>
    /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
    public virtual async Task<int> ExecuteAsync(string sql, object? param = null, int? commandTimeoutS = DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
    {
        await _sem.WaitAsync(stoppingToken);

        EnsureConnectionIsOpen(attemptReopen: true);

        if (_connection == null)
        {
            throw new Exception($"Not connected to MySQL database server!");
        }

        var rowsAffected = 0;
        try
        {
            rowsAffected = await _connection.ExecuteAsync(sql, param, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ExecuteAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
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
    public virtual async Task<int> ExecuteAsync(IEnumerable<string> sqls, int? commandTimeoutS = DefaultCommandTimeoutS, CancellationToken stoppingToken = default)
    {
        await _sem.WaitAsync(stoppingToken);

        // EnsureConnectionIsOpen(attemptReopen: true);

        if (_connection == null)
        {
            throw new Exception($"Not connected to MySQL database server!");
        }

        var rowsAffected = 0;
        using var trans = await _connection.BeginTransactionAsync(stoppingToken);
        try
        {
            foreach (var sql in sqls)
            {
                rowsAffected += await _connection.ExecuteAsync(sql, transaction: trans, commandTimeout: commandTimeoutS, commandType: CommandType.Text);
            }
            await trans.CommitAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ExecuteAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
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
    public virtual async Task<int> ExecuteBulkAsync<TEntity>(string tableName, IEnumerable<TEntity> entities, ColumnDataExpression<TEntity> dataFunc, CancellationToken stoppingToken = default)
        where TEntity : BaseEntity
    {
        await _sem.WaitAsync(stoppingToken);

        if (_connection == null)
        {
            throw new Exception($"Not connected to MySQL database server!");
        }

        var rowsAffected = 0;
        using var trans = await _connection.BeginTransactionAsync(stoppingToken);
        try
        {
            rowsAffected += await _connection.BulkInsertAsync(tableName, entities, dataFunc, trans, includeOnDuplicateQuery: true);
            await trans.CommitAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ExecuteBulkAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
            await trans.RollbackAsync(stoppingToken);
        }

        _sem.Release();
        return rowsAffected;
    }

    #endregion

    #region Private Methods

    private void OpenConnection(string name)
    {
        Task.Run(async () => await OpenConnectionAsync(name)).Wait();
    }

    private async Task OpenConnectionAsync(string name, CancellationToken stoppingToken = default)
    {
        if (_connection != null && _connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(stoppingToken);
        }
        else if (_connection == null || (_connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
        {
            _connection?.Dispose();
            _connection = await EntityRepository.CreateConnectionAsync(name, stoppingToken: stoppingToken);
        }
    }

    private void EnsureConnectionIsOpen(bool attemptReopen = true)
    {
        if (_connection == null || (_connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
        {
            if (!attemptReopen)
            {
                throw new Exception($"Not connected to MySQL database server!");
            }

            OpenConnection($"{nameof(EntityDataRepository)}::EnsureConnectionIsOpen");
        }
    }

    #endregion

    #region Dapper Type Mappings

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

    public static void SetTypeMap<TEntity>() => SetTypeMap(typeof(TEntity));

    public static void SetTypeMap(Type type)
    {
        SqlMapper.SetTypeMap(
            type,
            new CustomPropertyTypeMap(
                type,
                (type, columnName) =>
                    type.GetProperties().FirstOrDefault(prop =>
                        prop.GetCustomAttributes(false)
                            .OfType<ColumnAttribute>()
                            .Any(attr => attr.Name == columnName))));
    }

    #endregion
}