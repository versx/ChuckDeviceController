namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class IVListRepository : EfCoreRepository<IVList, DeviceControllerContext>
    {
        public IVListRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}