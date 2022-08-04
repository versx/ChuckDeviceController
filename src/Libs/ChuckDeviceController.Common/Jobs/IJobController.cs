namespace ChuckDeviceController.Common.Jobs
{
    using ChuckDeviceController.Common.Tasks;

    /// <summary>
    /// Job controller instance minimum contract all job controllers
    /// must adhere to.
    /// </summary>
    public interface IJobController : IEventInstanceController
    {
        /// <summary>
        /// Gets the name of the job controller.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the minimum level an account must meet in order to
        /// use this job controller.
        /// </summary>
        ushort MinimumLevel { get; }

        /// <summary>
        /// Gets the maximum level an account must meet in order to
        /// use this job controller.
        /// </summary>
        ushort MaximumLevel { get; }

        /// <summary>
        /// Gets a task for the specified device.
        /// </summary>
        /// <param name="options">
        /// Task options to include when retrieving the task.
        /// </param>
        /// <returns>Returns a job controller task.</returns>
        Task<ITask> GetTaskAsync(TaskOptions options);

        /// <summary>
        /// Gets the status of the job controller.
        /// </summary>
        /// <returns>
        /// Returns the text value of the status for the job controller.
        /// </returns>
        Task<string> GetStatusAsync();

        /// <summary>
        /// Stops the job controller.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Reloads or resets the job controller as if it was newly created.
        /// </summary>
        Task ReloadAsync();
    }
}