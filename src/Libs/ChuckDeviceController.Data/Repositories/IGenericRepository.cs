namespace ChuckDeviceController.Data.Repositories
{
    using System.Linq.Expressions;

    public interface IGenericRepository<TEntity>
        where TEntity : class
    {
        bool Any(Expression<Func<TEntity, bool>> expression);

        IEnumerable<TEntity> FindAll();

        TEntity? FindById<TKey>(TKey id);

        IEnumerable<TEntity> Find(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null
        );

        void Add(TEntity entity);

        void AddRange(IEnumerable<TEntity> entities);

        void Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);

        void Update(TEntity entity);

        void UpdateRange(IEnumerable<TEntity> entities);
    }
}
