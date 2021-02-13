namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class SpawnpointRepository : EfCoreRepository<Spawnpoint, DeviceControllerContext>
    {
        public SpawnpointRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}