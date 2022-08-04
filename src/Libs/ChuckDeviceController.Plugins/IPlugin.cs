namespace ChuckDeviceController.Plugins
{
    // TODO: Add support for custom IJobControllers
    // TODO: Add support for Configurator to inherit shared library for custom services registration

    // NOTE: Only used for DI testing purposes
    public interface IPluginService
    {
        string Test { get; }
    }

    /// <summary>
    /// Base Plugin interface contract all plugins will inherit
    /// at a minimum.
    /// </summary>
    public interface IPlugin : IMetadata, IWebPlugin
    {
        Task InitializeAsync();
    }

    #region Job Controller Contracts

    public interface IJobControllerServiceHost
    {
        Task AddJobControllerAsync(IJobController jobController);

        //Task AddInstanceAsync(IInstance instance);
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

    // TODO: Use ChuckDeviceConfigurator.JobControllers.IJobController interface
    // TODO: Possibly allow Instance creations to link job controller type with
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

    // NOTE: Just for testing purposes
    public interface ITask
    {
    }

    // NOTE: Just for testing purposes
    public class TaskOptions
    {
    }

    public interface IInstanceControllerService
    {
        Task CreateInstance(string name, InstanceType type, ushort minLevel, ushort maxLevel, List<string> geofences, IInstanceData data);

        Task RegisterInstanceType(InstanceType type);
    }

    // NOTE: Just for testing purposes
    public enum InstanceType
    {
    }

    // NOTE: Just for testing purposes
    public interface IInstanceData
    {
    }

    #endregion
}