namespace Chuck.Data.Repositories
{
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class IVListRepository : EfCoreRepository<IVList, DeviceControllerContext>
    {
        public IVListRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}