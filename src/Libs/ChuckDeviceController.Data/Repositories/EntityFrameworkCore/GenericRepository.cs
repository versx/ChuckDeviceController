namespace ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

public class GenericRepository<TDbContext, TEntity> : IGenericRepository<TEntity>, IGenericRepositoryAsync<TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    protected readonly TDbContext _context;

    public GenericRepository(TDbContext context)
    {
        _context = context;
    }

    #region Synchronous Repository Pattern

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

    public IEnumerable<TEntity> Find(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (filter != null) query = query.Where(filter);
        if (orderBy != null) query = orderBy(query);

        return query;
    }

    public IEnumerable<TEntity> FindAll()
    {
        return _context.Set<TEntity>().ToList();
    }
    public TEntity? FindById<TKey>(TKey id)
    {
        return _context.Set<TEntity>().Find(id);
    }

    public void Add(TEntity entity)
    {
        _context.Set<TEntity>().Add(entity);
    }
    public void AddRange(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().AddRange(entities);
    }

    public void Remove(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
    }
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().RemoveRange(entities);
    }

    public void Update(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
    }
    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        _context.Set<TEntity>().UpdateRange(entities);
    }

    #endregion

    #region Asynchronous Repository Pattern

    public async Task<IEnumerable<TEntity>> FindAsync(
        //Expression<Func<TEntity, bool>>? filter = null,
        Func<TEntity, bool>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (orderBy != null) query = orderBy(query);
        if (filter != null) return query.Where(filter); // NOTES: Crappy workaround

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

    #endregion
}