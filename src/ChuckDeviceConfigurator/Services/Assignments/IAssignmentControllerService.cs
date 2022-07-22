namespace ChuckDeviceConfigurator.Services.Assignments
{
    using ChuckDeviceConfigurator.Services.Assignments.EventArgs;
    using ChuckDeviceController.Data.Entities;

    /// <summary>
    /// Manages all auto-assignments for devices.
    /// </summary>
    public interface IAssignmentControllerService : IControllerService<Assignment, uint>
    {
        /// <summary>
        /// Event that is fired when an AutoInstanceController completes, informs
        /// <seealso cref="Jobs.JobControllerService"/> that the cached device needs
        /// to be reloaded.
        /// </summary>
        event EventHandler<AssignmentDeviceReloadedEventArgs> DeviceReloaded;

        /// <summary>
        /// Starts the <see cref="IAssignmentControllerService"/>.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the <see cref="IAssignmentControllerService"/>.
        /// </summary>
        void Stop();

        /// <summary>
        /// Deletes (removes) the specified assignment from the assignments cache.
        /// </summary>
        /// <param name="assignment">Assignment to delete from the cache.</param>
        void Delete(Assignment assignment);

        /// <summary>
        /// Starts the assignment for any devices specified for it.
        /// </summary>
        /// <param name="assignment">Assignment to start.</param>
        /// <returns></returns>
        Task StartAssignmentAsync(Assignment assignment);

        /// <summary>
        /// Starts all assignments in the assignment group.
        /// </summary>
        /// <param name="assignmentGroup">Assignment group to start.</param>
        Task StartAssignmentGroupAsync(AssignmentGroup assignmentGroup);

        /// <summary>
        ///     Called when an AutoInstanceController completes. Triggers all "On-Complete"
        ///     assignments for devices assigned to AutoInstanceController.
        /// </summary>
        /// <param name="instanceName">
        ///     (Optional) Instance name device is switching
        ///     from.
        /// </param>
        Task InstanceControllerCompleteAsync(string instanceName);
    }
}