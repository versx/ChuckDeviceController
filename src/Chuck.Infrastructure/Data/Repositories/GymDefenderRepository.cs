namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class GymDefenderRepository : EfCoreRepository<GymDefender, DeviceControllerContext>
    {
        public GymDefenderRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}