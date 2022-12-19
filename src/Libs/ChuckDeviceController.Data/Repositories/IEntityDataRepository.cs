namespace ChuckDeviceController.Data.Repositories;

using MySqlConnector;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;

public interface IEntityDataRepository
{
    /// <summary>
    /// Get data entity for type by primary key.
    /// </summary>
    /// <typeparam name="TKey">Primary key type for data entity.</typeparam>
    /// <typeparam name="TEntity">Data entity type.</typeparam>
    /// <param name="key">Primary key of data entity.</param>
    /// <param name="stoppingToken">Cancellation token to signal exit.</param>
    /// <returns>Returns data entity of primary key.</returns>
    //Task<TEntity?> GetByIdAsync<TKey, TEntity>(TKey key, CancellationToken stoppingToken = default)
    Task<TEntity?> GetByIdAsync<TKey, TEntity>(MySqlConnection connection, TKey key, CancellationToken stoppingToken = default)
        where TKey : notnull
        where TEntity : BaseEntity;

    /// <summary>
    /// Get list of all data entities for type.
    /// </summary>
    /// <typeparam name="TEntity">Data entity type.</typeparam>
    /// <param name="stoppingToken">Cancellation token to signal exit.</param>
    /// <returns>Returns list of data entities.</returns>
    Task<IEnumerable<TEntity>?> GetAllAsync<TEntity>(CancellationToken stoppingToken = default)
        where TEntity : BaseEntity;

    /// <summary>
    /// Execute parameterized SQL query.
    /// </summary>
    /// <param name="sql">SQL query to execute.</param>
    /// <param name="param">Parameter for SQL query.</param>
    /// <param name="commandTimeoutS">SQL command timeout in seconds.</param>
    /// <param name="stoppingToken">Cancellation token to signal exit.</param>
    /// <returns>Returns the number of rows affected by the query.</returns>
    /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
    Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        int? commandTimeoutS = EntityDataRepository.DefaultCommandTimeoutS,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// Execute raw SQL query.
    /// </summary>
    /// <param name="sqls">List of SQL queries to execute.</param>
    /// <param name="commandTimeoutS">SQL command timeout in seconds.</param>
    /// <param name="stoppingToken">Cancellation token to signal exit.</param>
    /// <returns>Returns the number of rows affected by the query.</returns>
    /// <exception cref="Exception">Throws if MySQL connection is null.</exception>
    Task<int> ExecuteAsync(
        IEnumerable<string> sqls,
        int? commandTimeoutS = EntityDataRepository.DefaultCommandTimeoutS,
        CancellationToken stoppingToken = default);

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
    Task<int> ExecuteBulkAsync<TEntity>(
        string tableName,
        IEnumerable<TEntity> entities,
        ColumnDataExpression<TEntity> dataFunc,
        CancellationToken stoppingToken = default)
        where TEntity : BaseEntity;
}