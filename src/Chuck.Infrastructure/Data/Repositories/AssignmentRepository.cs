namespace Chuck.Infrastructure.Data.Repositories
{
    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;

    public class AssignmentRepository : EfCoreRepository<Assignment, DeviceControllerContext>
    {
        public AssignmentRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}