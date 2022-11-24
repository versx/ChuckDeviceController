namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Entities;

    public interface IEntityDataRepository
    {
        Task<TEntity?> GetByIdAsync<TKey, TEntity>(TKey key, CancellationToken stoppingToken = default)
            where TKey : notnull
            where TEntity : BaseEntity;

        Task<IEnumerable<TEntity>?> GetAllAsync<TEntity>(CancellationToken stoppingToken = default)
            where TEntity : BaseEntity;
    }
}