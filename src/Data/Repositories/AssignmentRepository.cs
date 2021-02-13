namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class AssignmentRepository : EfCoreRepository<Assignment, DeviceControllerContext>
    {
        public AssignmentRepository(DeviceControllerContext context)
            : base(context)
        {
        }
    }
}