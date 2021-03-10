namespace Chuck.Data.Repositories
{
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class InstanceRepository : EfCoreRepository<Instance, DeviceControllerContext>
    {
        public InstanceRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}