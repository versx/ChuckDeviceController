namespace ChuckDeviceController.Data.Repositories;

using System.Linq.Expressions;

public interface IGenericRepository<TEntity>
    where TEntity : class
{
    bool Any(Expression<Func<TEntity, bool>> expression);

    TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? filter = null);

    Task<IEnumerable<TEntity>> FindAllAsync();

    Task<TEntity?> FindByIdAsync<TKey>(TKey id);

    Task<IEnumerable<TEntity>> FindAsync(
        //Expression<Func<TEntity, bool>>? filter = null,
        Func<TEntity, bool>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null
    );

    Task AddAsync(TEntity entity);

    Task AddRangeAsync(IEnumerable<TEntity> entities);

    Task RemoveAsync(TEntity entity);

    Task RemoveRangeAsync(IEnumerable<TEntity> entities);

    Task UpdateAsync(TEntity entity);

    Task UpdateRangeAsync(IEnumerable<TEntity> entities);
}