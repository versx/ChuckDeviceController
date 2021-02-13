namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class GymDefenderRepository : EfCoreRepository<GymDefender, DeviceControllerContext>
    {
        public GymDefenderRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}