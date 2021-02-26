namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class InstanceRepository : EfCoreRepository<Instance, DeviceControllerContext>
    {
        public InstanceRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}