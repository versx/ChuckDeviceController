namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class TrainerRepository : EfCoreRepository<Trainer, DeviceControllerContext>
    {
        public TrainerRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}