namespace ChuckDeviceConfigurator.Services.Assignments;

using ChuckDeviceConfigurator.Services.Assignments.EventArgs;
using ChuckDeviceController.Data.Entities;

/// <summary>
/// Manages all auto-assignments for devices.
/// </summary>
public interface IAssignmentControllerService : IControllerService<Assignment, uint>
{
    /// <summary>
    /// Event that is fired when an AutoInstanceController completes, informs
    /// <seealso cref="Jobs.IJobControllerService"/> that the cached device needs
    /// to be reloaded.
    /// </summary>
    event EventHandler<AssignmentDeviceReloadedEventArgs> DeviceReloaded;

    /// <summary>
    /// 
    /// </summary>
    event EventHandler<ReloadInstanceEventArgs> ReloadInstance;

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

    #region Start Assignments

    /// <summary>
    /// Starts the assignment for any devices specified for it.
    /// </summary>
    /// <param name="assignment">Assignment to start.</param>
    Task StartAssignmentAsync(Assignment assignment);

    /// <summary>
    /// Starts all assignments in the assignment group.
    /// </summary>
    /// <param name="assignmentGroup">Assignment group to start.</param>
    Task StartAssignmentGroupAsync(AssignmentGroup assignmentGroup);

    #endregion

    #region ReQuest Assignments

    /// <summary>
    /// Clears all quests for related instances affected by assignment group
    /// assignments and re-quests.
    /// </summary>
    /// <param name="assignmentIds">Assignment IDs to re-quest.</param>
    Task ReQuestAssignmentsAsync(IEnumerable<uint> assignmentIds);

    /// <summary>
    /// Clears all quests for related instances affected by assignment
    /// and re-quests.
    /// </summary>
    /// <param name="assignmentId">Assignment ID to re-quest.</param>
    Task ReQuestAssignmentAsync(uint assignmentId);

    #endregion

    #region Clear Quests

    /// <summary>
    /// Clears all quests for related instances affected by assignment.
    /// </summary>
    /// <param name="assignment">Assignment to clear instance Pokestop quests from.</param>
    Task ClearQuestsAsync(Assignment assignment);

    /// <summary>
    /// Clears all quests for related instances affected by assignments.
    /// </summary>
    /// <param name="assignmentIds">Assignment IDs to clear instance Pokestop quests from.</param>
    Task ClearQuestsAsync(IEnumerable<uint> assignmentIds);

    /// <summary>
    /// Clears all quests for related instances affected by assignments.
    /// </summary>
    /// <param name="assignments">Assignments to clear instance Pokestop quests from.</param>
    Task ClearQuestsAsync(IEnumerable<Assignment> assignments);

    #endregion

    /// <summary>
    ///     Called when an AutoInstanceController completes. Triggers all "On-Complete"
    ///     assignments for devices assigned to AutoInstanceController.
    /// </summary>
    /// <param name="instanceName">(Optional) Instance name device is switching from.</param>
    Task InstanceControllerCompleteAsync(string instanceName);
}