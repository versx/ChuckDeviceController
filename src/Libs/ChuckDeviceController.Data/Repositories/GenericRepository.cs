namespace ChuckDeviceController.Data.Repositories;

using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using MySqlConnector;

public class GenericDapperRepository<TEntity> : IGenericRepository<TEntity>, IGenericRepositoryAsync<TEntity>
    where TEntity : class
{
    protected readonly MySqlConnection _connection;

    public GenericDapperRepository(MySqlConnection connection)
    {
        _connection = connection;
    }

    public bool Any(Expression<Func<TEntity, bool>> expression)
    {
        return false;
    }

    public TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? filter = null)
    {
        TEntity? entity = null;
        return entity;
    }

    public IEnumerable<TEntity> Find(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        return null;
    }

    public IEnumerable<TEntity> FindAll()
    {
        return null;
    }
    public TEntity? FindById<TKey>(TKey id)
    {
        return null;
    }

    public void Add(TEntity entity)
    {
    }
    public void AddRange(IEnumerable<TEntity> entities)
    {
    }

    public void Remove(TEntity entity)
    {
    }
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
    }

    public void Update(TEntity entity)
    {
    }
    public void UpdateRange(IEnumerable<TEntity> entities)
    {
    }


    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        return null;
    }
    public async Task<IEnumerable<TEntity>> FindAllAsync()
    {
        return null;
    }
    public async Task<TEntity?> FindByIdAsync<TKey>(TKey id)
    {
        return null;
    }

    public async Task AddAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }
    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }
    public async Task RemoveRangeAsync(IEnumerable<TEntity> entities)
    {
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(TEntity entity)
    {
        await Task.CompletedTask;
    }
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        await Task.CompletedTask;
    }
}

public class GenericRepository<TDbContext, TEntity> : IGenericRepository<TEntity>, IGenericRepositoryAsync<TEntity>
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


    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (filter != null) query = query.Where(filter);
        if (orderBy != null) query = orderBy(query);

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