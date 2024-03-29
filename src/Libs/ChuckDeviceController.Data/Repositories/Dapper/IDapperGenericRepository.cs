﻿namespace ChuckDeviceController.Data.Repositories.Dapper;

public interface IDapperGenericRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : class
{
    Task<IEnumerable<TEntity>> FindAllAsync(
        CancellationToken stoppingToken = default);

    Task<TEntity> FindAsync(
        TKey id,
        CancellationToken stoppingToken = default);

    Task<int> InsertAsync(
        TEntity entity,
        CancellationToken stoppingToken = default);

    Task<int> InsertRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken stoppingToken = default);

    Task<int> UpdateAsync(
        TEntity entity,
        CancellationToken stoppingToken = default);

    Task<int> UpdateAsync(
        TEntity entity,
        Dictionary<string, Func<TEntity, object>> mappings,
        CancellationToken stoppingToken = default);

    Task<bool> DeleteAsync(
        TKey id,
        CancellationToken stoppingToken = default);

    Task<bool> DeleteRangeAsync(
        IEnumerable<TKey> ids,
        CancellationToken stoppingToken = default);
}