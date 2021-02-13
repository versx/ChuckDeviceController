namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class TrainerRepository : EfCoreRepository<Trainer, DeviceControllerContext>
    {
        public TrainerRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}