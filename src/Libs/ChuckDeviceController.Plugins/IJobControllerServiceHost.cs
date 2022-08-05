namespace ChuckDeviceController.Plugins
{
    public interface IJobControllerServiceHost
    {
        Task RegisterAllJobControllerTypesAsync();

        Task RegisterJobControllerTypeAsync(InstanceType type);

        Task AddJobControllerAsync(IJobController controller);
    }

    public interface ICoordinate
    {
        double Latitude { get; }

        double Longitude { get; }
    }

    public interface IJobControllerCoordinates
    {
        IReadOnlyList<ICoordinate> Coordinates { get; }
    }

    // TODO: Use ChuckDeviceController.Common.Jobs.IJobController interface

    public interface IJobController : IJobControllerCoordinates
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

    // NOTE: Just for testing purposes, reference ChuckDeviceController.Common
    public interface ITask
    {
    }

    // NOTE: Just for testing purposes, reference ChuckDeviceController.Common
    public class TaskOptions
    {
    }

    // NOTE: Just for testing purposes, reference ChuckDeviceController.Common
    public enum InstanceType
    {
    }

    // NOTE: Just for testing purposes
    public interface IInstanceData
    {
    }
}