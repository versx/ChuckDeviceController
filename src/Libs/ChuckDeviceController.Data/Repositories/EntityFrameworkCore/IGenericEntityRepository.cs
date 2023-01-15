namespace ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

using ChuckDeviceController.Data.Entities;

public interface IGenericEntityRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
}