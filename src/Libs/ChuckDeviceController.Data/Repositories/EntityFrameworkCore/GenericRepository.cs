namespace ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

public class GenericRepository<TDbContext, TEntity> : IGenericRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    protected readonly TDbContext _context;

    public GenericRepository(TDbContext context)
    {
        _context = context;
    }

    public bool Any(Expression<Func<TEntity, bool>> expression)
    {
        return _context.Set<TEntity>().Any(expression);
    }

    public TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? filter = null)
    {
        TEntity? entity = null;
        var query = _context.Set<TEntity>();
        if (filter != null) entity = query.FirstOrDefault(filter);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> FindAsync(
        //Expression<Func<TEntity, bool>>? filter = null,
        Func<TEntity, bool>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (orderBy != null) query = orderBy(query);
        if (filter != null) return query.Where(filter); // NOTE: Crappy workaround

        return await Task.FromResult(query);
    }
    public async Task<IEnumerable<TEntity>> FindAllAsync()
    {
        return await _context.Set<TEntity>().ToListAsync();
    }
    public async Task<TEntity?> FindByIdAsync<TKey>(TKey id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    public async Task AddAsync(TEntity entity)
    {
        await _context.Set<TEntity>().AddAsync(entity);
    }
    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().AddRange(entities);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
        await Task.CompletedTask;
    }
    public async Task RemoveRangeAsync(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().RemoveRange(entities);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
        await Task.CompletedTask;
    }
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().UpdateRange(entities);
        await Task.CompletedTask;
    }
}