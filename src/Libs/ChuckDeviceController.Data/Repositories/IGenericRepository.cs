namespace ChuckDeviceController.Data.Repositories
{
    using System.Linq.Expressions;

    public interface IGenericRepository<TEntity>
        where TEntity : class
    {
        IEnumerable<TEntity> FindAll();

        TEntity? FindById<TKey>(TKey id);

        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression);

        void Add(TEntity entity);

        void AddRange(IEnumerable<TEntity> entities);

        void Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);

        void Update(TEntity entity);

        void UpdateRange(IEnumerable<TEntity> entities);
    }
}
