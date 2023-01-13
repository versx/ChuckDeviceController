namespace ChuckDeviceController.Data.Repositories.Dapper;

using System.Linq.Expressions;

public interface IDapperGenericRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : class
{
    bool Any(Expression<Func<TEntity, bool>>? predicate = null);

    int Count(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includes);

    Task<IEnumerable<TEntity>> FindAllAsync(
        CancellationToken stoppingToken = default);

    Task<TEntity> FindAsync(
        TKey id,
        CancellationToken stoppingToken = default);

    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken stoppingToken = default);

    Task<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
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

    Task<int> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken stoppingToken = default);

    Task<int> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        Dictionary<string, Func<TEntity, object>> mappings,
        CancellationToken stoppingToken = default);

    Task<bool> DeleteAsync(
        TKey id,
        CancellationToken stoppingToken = default);

    Task<bool> DeleteRangeAsync(
        IEnumerable<TKey> ids,
        CancellationToken stoppingToken = default);
}