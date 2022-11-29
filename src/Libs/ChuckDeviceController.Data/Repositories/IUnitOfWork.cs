namespace ChuckDeviceController.Data.Repositories
{
    // TODO: Finish IUnitOfWork repository pattern impl

    public interface IUnitOfWork
    {
        IEntityDataRepository Accounts { get; }

        IEntityDataRepository Assignments { get; }

        IEntityDataRepository AssignmentGroups { get; }

        IEntityDataRepository Devices { get; }

        IEntityDataRepository DeviceGroups { get; }

        // etc ...
    }
}