namespace Chuck.Data.Repositories
{
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class DeviceGroupRepository : EfCoreRepository<DeviceGroup, DeviceControllerContext>
    {
        public DeviceGroupRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}