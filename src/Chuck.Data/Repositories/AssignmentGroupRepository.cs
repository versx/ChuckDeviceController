namespace Chuck.Data.Repositories
{
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class AssignmentGroupRepository : EfCoreRepository<AssignmentGroup, DeviceControllerContext>
    {
        public AssignmentGroupRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}