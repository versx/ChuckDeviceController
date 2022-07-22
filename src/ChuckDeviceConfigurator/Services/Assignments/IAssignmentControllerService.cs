namespace ChuckDeviceConfigurator.Services.Assignments
{
    using ChuckDeviceConfigurator.Services.Assignments.EventArgs;
    using ChuckDeviceController.Data.Entities;

    public interface IAssignmentControllerService : IControllerService<Assignment, uint>
    {
        event EventHandler<AssignmentDeviceReloadedEventArgs> DeviceReloaded;

        void Start();

        void Stop();

        void Delete(Assignment assignment);

        Task StartAssignmentAsync(Assignment assignment);

        Task InstanceControllerCompleteAsync(string instanceName);
    }
}