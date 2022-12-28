namespace ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Data.Entities;

public interface IGenericEntityRepository<TEntity> : IGenericRepository<TEntity>, IGenericRepositoryAsync<TEntity>
    where TEntity : BaseEntity
{
}

public class GenericEntityRepository<TDbContext, TEntity> : GenericRepository<TDbContext, TEntity>, IGenericEntityRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : BaseEntity
{
    public GenericEntityRepository(TDbContext context)
        : base(context)
    {
    }
}

//public interface IDeviceRepository : IGenericRepository<Device>, IGenericRepositoryAsync<Device> { }

//public class DeviceRepository<TDbContext> : GenericRepository<TDbContext, Device>, IDeviceRepository
//    where TDbContext : DbContext
//{
//    public DeviceRepository(TDbContext context)
//        : base(context) { }
//}