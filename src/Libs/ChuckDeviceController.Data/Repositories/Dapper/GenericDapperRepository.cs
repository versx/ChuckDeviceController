namespace ChuckDeviceController.Data.Repositories.Dapper;

using System.Linq.Expressions;

using MySqlConnector;

public class GenericDapperRepository<TEntity> : IGenericRepository<TEntity>, IGenericRepositoryAsync<TEntity>
    where TEntity : class
{
    protected readonly MySqlConnection _connection;

    public GenericDapperRepository(MySqlConnection connection)
    {
        _connection = connection;
    }

    #region Synchronous Repository Pattern

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

    #endregion

    #region Asynchronous Repository Pattern

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

    #endregion
}