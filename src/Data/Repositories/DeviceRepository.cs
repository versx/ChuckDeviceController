namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class DeviceRepository : EfCoreRepository<Device, DeviceControllerContext>
    {
        public DeviceRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}
