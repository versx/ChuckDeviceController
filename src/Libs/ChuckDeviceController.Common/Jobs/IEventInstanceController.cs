namespace ChuckDeviceController.Common.Jobs
{
    /// <summary>
    /// Unique event job controller.
    /// </summary>
    public interface IEventInstanceController
    {
        /// <summary>
        /// Gets a unique group name to use with job controller instances
        /// to group related accounts with.
        /// </summary>
        string GroupName { get; }

        /// <summary>
        /// Gets a value determining whether the instance is for an event or
        /// not. Returns <c>true</c> if it is an event, otherwise <c>false</c>.
        /// </summary>
        bool IsEvent { get; }
    }
}