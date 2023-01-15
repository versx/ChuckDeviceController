namespace ChuckDeviceController.Data.Repositories.Dapper;

using System.Linq.Expressions;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public interface IDapperGenericRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    bool Any(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    int Count(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> FindAllAsync(
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<TEntity> FindAsync(
        TKey id,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<int> InsertAsync(
        TEntity entity,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<int> InsertRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<int> UpdateAsync(
        TEntity entity,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="mappings"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<int> UpdateAsync(
        TEntity entity,
        Dictionary<string, Func<TEntity, object>> mappings,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<int> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="mappings"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<int> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        Dictionary<string, Func<TEntity, object>> mappings,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(
        TKey id,
        CancellationToken stoppingToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    Task<bool> DeleteRangeAsync(
        IEnumerable<TKey> ids,
        CancellationToken stoppingToken = default);
}