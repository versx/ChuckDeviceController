namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class DeviceRepository : EfCoreRepository<Device, DeviceControllerContext>
    {
        public DeviceRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}
