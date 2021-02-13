namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class InstanceRepository : EfCoreRepository<Instance, DeviceControllerContext>
    {
        public InstanceRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}