namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class DeviceGroupRepository : EfCoreRepository<DeviceGroup, DeviceControllerContext>
    {
        public DeviceGroupRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}