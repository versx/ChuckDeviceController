namespace ChuckDeviceConfigurator.Services.Assignments
{
    using ChuckDeviceController.Data.Entities;

    public interface IAssignmentControllerService : IControllerService<Assignment, uint>
    {
        event EventHandler<AssignmentDeviceReloadedEventArgs> DeviceReloaded;

        void Start();

        void Stop();

        /*
        void Reload();

        void Add(Assignment assignment);

        void Edit(uint oldAssignmentId, Assignment newAssignment);

        void Delete(uint oldAssignmentId);
        */

        void Delete(Assignment assignment);

        Task InstanceControllerComplete(string name);
    }
}