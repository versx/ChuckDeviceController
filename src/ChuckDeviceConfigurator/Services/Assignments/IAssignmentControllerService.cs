namespace ChuckDeviceConfigurator.Services.Assignments
{
    using ChuckDeviceController.Data.Entities;

    public interface IAssignmentControllerService : IControllerService<Assignment, uint>
    {
        event EventHandler<AssignmentDeviceReloadedEventArgs> DeviceReloaded;

        void Start();

        void Stop();

        void Delete(Assignment assignment);

        Task InstanceControllerComplete(string name);
    }
}