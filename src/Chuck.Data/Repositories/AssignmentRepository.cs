namespace Chuck.Data.Repositories
{
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class AssignmentRepository : EfCoreRepository<Assignment, DeviceControllerContext>
    {
        public AssignmentRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}