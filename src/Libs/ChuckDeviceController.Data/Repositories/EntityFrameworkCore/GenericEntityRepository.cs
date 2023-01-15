namespace ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Data.Entities;

public class GenericEntityRepository<TDbContext, TEntity> : GenericRepository<TDbContext, TEntity>, IGenericEntityRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : BaseEntity
{
    public GenericEntityRepository(TDbContext context)
        : base(context)
    {
    }
}